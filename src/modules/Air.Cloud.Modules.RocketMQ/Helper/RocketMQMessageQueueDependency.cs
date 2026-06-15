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
using Air.Cloud.Core.Standard.MessageQueue.Enums;
using Air.Cloud.Modules.RocketMQ.Config;
using Air.Cloud.Modules.RocketMQ.Contexts;
using Air.Cloud.Modules.RocketMQ.Model;

using Microsoft.Extensions.DependencyInjection;

using Org.Apache.Rocketmq;

using System.Diagnostics;
using System.Text;

namespace Air.Cloud.Modules.RocketMQ.Helper
{
    /// <summary>
    /// <para>zh-cn:RocketMQ 消息队列标准实现，负责发布、订阅、Key 生成、消费 Ack、消费者恢复桥接和消息失败补偿。</para>
    /// <para>en-us:RocketMQ implementation of the message-queue standard. It handles publishing, subscribing, key generation, consume acknowledgement, consumer recovery bridging, and message failure compensation.</para>
    /// </summary>
    public class RocketMQMessageQueueDependency : IMessageQueueStandard
    {
        private static readonly Dictionary<string, Producer> ProducerPool = new();
        private static readonly object ProducerPoolLock = new();

        /// <summary>
        /// <para>zh-cn:获取 RocketMQ 模块配置选项。</para>
        /// <para>en-us:Gets RocketMQ module configuration options.</para>
        /// </summary>
        public RocketMQSettingsOptions RocketMQOptions => AppCore.GetOptions<RocketMQSettingsOptions>();

        /// <summary>
        /// <para>zh-cn:发布消息；该同步标准方法内部调用 RocketMQ 异步发送并等待完成。</para>
        /// <para>en-us:Publishes a message. This synchronous standard method calls the RocketMQ asynchronous send API and waits for completion.</para>
        /// </summary>
        /// <typeparam name="TTopicPublishConfig">
        /// <para>zh-cn:RocketMQ 发布配置类型。</para>
        /// <para>en-us:The RocketMQ publish configuration type.</para>
        /// </typeparam>
        /// <typeparam name="TMessageContentStandard">
        /// <para>zh-cn:消息内容类型。</para>
        /// <para>en-us:The message content type.</para>
        /// </typeparam>
        /// <param name="producerConfigModel">
        /// <para>zh-cn:发布配置模型。</para>
        /// <para>en-us:The publish configuration model.</para>
        /// </param>
        /// <param name="Content">
        /// <para>zh-cn:待发布消息内容。</para>
        /// <para>en-us:The message content to publish.</para>
        /// </param>
        public void Publish<TTopicPublishConfig, TMessageContentStandard>(
            ITopicPublishConfig<TTopicPublishConfig> producerConfigModel,
            TMessageContentStandard Content)
            where TTopicPublishConfig : class
            where TMessageContentStandard : class, new()
        {
            PublishAsync(producerConfigModel, Content).GetAwaiter().GetResult();
        }

        /// <summary>
        /// <para>zh-cn:订阅消息；使用 RocketMQ SimpleConsumer 启动长运行消费循环，业务处理成功后 Ack，失败或超时后触发补偿且不 Ack。</para>
        /// <para>en-us:Subscribes to messages by starting a long-running RocketMQ SimpleConsumer loop. Messages are acknowledged after successful business handling; failures or timeouts trigger compensation and are not acknowledged.</para>
        /// </summary>
        /// <typeparam name="TTopicSubscribeConfig">
        /// <para>zh-cn:RocketMQ 订阅配置类型。</para>
        /// <para>en-us:The RocketMQ subscription configuration type.</para>
        /// </typeparam>
        /// <typeparam name="TMessageContentStandard">
        /// <para>zh-cn:消息内容类型。</para>
        /// <para>en-us:The message content type.</para>
        /// </typeparam>
        /// <param name="subscribeConfig">
        /// <para>zh-cn:订阅配置模型。</para>
        /// <para>en-us:The subscription configuration model.</para>
        /// </param>
        /// <param name="action">
        /// <para>zh-cn:消息处理动作。</para>
        /// <para>en-us:The message handling action.</para>
        /// </param>
        /// <param name="GroupId">
        /// <para>zh-cn:消费者组编号；为空时使用订阅配置中的 ConsumerGroup。</para>
        /// <para>en-us:The consumer group id. When empty, the ConsumerGroup from subscription configuration is used.</para>
        /// </param>
        public void Subscribe<TTopicSubscribeConfig, TMessageContentStandard>(
            ITopicSubscribeConfig<TTopicSubscribeConfig> subscribeConfig,
            Action<TMessageContentStandard> action,
            string GroupId = null)
            where TTopicSubscribeConfig : class
            where TMessageContentStandard : class, new()
        {
            Task.Factory.StartNew(
                () => SubscribeLoopAsync(subscribeConfig, action, GroupId).GetAwaiter().GetResult(),
                TaskCreationOptions.LongRunning);
        }

        /// <summary>
        /// <para>zh-cn:异步发布消息，便于测试和内部复用；外部标准入口仍是 Publish。</para>
        /// <para>en-us:Publishes a message asynchronously for testing and internal reuse. The external standard entry point remains Publish.</para>
        /// </summary>
        /// <typeparam name="TTopicPublishConfig">
        /// <para>zh-cn:RocketMQ 发布配置类型。</para>
        /// <para>en-us:The RocketMQ publish configuration type.</para>
        /// </typeparam>
        /// <typeparam name="TMessageContentStandard">
        /// <para>zh-cn:消息内容类型。</para>
        /// <para>en-us:The message content type.</para>
        /// </typeparam>
        /// <param name="producerConfigModel">
        /// <para>zh-cn:发布配置模型。</para>
        /// <para>en-us:The publish configuration model.</para>
        /// </param>
        /// <param name="content">
        /// <para>zh-cn:待发布消息内容。</para>
        /// <para>en-us:The message content to publish.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:RocketMQ 发送回执。</para>
        /// <para>en-us>The RocketMQ send receipt.</para>
        /// </returns>
        public async Task<ISendReceipt> PublishAsync<TTopicPublishConfig, TMessageContentStandard>(
            ITopicPublishConfig<TTopicPublishConfig> producerConfigModel,
            TMessageContentStandard content)
            where TTopicPublishConfig : class
            where TMessageContentStandard : class, new()
        {
            EnsureProducerConfig(producerConfigModel);
            var config = producerConfigModel.Config as RocketMQProducerConfig;
            if (config == null)
            {
                throw new InvalidOperationException($"RocketMQ producer config type must be {nameof(RocketMQProducerConfig)}.");
            }

            var producer = await GetOrCreateProducerAsync(producerConfigModel.TopicName, config);
            var message = BuildMessage(producerConfigModel.TopicName, config, producerConfigModel.KeyType, content);
            return await producer.Send(message);
        }

        private async Task SubscribeLoopAsync<TTopicSubscribeConfig, TMessageContentStandard>(
            ITopicSubscribeConfig<TTopicSubscribeConfig> subscribeConfig,
            Action<TMessageContentStandard> action,
            string groupId = null)
            where TTopicSubscribeConfig : class
            where TMessageContentStandard : class, new()
        {
            EnsureConsumerConfig(subscribeConfig, groupId);
            var config = subscribeConfig.Config as RocketMQConsumerConfig;
            if (config == null)
            {
                throw new InvalidOperationException($"RocketMQ consumer config type must be {nameof(RocketMQConsumerConfig)}.");
            }

            var recovery = GetConsumerRecoveryStandard();
            var compensation = GetFailureCompensationStandard();
            var executionOptions = GetConsumerExecutionOptions();
            var reconnectDelay = TimeSpan.FromSeconds(3);
            var resolvedGroupId = ResolveGroupId(config, groupId);

            while (true)
            {
                SimpleConsumer consumer = null;
                try
                {
                    consumer = await BuildSimpleConsumerAsync(subscribeConfig.TopicName, config, resolvedGroupId);
                    NotifyConsumerRecovered(recovery, subscribeConfig.TopicName, resolvedGroupId, consumer, null, null);

                    while (true)
                    {
                        var messages = await consumer.Receive(config.BatchSize, config.InvisibleDuration);
                        foreach (var message in messages)
                        {
                            var handled = HandleMessage(
                                subscribeConfig.TopicName,
                                message,
                                action,
                                compensation,
                                executionOptions);

                            if (handled)
                            {
                                await consumer.Ack(message);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    ReportConsumerLoopException(
                        recovery,
                        compensation,
                        subscribeConfig.TopicName,
                        resolvedGroupId,
                        consumer,
                        ex,
                        ex);
                }
                finally
                {
                    consumer?.Dispose();
                }

                await Task.Delay(reconnectDelay);
            }
        }

        private Message BuildMessage<TMessageContentStandard>(
            string topicName,
            RocketMQProducerConfig config,
            Type keyType,
            TMessageContentStandard content)
            where TMessageContentStandard : class, new()
        {
            keyType ??= typeof(string);
            if (keyType != typeof(string))
            {
                throw new InvalidOperationException("RocketMQ message key type must be string because RocketMQ.Client exposes message keys as string values.");
            }

            var context = new RocketMQMessageQueueKeyGenerationContext()
            {
                TopicName = topicName,
                MessageContent = content,
                PublishConfig = config,
                KeyType = keyType
            };

            var builder = new Message.Builder()
                .SetTopic(topicName)
                .SetBody(Encoding.UTF8.GetBytes(AppRealization.JSON.Serialize(content)))
                .SetKeys(GenerateKey(context));

            if (!string.IsNullOrWhiteSpace(config.Tag))
            {
                builder.SetTag(config.Tag);
            }

            if (!string.IsNullOrWhiteSpace(config.MessageGroup))
            {
                builder.SetMessageGroup(config.MessageGroup);
            }

            foreach (var property in config.Properties ?? new Dictionary<string, string>())
            {
                builder.AddProperty(property.Key, property.Value);
            }

            return builder.Build();
        }

        private bool HandleMessage<TMessageContentStandard>(
            string topicName,
            MessageView message,
            Action<TMessageContentStandard> action,
            IMessageQueueFailureCompensationStandard compensation,
            IMessageQueueConsumerExecutionOptions executionOptions)
            where TMessageContentStandard : class, new()
        {
            var stopwatch = Stopwatch.StartNew();
            TMessageContentStandard messageContent = null;

            try
            {
                messageContent = AppRealization.JSON.Deserialize<TMessageContentStandard>(Encoding.UTF8.GetString(message.Body));
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                NotifyMessageFailed(
                    compensation,
                    topicName,
                    null,
                    ex,
                    false,
                    executionOptions?.MessageHandlingTimeout,
                    stopwatch.Elapsed,
                    MessageQueueFailureReason.DeserializeFailed,
                    message);
                return false;
            }

            try
            {
                var timeout = executionOptions?.MessageHandlingTimeout;
                var handlingTask = Task.Run(() => action.Invoke(messageContent));
                if (timeout.HasValue && !handlingTask.Wait(timeout.Value))
                {
                    stopwatch.Stop();
                    NotifyMessageFailed(
                        compensation,
                        topicName,
                        messageContent,
                        new TimeoutException($"RocketMQ message handling timed out after {timeout.Value}."),
                        true,
                        timeout,
                        stopwatch.Elapsed,
                        MessageQueueFailureReason.Timeout,
                        message);
                    return false;
                }

                handlingTask.GetAwaiter().GetResult();
                stopwatch.Stop();
                return true;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                NotifyMessageFailed(
                    compensation,
                    topicName,
                    messageContent,
                    ex,
                    false,
                    executionOptions?.MessageHandlingTimeout,
                    stopwatch.Elapsed,
                    MessageQueueFailureReason.Exception,
                    message);
                return false;
            }
        }

        private async Task<Producer> GetOrCreateProducerAsync(string topicName, RocketMQProducerConfig config)
        {
            var cacheKey = BuildProducerCacheKey(topicName, config);
            lock (ProducerPoolLock)
            {
                if (ProducerPool.TryGetValue(cacheKey, out var cachedProducer))
                {
                    return cachedProducer;
                }
            }

            var producer = await new Producer.Builder()
                .SetClientConfig(BuildClientConfig(config))
                .SetTopics(topicName)
                .SetMaxAttempts(config.MaxAttempts)
                .Build();

            lock (ProducerPoolLock)
            {
                if (ProducerPool.TryGetValue(cacheKey, out var cachedProducer))
                {
                    producer.Dispose();
                    return cachedProducer;
                }

                ProducerPool[cacheKey] = producer;
                return producer;
            }
        }

        private async Task<SimpleConsumer> BuildSimpleConsumerAsync(string topicName, RocketMQConsumerConfig config, string groupId)
        {
            var expressions = new Dictionary<string, FilterExpression>()
            {
                { topicName, new FilterExpression(config.FilterExpression ?? "*", config.ExpressionType) }
            };

            return await new SimpleConsumer.Builder()
                .SetClientConfig(BuildClientConfig(config))
                .SetConsumerGroup(groupId)
                .SetAwaitDuration(config.AwaitDuration)
                .SetSubscriptionExpression(expressions)
                .Build();
        }

        private ClientConfig BuildClientConfig(RocketMQClientConfig config)
        {
            if (string.IsNullOrWhiteSpace(config.Endpoints))
            {
                throw new InvalidOperationException("RocketMQ Endpoints is required.");
            }

            var builder = new ClientConfig.Builder()
                .SetEndpoints(config.Endpoints)
                .EnableSsl(config.SslEnabled);

            if (config.RequestTimeout.HasValue)
            {
                builder.SetRequestTimeout(config.RequestTimeout.Value);
            }

            if (!string.IsNullOrWhiteSpace(config.AccessKey) && !string.IsNullOrWhiteSpace(config.AccessSecret))
            {
                builder.SetCredentialsProvider(new RocketMQSessionCredentialsProvider(config.AccessKey, config.AccessSecret, config.SecurityToken));
            }

            return builder.Build();
        }

        private void EnsureProducerConfig<TTopicPublishConfig>(ITopicPublishConfig<TTopicPublishConfig> producerConfigModel)
            where TTopicPublishConfig : class
        {
            if (producerConfigModel.Config != null)
            {
                return;
            }

            var config = RocketMQOptions?.CreateProducerConfig() as TTopicPublishConfig;
            if (config == null)
            {
                throw new InvalidOperationException($"RocketMQ producer config type must be {nameof(RocketMQProducerConfig)}.");
            }

            producerConfigModel.Config = config;
        }

        private void EnsureConsumerConfig<TTopicSubscribeConfig>(ITopicSubscribeConfig<TTopicSubscribeConfig> subscribeConfig, string groupId)
            where TTopicSubscribeConfig : class
        {
            if (subscribeConfig.Config == null)
            {
                var config = RocketMQOptions?.CreateConsumerConfig() as TTopicSubscribeConfig;
                if (config == null)
                {
                    throw new InvalidOperationException($"RocketMQ consumer config type must be {nameof(RocketMQConsumerConfig)}.");
                }

                subscribeConfig.Config = config;
            }

            var consumerConfig = subscribeConfig.Config as RocketMQConsumerConfig;
            if (consumerConfig == null)
            {
                throw new InvalidOperationException($"RocketMQ consumer config type must be {nameof(RocketMQConsumerConfig)}.");
            }

            if (string.IsNullOrWhiteSpace(ResolveGroupId(consumerConfig, groupId)))
            {
                throw new InvalidOperationException("RocketMQ ConsumerGroup is required.");
            }
        }

        private string GenerateKey(IMessageQueueKeyGenerationContext context)
        {
            var generator = AppCore.GetService<IMessageQueueKeyGenerator<string>>();
            if (generator != null)
            {
                return generator.Generate(context);
            }

            var generatorObject = GetKeyGenerators().FirstOrDefault(generator => generator.GetKeyType() == typeof(string));
            if (generatorObject != null)
            {
                return (string)generatorObject.Generate(context);
            }

            throw new InvalidOperationException("No RocketMQ string message queue key generator was registered.");
        }

        private IEnumerable<IMessageQueueKeyGenerator> GetKeyGenerators()
        {
            var serviceProvider = AppCore.GetServiceProvider(typeof(IMessageQueueKeyGenerator));
            return serviceProvider?.GetServices<IMessageQueueKeyGenerator>() ?? Enumerable.Empty<IMessageQueueKeyGenerator>();
        }

        private IMessageQueueConsumerRecoveryStandard GetConsumerRecoveryStandard()
        {
            return AppCore.GetService<IMessageQueueConsumerRecoveryStandard>();
        }

        private IMessageQueueFailureCompensationStandard GetFailureCompensationStandard()
        {
            return AppCore.GetService<IMessageQueueFailureCompensationStandard>();
        }

        private IMessageQueueConsumerExecutionOptions GetConsumerExecutionOptions()
        {
            var options = AppCore.GetService<IMessageQueueConsumerExecutionOptions>();
            if (options == null)
            {
                throw new InvalidOperationException($"No {nameof(IMessageQueueConsumerExecutionOptions)} was registered.");
            }

            return options;
        }

        private void ReportConsumerLoopException(
            IMessageQueueConsumerRecoveryStandard recovery,
            IMessageQueueFailureCompensationStandard compensation,
            string topicName,
            string groupId,
            object consumer,
            object nativeEventArgs,
            Exception exception)
        {
            try
            {
                NotifyConsumerInterrupted(recovery, topicName, groupId, consumer, nativeEventArgs, MessageQueueConsumerInterruptedReason.ConnectionError, exception);
            }
            catch (Exception recoveryException)
            {
                Console.Error.WriteLine($"RocketMQ consumer interruption callback failed. Topic={topicName}, GroupId={groupId}, Exception={recoveryException}");
            }

            try
            {
                NotifyMessageFailed(
                    compensation,
                    topicName,
                    null,
                    exception,
                    false,
                    null,
                    TimeSpan.Zero,
                    MessageQueueFailureReason.ConsumerInterrupted,
                    nativeEventArgs);
            }
            catch (Exception compensationException)
            {
                Console.Error.WriteLine($"RocketMQ consumer failure compensation callback failed. Topic={topicName}, GroupId={groupId}, Exception={compensationException}");
            }
        }

        private void NotifyConsumerInterrupted(
            IMessageQueueConsumerRecoveryStandard recovery,
            string topicName,
            string groupId,
            object consumer,
            object nativeEventArgs,
            MessageQueueConsumerInterruptedReason reason,
            Exception exception)
        {
            recovery?.OnConsumerInterruptedAsync(new RocketMQMessageQueueConsumerContext()
            {
                TopicName = topicName,
                GroupId = groupId,
                InterruptedReason = reason,
                Exception = exception,
                NativeConsumer = consumer,
                NativeEventArgs = nativeEventArgs
            }).GetAwaiter().GetResult();
        }

        private void NotifyConsumerRecovered(
            IMessageQueueConsumerRecoveryStandard recovery,
            string topicName,
            string groupId,
            object consumer,
            object nativeEventArgs,
            Exception exception)
        {
            recovery?.OnConsumerRecoveredAsync(new RocketMQMessageQueueConsumerContext()
            {
                TopicName = topicName,
                GroupId = groupId,
                InterruptedReason = MessageQueueConsumerInterruptedReason.Unknown,
                Exception = exception,
                NativeConsumer = consumer,
                NativeEventArgs = nativeEventArgs
            }).GetAwaiter().GetResult();
        }

        private void NotifyMessageFailed(
            IMessageQueueFailureCompensationStandard compensation,
            string topicName,
            object messageContent,
            Exception exception,
            bool isTimeout,
            TimeSpan? handlingTimeout,
            TimeSpan handlingElapsed,
            MessageQueueFailureReason failureReason,
            object nativeMessage)
        {
            var retryCount = nativeMessage is MessageView message ? message.DeliveryAttempt : 0;
            compensation?.OnFailedAsync(new RocketMQMessageQueueFailureContext()
            {
                TopicName = topicName,
                MessageContent = messageContent,
                Exception = exception,
                IsTimeout = isTimeout,
                HandlingTimeout = handlingTimeout,
                HandlingElapsed = handlingElapsed,
                RetryCount = retryCount,
                FailureReason = failureReason,
                NativeMessage = nativeMessage
            }).GetAwaiter().GetResult();
        }

        private string ResolveGroupId(RocketMQConsumerConfig config, string groupId)
        {
            return string.IsNullOrWhiteSpace(groupId) ? config.ConsumerGroup : groupId;
        }

        private string BuildProducerCacheKey(string topicName, RocketMQProducerConfig config)
        {
            return $"{topicName}_{config.Endpoints}_{config.SslEnabled}_{config.AccessKey}_{config.MaxAttempts}";
        }

        private sealed class RocketMQSessionCredentialsProvider : ISessionCredentialsProvider
        {
            public RocketMQSessionCredentialsProvider(string accessKey, string accessSecret, string securityToken)
            {
                SessionCredentials = string.IsNullOrWhiteSpace(securityToken)
                    ? new SessionCredentials(accessKey, accessSecret)
                    : new SessionCredentials(accessKey, accessSecret, securityToken);
            }

            public SessionCredentials SessionCredentials { get; }
        }
    }
}
