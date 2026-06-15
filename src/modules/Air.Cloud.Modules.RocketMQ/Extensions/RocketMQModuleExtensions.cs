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
using Air.Cloud.Core;
using Air.Cloud.Core.App;
using Air.Cloud.Core.Standard.MessageQueue;
using Air.Cloud.Core.Standard.MessageQueue.Attributes;
using Air.Cloud.Core.Standard.TraceLog.Defaults;
using Air.Cloud.Modules.RocketMQ.Config;
using Air.Cloud.Modules.RocketMQ.Helper;
using Air.Cloud.Modules.RocketMQ.Model;
using Air.Cloud.Modules.RocketMQ.Options;
using Air.Cloud.Modules.RocketMQ.Standards;
using Air.Cloud.Modules.RocketMQ.Utils;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using System.Reflection;

namespace Air.Cloud.Modules.RocketMQ.Extensions
{
    /// <summary>
    /// <para>zh-cn:RocketMQ 模块服务注册扩展。</para>
    /// <para>en-us:RocketMQ module service registration extensions.</para>
    /// </summary>
    public static class RocketMQModuleExtensions
    {
        public static IDictionary<Type, object> MessageQueueSubscriber = new Dictionary<Type, object>();

        /// <summary>
        /// <para>zh-cn:初始化 RocketMQ 服务，注册消息队列标准、默认字符串 Key 生成器、消费恢复、失败补偿和消费执行策略，并装载 AppQueueDescriptor 自动订阅。</para>
        /// <para>en-us:Initializes RocketMQ services by registering the message-queue standard, default string key generator, consumer recovery, failure compensation, consumer execution options, and AppQueueDescriptor auto subscriptions.</para>
        /// </summary>
        /// <param name="services">
        /// <para>zh-cn:服务集合。</para>
        /// <para>en-us:The service collection.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:服务集合。</para>
        /// <para>en-us:The service collection.</para>
        /// </returns>
        public static IServiceCollection AddRocketMQService(this IServiceCollection services)
        {
            services.AddSingleton<IMessageQueueStandard, RocketMQMessageQueueDependency>();
            services.TryAddSingleton<IMessageQueueKeyGenerator<string>, RocketMQStringMessageQueueKeyGenerator>();
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IMessageQueueKeyGenerator, RocketMQStringMessageQueueKeyGenerator>());
            services.TryAddSingleton<IMessageQueueConsumerRecoveryStandard, RocketMQConsumerRecoveryStandard>();
            services.TryAddSingleton<IMessageQueueFailureCompensationStandard, RocketMQFailureCompensationStandard>();
            services.TryAddSingleton<IMessageQueueConsumerExecutionOptions, RocketMQConsumerExecutionOptions>();

            foreach (var item in AppCore.LoadSpecifyTypes(typeof(IMessageQueueSubscribeStandard<>)))
            {
                var subscribeAttribute = item.GetCustomAttribute<AppQueueDescriptorAttribute>();
                if (subscribeAttribute == null)
                {
                    continue;
                }

                var implMethod = item.GetMethod(IMessageQueueSubscribeStandard<IMessageContentStandard>.SUBSCRIBE_METHOD);
                var parameterType = implMethod?.GetParameters().FirstOrDefault()?.ParameterType;
                if (implMethod == null || parameterType == null)
                {
                    continue;
                }

                var method = AppRealization.Queue.GetType()
                    .GetMethod(IMessageQueueStandard.SUBSCRIBE_METHOD)
                    ?.MakeGenericMethod(typeof(RocketMQConsumerConfig), parameterType);
                if (method == null)
                {
                    continue;
                }

                var needProduce = !string.IsNullOrEmpty(subscribeAttribute.ReciveQueue);
                var rocketOptions = AppCore.GetOptions<RocketMQSettingsOptions>();
                var consumerConfig = rocketOptions?.GetConsumerConfigModelByName(subscribeAttribute.SubscribeQueue) ?? new RocketMQConsumerConfigModel()
                {
                    TopicName = subscribeAttribute.SubscribeQueue
                };
                consumerConfig.TopicName = subscribeAttribute.SubscribeQueue;

                RocketMQProducerConfigModel producerConfig = null;
                if (needProduce)
                {
                    producerConfig = rocketOptions?.GetProducerConfigModelByName(subscribeAttribute.ReciveQueue) ?? new RocketMQProducerConfigModel()
                    {
                        TopicName = subscribeAttribute.ReciveQueue
                    };
                    producerConfig.TopicName = subscribeAttribute.ReciveQueue;
                }

                Action<IMessageContentStandard> subscribe = message =>
                {
                    try
                    {
                        var instance = MessageQueueSubscriber.ContainsKey(item)
                            ? MessageQueueSubscriber[item]
                            : Activator.CreateInstance(item);
                        if (!MessageQueueSubscriber.ContainsKey(item))
                        {
                            MessageQueueSubscriber.Add(item, instance);
                        }

                        var result = implMethod.Invoke(instance, new object[] { message });
                        if (needProduce)
                        {
                            AppRealization.Queue.Publish(producerConfig, result);
                        }
                    }
                    catch (Exception ex)
                    {
                        AppRealization.TraceLog.Write(new DefaultTraceLogContent()
                        {
                            Title = "rocketmq-error",
                            Content = ex.Message,
                            AdditionalParams = new Dictionary<string, object>()
                            {
                                { "StackTrace", ex.StackTrace },
                                { "Message", ex.Message },
                                { "Source", ex.Source },
                                { "InnerException", ex.InnerException?.Message },
                                { "InnerExceptionStackTrace", ex.InnerException?.StackTrace }
                            },
                            Tags = "rocketmq_subscribe_error"
                        });
                    }
                };

                method.Invoke(AppRealization.Queue, new object[]
                {
                    consumerConfig,
                    subscribe,
                    subscribeAttribute.GroupId
                });
            }

            return services;
        }
    }
}
