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
using Air.Cloud.Core.Extensions;
using Air.Cloud.Modules.Kafka.Model;
using Air.Cloud.Modules.Kafka.Topic;

namespace Air.Cloud.Modules.Kafka.Config
{
    public class KafkaSettingsOptions
    {
        /// <summary>
        /// 集群地址信息
        /// </summary>
        public string ClusterAddress { get; set; }

        /// <summary>
        /// 生产配置信息
        /// </summary>
        public List<ProducerConfigModel> ProducerConfigs { get; set; }

        /// <summary>
        /// 异常消息队列
        /// </summary>
        public ProducerConfigModel ErrorProducerConfig { get; set; }
        /// <summary>
        /// 消费配置信息
        /// </summary>
        public List<ConsumerConfigModel> ConsumerConfigs { get; set; }

        /// <summary>
        /// Topic创建模板信息
        /// </summary>
        public TopicTemplateInfo TopicTemplateInfo;

        /// <summary>
        /// 是否初始化Kafka
        /// </summary>
        public static bool IsInitKafka = false;

        /// <summary>
        /// 根据主题名称获取生产配置
        /// </summary>
        /// <param name="TopicName">主题名称</param>
        /// <returns></returns>
        public ProducerConfigModel GetProducerConfigModelByName(string TopicName)
        {
            ProducerConfigModel producerConfig = ProducerConfigs.FirstOrDefault(s => s.TopicName == TopicName);
            if (producerConfig == null)
            {
                return new ProducerConfigModel()
                {
                    TopicName = TopicName,
                    Config = new Confluent.Kafka.ProducerConfig()
                    {
                        BootstrapServers = ClusterAddress
                    }
                };
            }
            if (producerConfig.Config == null)
            {
                producerConfig.Config = new Confluent.Kafka.ProducerConfig()
                {
                    BootstrapServers = ClusterAddress
                };
            }
            else
            {
                if (producerConfig.Config.BootstrapServers.IsNullOrEmpty())
                {
                    producerConfig.Config.BootstrapServers = ClusterAddress;
                }
            }
            return producerConfig;
        }

        /// <summary>
        /// 根据主题名称获取消费配置
        /// </summary>
        /// <param name="TopicName">主题名称</param>
        /// <returns></returns>
        public ConsumerConfigModel GetConsumerConfigModelByName(string TopicName)
        {
            ConsumerConfigModel consumerConfig = ConsumerConfigs.FirstOrDefault(s => s.TopicName == TopicName);
            if (consumerConfig == null)
            {
                return new ConsumerConfigModel()
                {
                    TopicName = TopicName,
                    Config = new Confluent.Kafka.ConsumerConfig()
                    {
                        BootstrapServers = ClusterAddress
                    }
                };
            }

            if (consumerConfig.Config == null)
            {
                consumerConfig.Config = new Confluent.Kafka.ConsumerConfig()
                {
                    BootstrapServers = ClusterAddress
                };
            }
            else
            {
                if (consumerConfig.Config.BootstrapServers.IsNullOrEmpty())
                {
                    consumerConfig.Config.BootstrapServers = ClusterAddress;
                }
            }
            return consumerConfig;
        }
    }
}
