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
using Air.Cloud.Core.Standard.MessageQueue.Config;
using Air.Cloud.Modules.Kafka.Config;
using Air.Cloud.Modules.Kafka.Pool;
using Air.Cloud.Modules.Kafka.Utils;

using Confluent.Kafka;

namespace Air.Cloud.Modules.Kafka.Helper
{
    /// <summary>
    /// kafka 工具类
    /// </summary>
    public class KafkaMessageQueueDependency :IMessageQueueStandard
    {
        public KafkaSettingsOptions KafkaClusterOptions => AppCore.GetOptions<KafkaSettingsOptions>();

        /// <summary>
        /// 生产者配置池
        /// </summary>
        private static ProducerConfigPool producerConfigPool = new ProducerConfigPool();

        /// <summary>
        /// 生产者池
        /// </summary>
        private static ProducerPool producerPool=new ProducerPool();


        #region 生产与消费
        public void Publish<TTopicPublishConfig,TMessageContentStandard>
                (ITopicPublishConfig<TTopicPublishConfig> producerConfigModel, TMessageContentStandard Content) 
                where TTopicPublishConfig:class
                where TMessageContentStandard : class, new()
        {
            if (producerConfigModel.Config == null)
            {
                var Config = producerConfigPool.Get(producerConfigModel.TopicName);
                if (Config==null)
                {
                    ProducerConfig DefaultProducerConfig = new ProducerConfig()
                    {
                        BootstrapServers = KafkaClusterOptions.ClusterAddress,
                    };
                    producerConfigModel.Config = DefaultProducerConfig as TTopicPublishConfig;
                    producerConfigPool.Set((ITopicPublishConfig<ProducerConfig>)producerConfigModel);
                }
                else
                {
                    producerConfigModel = (ITopicPublishConfig<TTopicPublishConfig>)Config;
                }
            }
            Tuple<string, IProducer<int, string>> producerInstance = producerPool.Get(producerConfigModel.TopicName);
            if (producerInstance == null)
            {
                //重新构建生产者
                ProducerBuilder<int, string> produce = new ProducerBuilder<int, string>(producerConfigModel.Config as ProducerConfig);
                producerInstance = new Tuple<string, IProducer<int, string>>(producerConfigModel.TopicName, produce.Build());
                producerPool.Set(producerInstance);
            }
            producerInstance.Item2.Produce(producerInstance.Item1, new Message<int, string>()
            {
                Key = KafkaRandomKey.GetRandom(),
                Value = AppRealization.JSON.Serialize(Content),
                Timestamp = new Timestamp(DateTime.Now)
            });
            producerInstance.Item2.Flush(new TimeSpan(0, 0, 5));
        }

        public void Subscribe<TTopicSubscribeConfig, TMessageContentStandard>
            (ITopicSubscribeConfig<TTopicSubscribeConfig> subscribeConfig, 
                Action<TMessageContentStandard> action,string GroupId=null)
               where TTopicSubscribeConfig : class
                where TMessageContentStandard : class,new()
        {
            //消费数据
            Task.Factory.StartNew(() =>
            {
                if (subscribeConfig.Config == null)
                {
                    var Config = new ConsumerConfig()
                    {
                        GroupId = GroupId,
                        BootstrapServers = KafkaClusterOptions.ClusterAddress
                    } as TTopicSubscribeConfig;
                    if (Config == null) AppRealization.Output.Error(new Exception("订阅配置错误"));
                    subscribeConfig.Config = Config;
                }
                ConsumerConfig config = subscribeConfig.Config as ConsumerConfig;
                ConsumerBuilder<int, string> consume = new ConsumerBuilder<int, string>(config);
                config.EnableAutoCommit = true;
                var EnableCommit = config.EnableAutoCommit.HasValue && config.EnableAutoCommit.Value;
                using (var consumer = consume.Build())
                {
                    consumer.Subscribe(subscribeConfig.TopicName);
                    while (true)
                    {
                        var ConsumData = consumer.Consume();
                        try
                        {
                            TMessageContentStandard messageContentStandard=AppRealization.JSON.Deserialize<TMessageContentStandard>(ConsumData.Message.Value);
                            action.Invoke(messageContentStandard);
                            if (!EnableCommit)
                            {
                                //手动提交消费配置
                                consumer.Commit(ConsumData);
                            }
                        }
                        catch (Exception ex)
                        {
                            AppRealization.Output.Error(ex);
                        }
                    }
                }
            }, TaskCreationOptions.LongRunning);
        }
        #endregion
    }
}
