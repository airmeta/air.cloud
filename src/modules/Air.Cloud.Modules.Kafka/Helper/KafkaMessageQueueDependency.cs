/*
 * Copyright (c) 2024 星曳数据
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
using Air.Cloud.Modules.Kafka.Model;
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

        #region 生产与消费
        public void Publish<TTopicPublishConfig,TMessageContentStandard>
                (ITopicPublishConfig<TTopicPublishConfig> producerConfigModel, TMessageContentStandard Content) 
                where TTopicPublishConfig:class
                where TMessageContentStandard : class, new()
        {
            if (producerConfigModel.Config == null)
            {
                var Config = new ProducerConfig()
                {
                    BootstrapServers = KafkaClusterOptions.ClusterAddress,
                } as TTopicPublishConfig;
                if (Config == null)
                {
                    AppRealization.Output.Error(new Exception("推送配置错误"));
                }
                else
                {
                    producerConfigModel.Config = Config;
                }
            }
            IProducer<int, string> producer = null;
            if (producer == null)
            {
                ProducerBuilder<int, string> produce = new ProducerBuilder<int, string>(producerConfigModel.Config as ProducerConfig);
                //produce.SetErrorHandler(ErrorProducerHandler);
                producer=produce.Build();
            }
            producer.Produce(producerConfigModel.TopicName, new Message<int, string>()
            {
                Key = KafkaRandomKey.GetRandom(),
                Value = AppRealization.JSON.Serialize(Content),
                Timestamp = new Timestamp(DateTime.Now)
            });
            producer.Flush(new TimeSpan(0, 0, 5));
        }

        public void Subscribe<TTopicSubscribeConfig, TMessageContentStandard>
            (ITopicSubscribeConfig<TTopicSubscribeConfig> subscribeConfig, 
            Action<TMessageContentStandard> action,string GroupId=null)
               where TTopicSubscribeConfig : class
                where TMessageContentStandard : class, new()
        {
          
            if (subscribeConfig.Config == null)
            {
                var Config = new ConsumerConfig()
                {
                    GroupId= GroupId,
                    BootstrapServers = KafkaClusterOptions.ClusterAddress
                } as TTopicSubscribeConfig;
                if (Config == null)
                {
                    AppRealization.Output.Error(new Exception("订阅配置错误"));
                }
                else
                {
                    subscribeConfig.Config = Config;
                }
            }
            ConsumerConfig config = subscribeConfig.Config as ConsumerConfig;
            //消费数据
            Task.Factory.StartNew(() =>
            {
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
                            action.Invoke(AppRealization.JSON.Deserialize<TMessageContentStandard>(ConsumData.Message.Value));
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
