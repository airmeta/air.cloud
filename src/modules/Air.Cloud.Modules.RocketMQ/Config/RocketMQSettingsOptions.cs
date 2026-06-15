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
using Air.Cloud.Modules.RocketMQ.Model;

namespace Air.Cloud.Modules.RocketMQ.Config
{
    /// <summary>
    /// <para>zh-cn:RocketMQ 模块配置，提供全局 Endpoint、凭证、生产者配置和消费者配置的默认值。</para>
    /// <para>en-us:RocketMQ module settings that provide global defaults for endpoints, credentials, producer configurations, and consumer configurations.</para>
    /// </summary>
    public class RocketMQSettingsOptions
    {
        /// <summary>
        /// <para>zh-cn:RocketMQ Proxy Endpoint 地址；RocketMQ 5.x gRPC SDK 需要连接 Proxy，例如 127.0.0.1:8081。</para>
        /// <para>en-us:RocketMQ Proxy endpoint. The RocketMQ 5.x gRPC SDK connects to Proxy, for example 127.0.0.1:8081.</para>
        /// </summary>
        public string Endpoints { get; set; }

        /// <summary>
        /// <para>zh-cn:是否启用 TLS/SSL；默认 false，内网测试环境通常保持关闭。</para>
        /// <para>en-us:Whether TLS/SSL is enabled. The default is false, which is typical for internal test environments.</para>
        /// </summary>
        public bool SslEnabled { get; set; }

        /// <summary>
        /// <para>zh-cn:请求超时时间；未配置时使用 RocketMQ 客户端默认值。</para>
        /// <para>en-us:Request timeout. When not configured, the RocketMQ client default is used.</para>
        /// </summary>
        public TimeSpan? RequestTimeout { get; set; }

        /// <summary>
        /// <para>zh-cn:访问密钥；为空时不注册凭证提供方。</para>
        /// <para>en-us:Access key. When empty, no credentials provider is registered.</para>
        /// </summary>
        public string AccessKey { get; set; }

        /// <summary>
        /// <para>zh-cn:访问密钥 Secret；为空时不注册凭证提供方。</para>
        /// <para>en-us:Access secret. When empty, no credentials provider is registered.</para>
        /// </summary>
        public string AccessSecret { get; set; }

        /// <summary>
        /// <para>zh-cn:临时安全令牌；仅在 AccessKey 与 AccessSecret 同时存在时生效。</para>
        /// <para>en-us:Temporary security token. It is effective only when both AccessKey and AccessSecret are present.</para>
        /// </summary>
        public string SecurityToken { get; set; }

        /// <summary>
        /// <para>zh-cn:生产者配置集合，按 TopicName 匹配。</para>
        /// <para>en-us:Producer configuration collection matched by TopicName.</para>
        /// </summary>
        public List<RocketMQProducerConfigModel> ProducerConfigs { get; set; }

        /// <summary>
        /// <para>zh-cn:消费者配置集合，按 TopicName 匹配。</para>
        /// <para>en-us:Consumer configuration collection matched by TopicName.</para>
        /// </summary>
        public List<RocketMQConsumerConfigModel> ConsumerConfigs { get; set; }

        /// <summary>
        /// <para>zh-cn:根据主题名称获取生产配置；未配置时创建带全局默认值的生产配置。</para>
        /// <para>en-us:Gets the producer configuration by topic name. When missing, a producer configuration with global defaults is created.</para>
        /// </summary>
        /// <param name="topicName">
        /// <para>zh-cn:主题名称。</para>
        /// <para>en-us:Topic name.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:RocketMQ 生产配置模型。</para>
        /// <para>en-us:RocketMQ producer configuration model.</para>
        /// </returns>
        public RocketMQProducerConfigModel GetProducerConfigModelByName(string topicName)
        {
            var producerConfig = ProducerConfigs?.FirstOrDefault(s => s.TopicName == topicName);
            if (producerConfig == null)
            {
                producerConfig = new RocketMQProducerConfigModel()
                {
                    TopicName = topicName,
                    Config = CreateProducerConfig()
                };
            }

            producerConfig.TopicName = topicName;
            producerConfig.Config = FillProducerDefaults(producerConfig.Config);
            return producerConfig;
        }

        /// <summary>
        /// <para>zh-cn:根据主题名称获取消费配置；未配置时创建带全局默认值的消费配置。</para>
        /// <para>en-us:Gets the consumer configuration by topic name. When missing, a consumer configuration with global defaults is created.</para>
        /// </summary>
        /// <param name="topicName">
        /// <para>zh-cn:主题名称。</para>
        /// <para>en-us:Topic name.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:RocketMQ 消费配置模型。</para>
        /// <para>en-us:RocketMQ consumer configuration model.</para>
        /// </returns>
        public RocketMQConsumerConfigModel GetConsumerConfigModelByName(string topicName)
        {
            var consumerConfig = ConsumerConfigs?.FirstOrDefault(s => s.TopicName == topicName);
            if (consumerConfig == null)
            {
                consumerConfig = new RocketMQConsumerConfigModel()
                {
                    TopicName = topicName,
                    Config = CreateConsumerConfig()
                };
            }

            consumerConfig.TopicName = topicName;
            consumerConfig.Config = FillConsumerDefaults(consumerConfig.Config);
            return consumerConfig;
        }

        /// <summary>
        /// <para>zh-cn:创建全局默认生产配置，供 Topic 未显式配置时使用。</para>
        /// <para>en-us:Creates a global default producer configuration used when a topic is not explicitly configured.</para>
        /// </summary>
        /// <returns>
        /// <para>zh-cn:RocketMQ 生产配置。</para>
        /// <para>en-us:RocketMQ producer configuration.</para>
        /// </returns>
        public RocketMQProducerConfig CreateProducerConfig()
        {
            return FillProducerDefaults(new RocketMQProducerConfig());
        }

        /// <summary>
        /// <para>zh-cn:创建全局默认消费配置，供 Topic 未显式配置时使用。</para>
        /// <para>en-us:Creates a global default consumer configuration used when a topic is not explicitly configured.</para>
        /// </summary>
        /// <returns>
        /// <para>zh-cn:RocketMQ 消费配置。</para>
        /// <para>en-us:RocketMQ consumer configuration.</para>
        /// </returns>
        public RocketMQConsumerConfig CreateConsumerConfig()
        {
            return FillConsumerDefaults(new RocketMQConsumerConfig());
        }

        private RocketMQProducerConfig FillProducerDefaults(RocketMQProducerConfig config)
        {
            config ??= new RocketMQProducerConfig();
            FillClientDefaults(config);
            return config;
        }

        private RocketMQConsumerConfig FillConsumerDefaults(RocketMQConsumerConfig config)
        {
            config ??= new RocketMQConsumerConfig();
            FillClientDefaults(config);
            return config;
        }

        private void FillClientDefaults(RocketMQClientConfig config)
        {
            if (config.Endpoints.IsNullOrEmpty())
            {
                config.Endpoints = Endpoints;
            }

            if (!config.RequestTimeout.HasValue)
            {
                config.RequestTimeout = RequestTimeout;
            }

            if (config.AccessKey.IsNullOrEmpty())
            {
                config.AccessKey = AccessKey;
            }

            if (config.AccessSecret.IsNullOrEmpty())
            {
                config.AccessSecret = AccessSecret;
            }

            if (config.SecurityToken.IsNullOrEmpty())
            {
                config.SecurityToken = SecurityToken;
            }

            config.SslEnabled = config.SslEnabled || SslEnabled;
        }
    }
}
