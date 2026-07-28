/*
 * Copyright (c) 2024-2030 星曳数据
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 *
 * This file is provided under the Mozilla Public License Version 2.0,
 * and the "NO WARRANTY" clause of the MPL is hereby expressly
 * acknowledged.
 */
using Air.Cloud.Core.App;
using Air.Cloud.Core;
using Air.Cloud.Core.Standard.MessageQueue;
using Air.Cloud.Core.Standard.MessageQueue.Attributes;
using Air.Cloud.Modules.Kafka.Config;
using Air.Cloud.Modules.Kafka.Helper;
using Air.Cloud.Modules.Kafka.Model;
using Air.Cloud.Modules.Kafka.Options;
using Air.Cloud.Modules.Kafka.Standards;
using Air.Cloud.Modules.Kafka.Utils;

using Confluent.Kafka;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;
using Air.Cloud.Core.Standard.Print;
using Air.Cloud.Core.Standard.TraceLog.Defaults;

namespace Air.Cloud.Modules.Kafka.Extensions
{
    public static class KafkaModuleExtensions
    {
        public static IDictionary<Type,object> MessageQueueSubscriber = new Dictionary<Type, object>();
        /// <summary>
        /// <para>zh-cn:初始化Kafka服务</para>
        /// <para>en-us: Initialize Kafka service</para>
        /// </summary>
        /// <param name="services">
        /// <para>zh-cn:服务集合</para>
        /// <para>en-us: Service collection</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:服务集合</para>
        /// <para>en-us: Service collection</para>
        /// </returns>
        public static IServiceCollection AddKafkaService(this IServiceCollection services)
        {
            services.AddSingleton<IMessageQueueStandard, KafkaMessageQueueDependency>();

            // zh-cn:注册 Kafka 默认 int 类型 Key 生成器；使用 TryAdd 保证业务侧可以提前注册自定义 int 生成器覆盖默认行为。
            // en-us:Register the default Kafka int key generator. TryAdd allows business code to register a custom int generator earlier and override the default behavior.
            services.TryAddSingleton<IMessageQueueKeyGenerator<int>, KafkaIntMessageQueueKeyGenerator>();

            // zh-cn:同时注册非泛型 Key 生成器接口，供 Kafka 模块在未显式配置 KeyType 时进行运行时类型推断。
            // en-us:Register the non-generic key generator interface as well, so the Kafka module can infer the runtime key type when KeyType is not explicitly configured.
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IMessageQueueKeyGenerator, KafkaIntMessageQueueKeyGenerator>());

            // zh-cn:注册 Kafka 默认消费者恢复实现；业务侧可在 AddKafkaService 之前注册 IMessageQueueConsumerRecoveryStandard 覆盖该默认行为。
            // en-us:Register the default Kafka consumer recovery implementation. Business code can register IMessageQueueConsumerRecoveryStandard before AddKafkaService to override this default behavior.
            services.TryAddSingleton<IMessageQueueConsumerRecoveryStandard, KafkaConsumerRecoveryStandard>();

            // zh-cn:注册 Kafka 默认失败补偿实现；默认只记录失败事件，不主动重试、不写死信、不提交 offset。
            // en-us:Register the default Kafka failure compensation implementation. The default only records failure events and does not actively retry, dead-letter, or commit offsets.
            services.TryAddSingleton<IMessageQueueFailureCompensationStandard, KafkaFailureCompensationStandard>();

            // zh-cn:注册 Kafka 默认消费者执行策略；业务侧可覆盖 MessageHandlingTimeout 与 MaxRetryCount 等规则。
            // en-us:Register the default Kafka consumer execution options. Business code can override rules such as MessageHandlingTimeout and MaxRetryCount.
            services.TryAddSingleton<IMessageQueueConsumerExecutionOptions, KafkaConsumerExecutionOptions>();
            //装载所有的消息队列订阅实现
            foreach (var item in AppCore.LoadSpecifyTypes(typeof(IMessageQueueSubscribeStandard<>)))
            {
                var SubscribeAttribute = item.GetCustomAttribute<AppQueueDescriptorAttribute>();
                var ImplMethod = item.GetMethod(IMessageQueueSubscribeStandard<IMessageContentStandard>.SUBSCRIBE_METHOD);
                //参数类型
                var ParameterTypes = ImplMethod.GetParameters().FirstOrDefault()?.ParameterType;
                //消息队列的订阅方法
                var Method = (AppRealization.Queue.GetType().GetMethod(IMessageQueueStandard.SUBSCRIBE_METHOD)).MakeGenericMethod(typeof(ConsumerConfig), ParameterTypes);
                //是否需要将消息处理后的结果发送到消息队列
                bool NeedProduce = !string.IsNullOrEmpty(SubscribeAttribute.ReciveQueue);
                //消费者配置
                var kafkaOptions = AppCore.GetOptions<KafkaSettingsOptions>();
                ConsumerConfigModel ConsumConfig = kafkaOptions?.GetConsumerConfigModelByName(SubscribeAttribute.SubscribeQueue) ?? new ConsumerConfigModel()
                {
                    TopicName = SubscribeAttribute.SubscribeQueue
                };
                ConsumConfig.TopicName = SubscribeAttribute.SubscribeQueue;
                //生产者配置
                ProducerConfigModel producerConfig = null;
                if (NeedProduce)
                {
                    producerConfig = kafkaOptions?.GetProducerConfigModelByName(SubscribeAttribute.ReciveQueue) ?? new ProducerConfigModel()
                    {
                        TopicName = SubscribeAttribute.ReciveQueue
                    };
                    producerConfig.TopicName = SubscribeAttribute.ReciveQueue;
                }
                Action<IMessageContentStandard> Subscribe = (s) =>
                    InvokeSubscriber(item, ImplMethod, s, NeedProduce, producerConfig);
                Method.Invoke(AppRealization.Queue, new object[]
                {
                        ConsumConfig,
                        Subscribe,
                        SubscribeAttribute.GroupId
                });
            }
            return services;
        }

        /// <summary>
        /// <para>zh-cn:调用自动发现的订阅者，并在保留失败审计日志后重新抛出业务异常。异常必须传回 Kafka 消费循环，才能使当前记录被判定为处理失败；使用默认的手动提交配置时，不会提交该记录的 offset。</para>
        /// <para>en-us:Invokes an automatically discovered subscriber and rethrows business exceptions after retaining failure audit logs. The exception must reach the Kafka consume loop so the current record is marked as failed; with the default manual-commit configuration, its offset is not committed.</para>
        /// </summary>
        /// <param name="subscriberType">
        /// <para>zh-cn:订阅者实现类型。</para>
        /// <para>en-us:The subscriber implementation type.</para>
        /// </param>
        /// <param name="subscribeMethod">
        /// <para>zh-cn:订阅者消息处理方法。</para>
        /// <para>en-us:The subscriber message handler method.</para>
        /// </param>
        /// <param name="message">
        /// <para>zh-cn:当前消费到的消息。</para>
        /// <para>en-us:The currently consumed message.</para>
        /// </param>
        /// <param name="needProduce">
        /// <para>zh-cn:是否需要将订阅者返回值发布到回复主题。</para>
        /// <para>en-us:Whether the subscriber return value must be published to a reply topic.</para>
        /// </param>
        /// <param name="producerConfig">
        /// <para>zh-cn:回复主题的生产者配置；仅在 <paramref name="needProduce"/> 为 true 时使用。</para>
        /// <para>en-us:The producer configuration for the reply topic; used only when <paramref name="needProduce"/> is true.</para>
        /// </param>
        private static void InvokeSubscriber(
            Type subscriberType,
            MethodInfo subscribeMethod,
            IMessageContentStandard message,
            bool needProduce,
            ProducerConfigModel producerConfig)
        {
            try
            {
                var instance = MessageQueueSubscriber.ContainsKey(subscriberType)
                    ? MessageQueueSubscriber[subscriberType]
                    : Activator.CreateInstance(subscriberType);
                if (!MessageQueueSubscriber.ContainsKey(subscriberType))
                {
                    MessageQueueSubscriber.Add(subscriberType, instance);
                }

                var response = subscribeMethod.Invoke(instance, new object[] { message });
                if (needProduce)
                {
                    AppRealization.Queue.Publish(producerConfig, response);
                }
            }
            catch (Exception ex)
            {
                try
                {
                    AppRealization.TraceLog.Write(new DefaultTraceLogContent()
                    {
                        Title = "kafka-error",
                        Content = ex.Message,
                        AdditionalParams = new Dictionary<string, object>()
                        {
                            { "StackTrace",ex.StackTrace},
                            { "Message",ex.Message},
                            { "Source",ex.Source },
                            { "InnerException",ex.InnerException?.Message},
                            { "InnerExceptionStackTrace",ex.InnerException?.StackTrace}
                        },
                        Tags = "kafka_subscribe_error"
                    });
                }
                catch (Exception traceException)
                {
                    Console.Error.WriteLine($"Kafka subscriber failure trace logging failed. Exception={traceException}");
                }

                throw;
            }
        }
    }
}
