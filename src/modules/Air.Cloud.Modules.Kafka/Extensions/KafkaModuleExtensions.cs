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
using Air.Cloud.Modules.Kafka.Helper;
using Air.Cloud.Modules.Kafka.Model;

using Confluent.Kafka;

using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using Air.Cloud.Core.Standard.Print;
using Air.Cloud.Core.Standard.TraceLog.Defaults;

namespace Air.Cloud.Modules.Kafka.Extensions
{
    public static class KafkaModuleExtensions
    {
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
                ConsumerConfigModel ConsumConfig = new ConsumerConfigModel();
                ConsumConfig.TopicName = SubscribeAttribute.SubscribeQueue;
                //生产者配置
                ProducerConfigModel producerConfig = null;
                if (NeedProduce)
                {
                    producerConfig = new ProducerConfigModel();
                    producerConfig.TopicName = SubscribeAttribute.ReciveQueue;
                }
                Action<IMessageContentStandard> Subscribe = (s) =>
                {
                    try
                    {
                        var instance = Activator.CreateInstance(item);
                        var x = ImplMethod.Invoke(instance, new object[] { s });
                        if (NeedProduce) AppRealization.Queue.Publish(producerConfig, x);
                    }
                    catch (Exception ex)
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
                };
                Method.Invoke(AppRealization.Queue, new object[]
                {
                        ConsumConfig,
                        Subscribe,
                        SubscribeAttribute.GroupId
                });
            }
            return services;
        }
    }
}
