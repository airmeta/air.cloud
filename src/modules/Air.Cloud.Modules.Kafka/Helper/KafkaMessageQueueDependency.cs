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
using Air.Cloud.Modules.Kafka.Config;
using Air.Cloud.Modules.Kafka.Contexts;
using Air.Cloud.Modules.Kafka.Pool;

using Confluent.Kafka;

using Microsoft.Extensions.DependencyInjection;

using System.Diagnostics;
using System.Reflection;

namespace Air.Cloud.Modules.Kafka.Helper
{
    /// <summary>
    /// <para>zh-cn:Kafka 消息队列标准实现，负责发布、订阅、Key 类型选择、消费者恢复桥接和单条消息失败补偿。</para>
    /// <para>en-us:Kafka implementation of the message-queue standard. It handles publishing, subscribing, key type selection, consumer recovery bridging, and per-message failure compensation.</para>
    /// </summary>
    public class KafkaMessageQueueDependency : IMessageQueueStandard
    {
        /// <summary>
        /// <para>zh-cn:获取 Kafka 集群配置选项。</para>
        /// <para>en-us:Gets Kafka cluster configuration options.</para>
        /// </summary>
        public KafkaSettingsOptions KafkaClusterOptions => AppCore.GetOptions<KafkaSettingsOptions>();

        /// <summary>
        /// <para>zh-cn:生产者配置池。</para>
        /// <para>en-us:Producer configuration pool.</para>
        /// </summary>
        private static ProducerConfigPool producerConfigPool = new ProducerConfigPool();

        /// <summary>
        /// <para>zh-cn:生产者池；同一个 Topic 在不同 KeyType 下会缓存为不同的 Kafka Producer。</para>
        /// <para>en-us:Producer pool. The same topic is cached as different Kafka producers when different KeyType values are used.</para>
        /// </summary>
        private static ProducerPool producerPool = new ProducerPool();

        #region 生产与消费

        /// <summary>
        /// <para>zh-cn:发布消息；方法签名不暴露 TKey，Kafka 模块会根据 producerConfigModel.KeyType 选择底层 ProducerBuilder&lt;TKey, string&gt;。</para>
        /// <para>en-us:Publishes a message. The method signature does not expose TKey. The Kafka module selects the underlying ProducerBuilder&lt;TKey, string&gt; from producerConfigModel.KeyType.</para>
        /// </summary>
        /// <typeparam name="TTopicPublishConfig">
        /// <para>zh-cn:Kafka 发布配置类型。</para>
        /// <para>en-us:The Kafka publish configuration type.</para>
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
        public void Publish<TTopicPublishConfig, TMessageContentStandard>
            (ITopicPublishConfig<TTopicPublishConfig> producerConfigModel, TMessageContentStandard Content)
            where TTopicPublishConfig : class
            where TMessageContentStandard : class, new()
        {
            var keyType = ResolveKeyType(producerConfigModel.KeyType);
            producerConfigModel.KeyType = keyType;
            InvokeGenericMethod(
                nameof(PublishInternal),
                new[] { keyType, typeof(TTopicPublishConfig), typeof(TMessageContentStandard) },
                producerConfigModel,
                Content);
        }

        /// <summary>
        /// <para>zh-cn:订阅消息；方法签名不暴露 TKey，Kafka 模块会根据 subscribeConfig.KeyType 选择底层 ConsumerBuilder&lt;TKey, string&gt;。</para>
        /// <para>en-us:Subscribes to messages. The method signature does not expose TKey. The Kafka module selects the underlying ConsumerBuilder&lt;TKey, string&gt; from subscribeConfig.KeyType.</para>
        /// </summary>
        /// <typeparam name="TTopicSubscribeConfig">
        /// <para>zh-cn:Kafka 订阅配置类型。</para>
        /// <para>en-us:The Kafka subscription configuration type.</para>
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
        /// <para>zh-cn:消费者组编号。</para>
        /// <para>en-us:The consumer group id.</para>
        /// </param>
        public void Subscribe<TTopicSubscribeConfig, TMessageContentStandard>
            (ITopicSubscribeConfig<TTopicSubscribeConfig> subscribeConfig,
                Action<TMessageContentStandard> action, string GroupId = null)
            where TTopicSubscribeConfig : class
            where TMessageContentStandard : class, new()
        {
            var keyType = ResolveKeyType(subscribeConfig.KeyType);
            subscribeConfig.KeyType = keyType;
            InvokeGenericMethod(
                nameof(SubscribeInternal),
                new[] { keyType, typeof(TTopicSubscribeConfig), typeof(TMessageContentStandard) },
                subscribeConfig,
                action,
                GroupId);
        }

        /// <summary>
        /// <para>zh-cn:使用运行时 KeyType 调用内部强类型泛型方法。</para>
        /// <para>en-us:Invokes an internal strongly typed generic method with a runtime KeyType.</para>
        /// </summary>
        /// <param name="methodName">
        /// <para>zh-cn:内部方法名称。</para>
        /// <para>en-us:The internal method name.</para>
        /// </param>
        /// <param name="genericTypes">
        /// <para>zh-cn:内部泛型方法需要的泛型类型参数。</para>
        /// <para>en-us:The generic type arguments required by the internal generic method.</para>
        /// </param>
        /// <param name="parameters">
        /// <para>zh-cn:调用参数。</para>
        /// <para>en-us:The invocation parameters.</para>
        /// </param>
        private void InvokeGenericMethod(string methodName, Type[] genericTypes, params object[] parameters)
        {
            var method = GetType()
                .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)
                ?.MakeGenericMethod(genericTypes);

            if (method == null)
            {
                throw new MissingMethodException(nameof(KafkaMessageQueueDependency), methodName);
            }

            method.Invoke(this, parameters);
        }

        /// <summary>
        /// <para>zh-cn:发布消息的强类型内部实现。</para>
        /// <para>en-us:Strongly typed internal implementation for publishing messages.</para>
        /// </summary>
        /// <typeparam name="TKey">
        /// <para>zh-cn:Kafka 消息 Key 类型。</para>
        /// <para>en-us:The Kafka message key type.</para>
        /// </typeparam>
        /// <typeparam name="TTopicPublishConfig">
        /// <para>zh-cn:发布配置类型。</para>
        /// <para>en-us:The publish configuration type.</para>
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
        private void PublishInternal<TKey, TTopicPublishConfig, TMessageContentStandard>
            (ITopicPublishConfig<TTopicPublishConfig> producerConfigModel, TMessageContentStandard Content)
            where TTopicPublishConfig : class
            where TMessageContentStandard : class, new()
        {
            EnsureProducerConfig(producerConfigModel);

            var cacheKey = BuildProducerCacheKey(producerConfigModel.TopicName, typeof(TKey));
            var producerInstance = producerPool.Get(cacheKey) as IProducer<TKey, string>;
            if (producerInstance == null)
            {
                // 重新构建生产者。
                // Rebuild producer instance.
                ProducerBuilder<TKey, string> produce = new ProducerBuilder<TKey, string>(producerConfigModel.Config as ProducerConfig);
                producerInstance = produce.Build();
                producerPool.Set(cacheKey, producerInstance);
            }

            var context = new KafkaMessageQueueKeyGenerationContext()
            {
                TopicName = producerConfigModel.TopicName,
                MessageContent = Content,
                PublishConfig = producerConfigModel.Config,
                KeyType = typeof(TKey)
            };

            producerInstance.Produce(producerConfigModel.TopicName, new Message<TKey, string>()
            {
                Key = GenerateKey<TKey>(context),
                Value = AppRealization.JSON.Serialize(Content),
                Timestamp = new Timestamp(DateTime.Now)
            });
            producerInstance.Flush(new TimeSpan(0, 0, 5));
        }

        /// <summary>
        /// <para>zh-cn:订阅消息的强类型内部实现；外层 supervisor 会在消费循环异常退出后重建 consumer。</para>
        /// <para>en-us:Strongly typed internal implementation for subscribing to messages. The outer supervisor rebuilds the consumer after the consume loop exits due to exceptions.</para>
        /// </summary>
        /// <typeparam name="TKey">
        /// <para>zh-cn:Kafka 消息 Key 类型。</para>
        /// <para>en-us:The Kafka message key type.</para>
        /// </typeparam>
        /// <typeparam name="TTopicSubscribeConfig">
        /// <para>zh-cn:订阅配置类型。</para>
        /// <para>en-us:The subscription configuration type.</para>
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
        /// <para>en-us>The message handling action.</para>
        /// </param>
        /// <param name="GroupId">
        /// <para>zh-cn:消费者组编号。</para>
        /// <para>en-us>The consumer group id.</para>
        /// </param>
        private void SubscribeInternal<TKey, TTopicSubscribeConfig, TMessageContentStandard>
            (ITopicSubscribeConfig<TTopicSubscribeConfig> subscribeConfig,
                Action<TMessageContentStandard> action, string GroupId = null)
            where TTopicSubscribeConfig : class
            where TMessageContentStandard : class, new()
        {
            Task.Factory.StartNew(() =>
            {
                EnsureConsumerConfig(subscribeConfig, GroupId);
                ConsumerConfig config = subscribeConfig.Config as ConsumerConfig;
                var recovery = GetConsumerRecoveryStandard();
                var compensation = GetFailureCompensationStandard();
                var executionOptions = GetConsumerExecutionOptions();
                var reconnectDelay = TimeSpan.FromSeconds(3);

                while (true)
                {
                    IConsumer<TKey, string> consumer = null;
                    try
                    {
                        var consume = BuildConsumerBuilder<TKey>(config, subscribeConfig.TopicName, GroupId, recovery);
                        consumer = consume.Build();
                        consumer.Subscribe(subscribeConfig.TopicName);
                        NotifyConsumerRecovered(recovery, subscribeConfig.TopicName, GroupId, consumer, null, null);

                        while (true)
                        {
                            var consumeResult = consumer.Consume();
                            var handled = HandleConsumedMessage(
                                subscribeConfig.TopicName,
                                consumeResult,
                                action,
                                compensation,
                                executionOptions);

                            if (handled && config.EnableAutoCommit.HasValue && !config.EnableAutoCommit.Value)
                            {
                                // 手动提交成功处理的消息；失败消息默认不提交，让 Kafka 按未提交 offset 重新投递。
                                // Manually commit successfully handled messages. Failed messages are not committed by default, so Kafka can redeliver them by uncommitted offsets.
                                consumer.Commit(consumeResult);
                            }
                        }
                    }
                    catch (ConsumeException ex)
                    {
                        ReportConsumerLoopException(recovery, compensation, subscribeConfig.TopicName, GroupId, consumer, ex, ex);
                    }
                    catch (KafkaException ex)
                    {
                        ReportConsumerLoopException(recovery, compensation, subscribeConfig.TopicName, GroupId, consumer, ex, ex);
                    }
                    catch (Exception ex)
                    {
                        ReportConsumerLoopException(recovery, compensation, subscribeConfig.TopicName, GroupId, consumer, ex, ex);
                    }
                    finally
                    {
                        CloseConsumer(consumer);
                    }

                    Thread.Sleep(reconnectDelay);
                }
            }, TaskCreationOptions.LongRunning);
        }

        /// <summary>
        /// <para>zh-cn:构建带有 rebalance 桥接回调的 Kafka ConsumerBuilder。</para>
        /// <para>en-us:Builds a Kafka ConsumerBuilder with rebalance bridge callbacks.</para>
        /// </summary>
        /// <typeparam name="TKey">
        /// <para>zh-cn:Kafka 消息 Key 类型。</para>
        /// <para>en-us:The Kafka message key type.</para>
        /// </typeparam>
        /// <param name="config">
        /// <para>zh-cn:Kafka 消费者配置。</para>
        /// <para>en-us>The Kafka consumer configuration.</para>
        /// </param>
        /// <param name="topicName">
        /// <para>zh-cn:订阅 Topic 名称。</para>
        /// <para>en-us>The subscribed topic name.</para>
        /// </param>
        /// <param name="groupId">
        /// <para>zh-cn:消费者组编号。</para>
        /// <para>en-us>The consumer group id.</para>
        /// </param>
        /// <param name="recovery">
        /// <para>zh-cn:消费者恢复标准实现。</para>
        /// <para>en-us>The consumer recovery implementation.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:Kafka ConsumerBuilder。</para>
        /// <para>en-us>The Kafka ConsumerBuilder.</para>
        /// </returns>
        private void ReportConsumerLoopException<TKey>(
            IMessageQueueConsumerRecoveryStandard recovery,
            IMessageQueueFailureCompensationStandard compensation,
            string topicName,
            string groupId,
            IConsumer<TKey, string> consumer,
            object nativeEventArgs,
            Exception exception)
        {
            WriteConsumerLoopException(exception, topicName, groupId);
            TryCommitFailedConsumeRecord(consumer, exception, topicName, groupId);

            try
            {
                NotifyConsumerInterrupted(recovery, topicName, groupId, consumer, nativeEventArgs, MessageQueueConsumerInterruptedReason.ConnectionError, exception);
            }
            catch (Exception recoveryException)
            {
                Console.Error.WriteLine($"Kafka consumer interruption callback failed. Topic={topicName}, GroupId={groupId}, Exception={recoveryException}");
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
                Console.Error.WriteLine($"Kafka consumer failure compensation callback failed. Topic={topicName}, GroupId={groupId}, Exception={compensationException}");
            }
        }

        private void TryCommitFailedConsumeRecord<TKey>(
            IConsumer<TKey, string> consumer,
            Exception exception,
            string topicName,
            string groupId)
        {
            if (consumer == null || exception is not ConsumeException { ConsumerRecord: not null } consumeException)
            {
                return;
            }

            try
            {
                var failedRecord = consumeException.ConsumerRecord;
                var nextOffset = new TopicPartitionOffset(
                    failedRecord.TopicPartition,
                    failedRecord.Offset + 1);

                consumer.Commit(new[] { nextOffset });
                Console.Error.WriteLine($"Kafka consumer skipped failed record. Topic={topicName}, GroupId={groupId}, Offset={failedRecord.TopicPartitionOffset}");
            }
            catch (Exception commitException)
            {
                Console.Error.WriteLine($"Kafka consumer failed record commit failed. Topic={topicName}, GroupId={groupId}, Exception={commitException}");
            }
        }

        private void WriteConsumerLoopException(Exception exception, string topicName, string groupId)
        {
            var message = $"Kafka consumer loop error. Topic={topicName}, GroupId={groupId}, Exception={exception}";
            Console.Error.WriteLine(message);

            try
            {
                AppRealization.Output.Error(exception, new Dictionary<string, object>
                {
                    ["TopicName"] = topicName,
                    ["GroupId"] = groupId,
                    ["Provider"] = "Kafka"
                });
            }
            catch (Exception outputException)
            {
                Console.Error.WriteLine($"Kafka consumer error output failed. Exception={outputException}");
            }
        }

        private ConsumerBuilder<TKey, string> BuildConsumerBuilder<TKey>(
            ConsumerConfig config,
            string topicName,
            string groupId,
            IMessageQueueConsumerRecoveryStandard recovery)
        {
            return new ConsumerBuilder<TKey, string>(config)
                .SetPartitionsRevokedHandler((consumer, partitions) =>
                {
                    NotifyConsumerInterrupted(recovery, topicName, groupId, consumer, partitions, MessageQueueConsumerInterruptedReason.ResourceRevoked, null);
                })
                .SetPartitionsLostHandler((consumer, partitions) =>
                {
                    NotifyConsumerInterrupted(recovery, topicName, groupId, consumer, partitions, MessageQueueConsumerInterruptedReason.ResourceLost, null);
                })
                .SetPartitionsAssignedHandler((consumer, partitions) =>
                {
                    NotifyConsumerRecovered(recovery, topicName, groupId, consumer, partitions, null);
                });
        }

        /// <summary>
        /// <para>zh-cn:处理单条 Kafka 消息，包含反序列化、业务处理、处理超时判断与失败补偿触发。</para>
        /// <para>en-us:Handles a single Kafka message, including deserialization, business handling, handling-timeout detection, and failure compensation triggering.</para>
        /// </summary>
        /// <typeparam name="TKey">
        /// <para>zh-cn:Kafka 消息 Key 类型。</para>
        /// <para>en-us>The Kafka message key type.</para>
        /// </typeparam>
        /// <typeparam name="TMessageContentStandard">
        /// <para>zh-cn:消息内容类型。</para>
        /// <para>en-us>The message content type.</para>
        /// </typeparam>
        /// <param name="topicName">
        /// <para>zh-cn:Topic 名称。</para>
        /// <para>en-us>The topic name.</para>
        /// </param>
        /// <param name="consumeResult">
        /// <para>zh-cn:Kafka 原生消费结果。</para>
        /// <para>en-us>The native Kafka consume result.</para>
        /// </param>
        /// <param name="action">
        /// <para>zh-cn:业务处理动作。</para>
        /// <para>en-us>The business handling action.</para>
        /// </param>
        /// <param name="compensation">
        /// <para>zh-cn:失败补偿标准实现。</para>
        /// <para>en-us>The failure compensation implementation.</para>
        /// </param>
        /// <param name="executionOptions">
        /// <para>zh-cn:消费者执行策略。</para>
        /// <para>en-us>The consumer execution options.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:处理是否成功。</para>
        /// <para>en-us>Whether the message was handled successfully.</para>
        /// </returns>
        private bool HandleConsumedMessage<TKey, TMessageContentStandard>(
            string topicName,
            ConsumeResult<TKey, string> consumeResult,
            Action<TMessageContentStandard> action,
            IMessageQueueFailureCompensationStandard compensation,
            IMessageQueueConsumerExecutionOptions executionOptions)
            where TMessageContentStandard : class, new()
        {
            var stopwatch = Stopwatch.StartNew();
            TMessageContentStandard messageContentStandard = null;

            try
            {
                messageContentStandard = AppRealization.JSON.Deserialize<TMessageContentStandard>(consumeResult.Message.Value);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                NotifyMessageFailed(
                    compensation,
                    topicName,
                    messageContentStandard,
                    ex,
                    false,
                    executionOptions.MessageHandlingTimeout,
                    stopwatch.Elapsed,
                    MessageQueueFailureReason.DeserializeFailed,
                    consumeResult);
                return false;
            }

            var handlingTask = Task.Run(() => action.Invoke(messageContentStandard));
            var timeout = executionOptions.MessageHandlingTimeout;

            try
            {
                if (timeout.HasValue && !handlingTask.Wait(timeout.Value))
                {
                    stopwatch.Stop();
                    NotifyMessageFailed(
                        compensation,
                        topicName,
                        messageContentStandard,
                        new TimeoutException($"Kafka message handling exceeded timeout {timeout.Value}."),
                        true,
                        timeout,
                        stopwatch.Elapsed,
                        MessageQueueFailureReason.Timeout,
                        consumeResult);
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
                    messageContentStandard,
                    ex,
                    false,
                    timeout,
                    stopwatch.Elapsed,
                    MessageQueueFailureReason.Exception,
                    consumeResult);
                return false;
            }
        }

        /// <summary>
        /// <para>zh-cn:触发消费者中断标准接口。</para>
        /// <para>en-us:Triggers the consumer interruption standard interface.</para>
        /// </summary>
        /// <param name="recovery">
        /// <para>zh-cn:消费者恢复标准实现。</para>
        /// <para>en-us>The consumer recovery implementation.</para>
        /// </param>
        /// <param name="topicName">
        /// <para>zh-cn:Topic 名称。</para>
        /// <para>en-us>The topic name.</para>
        /// </param>
        /// <param name="groupId">
        /// <para>zh-cn:消费者组编号。</para>
        /// <para>en-us>The consumer group id.</para>
        /// </param>
        /// <param name="consumer">
        /// <para>zh-cn:Kafka 原生消费者。</para>
        /// <para>en-us>The native Kafka consumer.</para>
        /// </param>
        /// <param name="nativeEventArgs">
        /// <para>zh-cn:Kafka 原生事件参数。</para>
        /// <para>en-us>The native Kafka event arguments.</para>
        /// </param>
        /// <param name="reason">
        /// <para>zh-cn:消费者中断原因。</para>
        /// <para>en-us>The consumer interruption reason.</para>
        /// </param>
        /// <param name="exception">
        /// <para>zh-cn:触发中断的异常。</para>
        /// <para>en-us>The exception that triggered the interruption.</para>
        /// </param>
        private void NotifyConsumerInterrupted(
            IMessageQueueConsumerRecoveryStandard recovery,
            string topicName,
            string groupId,
            object consumer,
            object nativeEventArgs,
            MessageQueueConsumerInterruptedReason reason,
            Exception exception)
        {
            recovery?.OnConsumerInterruptedAsync(new KafkaMessageQueueConsumerContext()
            {
                TopicName = topicName,
                GroupId = groupId,
                InterruptedReason = reason,
                Exception = exception,
                NativeConsumer = consumer,
                NativeEventArgs = nativeEventArgs
            }).GetAwaiter().GetResult();
        }

        /// <summary>
        /// <para>zh-cn:触发消费者恢复标准接口。</para>
        /// <para>en-us:Triggers the consumer recovery standard interface.</para>
        /// </summary>
        /// <param name="recovery">
        /// <para>zh-cn:消费者恢复标准实现。</para>
        /// <para>en-us>The consumer recovery implementation.</para>
        /// </param>
        /// <param name="topicName">
        /// <para>zh-cn:Topic 名称。</para>
        /// <para>en-us>The topic name.</para>
        /// </param>
        /// <param name="groupId">
        /// <para>zh-cn:消费者组编号。</para>
        /// <para>en-us>The consumer group id.</para>
        /// </param>
        /// <param name="consumer">
        /// <para>zh-cn:Kafka 原生消费者。</para>
        /// <para>en-us>The native Kafka consumer.</para>
        /// </param>
        /// <param name="nativeEventArgs">
        /// <para>zh-cn:Kafka 原生事件参数。</para>
        /// <para>en-us>The native Kafka event arguments.</para>
        /// </param>
        /// <param name="exception">
        /// <para>zh-cn:触发恢复事件时关联的异常。</para>
        /// <para>en-us:The exception associated with the recovery event.</para>
        /// </param>
        private void NotifyConsumerRecovered(
            IMessageQueueConsumerRecoveryStandard recovery,
            string topicName,
            string groupId,
            object consumer,
            object nativeEventArgs,
            Exception exception)
        {
            recovery?.OnConsumerRecoveredAsync(new KafkaMessageQueueConsumerContext()
            {
                TopicName = topicName,
                GroupId = groupId,
                InterruptedReason = MessageQueueConsumerInterruptedReason.Unknown,
                Exception = exception,
                NativeConsumer = consumer,
                NativeEventArgs = nativeEventArgs
            }).GetAwaiter().GetResult();
        }

        /// <summary>
        /// <para>zh-cn:触发消息失败补偿标准接口。</para>
        /// <para>en-us:Triggers the message failure compensation standard interface.</para>
        /// </summary>
        /// <param name="compensation">
        /// <para>zh-cn:失败补偿标准实现。</para>
        /// <para>en-us>The failure compensation implementation.</para>
        /// </param>
        /// <param name="topicName">
        /// <para>zh-cn:Topic 名称。</para>
        /// <para>en-us>The topic name.</para>
        /// </param>
        /// <param name="messageContent">
        /// <para>zh-cn:消息内容。</para>
        /// <para>en-us>The message content.</para>
        /// </param>
        /// <param name="exception">
        /// <para>zh-cn:失败异常。</para>
        /// <para>en-us>The failure exception.</para>
        /// </param>
        /// <param name="isTimeout">
        /// <para>zh-cn:是否超时失败。</para>
        /// <para>en-us>Whether the failure is a timeout failure.</para>
        /// </param>
        /// <param name="handlingTimeout">
        /// <para>zh-cn:处理超时配置。</para>
        /// <para>en-us>The handling timeout configuration.</para>
        /// </param>
        /// <param name="handlingElapsed">
        /// <para>zh-cn:处理耗时。</para>
        /// <para>en-us>The handling elapsed time.</para>
        /// </param>
        /// <param name="failureReason">
        /// <para>zh-cn:失败原因。</para>
        /// <para>en-us>The failure reason.</para>
        /// </param>
        /// <param name="nativeConsumeResult">
        /// <para>zh-cn:Kafka 原生消费结果。</para>
        /// <para>en-us>The native Kafka consume result.</para>
        /// </param>
        private void NotifyMessageFailed(
            IMessageQueueFailureCompensationStandard compensation,
            string topicName,
            object messageContent,
            Exception exception,
            bool isTimeout,
            TimeSpan? handlingTimeout,
            TimeSpan handlingElapsed,
            MessageQueueFailureReason failureReason,
            object nativeConsumeResult)
        {
            compensation?.OnFailedAsync(new KafkaMessageQueueFailureContext()
            {
                TopicName = topicName,
                MessageContent = messageContent,
                Exception = exception,
                IsTimeout = isTimeout,
                HandlingTimeout = handlingTimeout,
                HandlingElapsed = handlingElapsed,
                RetryCount = 0,
                FailureReason = failureReason,
                NativeConsumeResult = nativeConsumeResult
            }).GetAwaiter().GetResult();
        }

        /// <summary>
        /// <para>zh-cn:确保发布配置存在；如果未传入配置则按 TopicName 使用配置池或创建默认 Kafka ProducerConfig。</para>
        /// <para>en-us:Ensures the publish configuration exists. If no configuration is provided, the configuration pool or a default Kafka ProducerConfig is used by TopicName.</para>
        /// </summary>
        /// <typeparam name="TTopicPublishConfig">
        /// <para>zh-cn:发布配置类型。</para>
        /// <para>en-us>The publish configuration type.</para>
        /// </typeparam>
        /// <param name="producerConfigModel">
        /// <para>zh-cn:发布配置模型。</para>
        /// <para>en-us>The publish configuration model.</para>
        /// </param>
        private void EnsureProducerConfig<TTopicPublishConfig>(ITopicPublishConfig<TTopicPublishConfig> producerConfigModel)
            where TTopicPublishConfig : class
        {
            if (producerConfigModel.Config != null)
            {
                return;
            }

            var config = producerConfigPool.Get(producerConfigModel.TopicName);
            if (config == null)
            {
                ProducerConfig defaultProducerConfig = new ProducerConfig()
                {
                    BootstrapServers = KafkaClusterOptions.ClusterAddress,
                };
                producerConfigModel.Config = defaultProducerConfig as TTopicPublishConfig;
                if (producerConfigModel.Config == null)
                {
                    throw new InvalidOperationException($"Kafka producer config type must be {nameof(ProducerConfig)}.");
                }

                producerConfigPool.Set((ITopicPublishConfig<ProducerConfig>)producerConfigModel);
            }
            else
            {
                producerConfigModel.Config = config.Config as TTopicPublishConfig;
            }
        }

        /// <summary>
        /// <para>zh-cn:确保订阅配置存在；如果未传入配置则创建默认 Kafka ConsumerConfig。</para>
        /// <para>en-us:Ensures the subscription configuration exists. If no configuration is provided, a default Kafka ConsumerConfig is created.</para>
        /// </summary>
        /// <typeparam name="TTopicSubscribeConfig">
        /// <para>zh-cn:订阅配置类型。</para>
        /// <para>en-us>The subscription configuration type.</para>
        /// </typeparam>
        /// <param name="subscribeConfig">
        /// <para>zh-cn:订阅配置模型。</para>
        /// <para>en-us>The subscription configuration model.</para>
        /// </param>
        /// <param name="groupId">
        /// <para>zh-cn:消费者组编号。</para>
        /// <para>en-us>The consumer group id.</para>
        /// </param>
        private void EnsureConsumerConfig<TTopicSubscribeConfig>(ITopicSubscribeConfig<TTopicSubscribeConfig> subscribeConfig, string groupId)
            where TTopicSubscribeConfig : class
        {
            if (subscribeConfig.Config == null)
            {
                var config = new ConsumerConfig()
                {
                    GroupId = groupId,
                    BootstrapServers = KafkaClusterOptions.ClusterAddress
                } as TTopicSubscribeConfig;
                if (config == null)
                {
                    throw new InvalidOperationException($"Kafka consumer config type must be {nameof(ConsumerConfig)}.");
                }
                subscribeConfig.Config = config;
            }

            var consumerConfig = subscribeConfig.Config as ConsumerConfig;
            if (consumerConfig == null)
            {
                throw new InvalidOperationException($"Kafka consumer config type must be {nameof(ConsumerConfig)}.");
            }

            if (!consumerConfig.EnableAutoCommit.HasValue)
            {
                consumerConfig.EnableAutoCommit = false;
            }
        }

        /// <summary>
        /// <para>zh-cn:安全关闭 Kafka 消费者。</para>
        /// <para>en-us:Safely closes a Kafka consumer.</para>
        /// </summary>
        /// <typeparam name="TKey">
        /// <para>zh-cn:Kafka 消息 Key 类型。</para>
        /// <para>en-us>The Kafka message key type.</para>
        /// </typeparam>
        /// <param name="consumer">
        /// <para>zh-cn:Kafka 消费者实例。</para>
        /// <para>en-us>The Kafka consumer instance.</para>
        /// </param>
        private void CloseConsumer<TKey>(IConsumer<TKey, string> consumer)
        {
            if (consumer == null)
            {
                return;
            }

            try
            {
                consumer.Close();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Kafka consumer close failed. Exception={ex}");
                try
                {
                    AppRealization.Output.Error(ex);
                }
                catch (Exception outputException)
                {
                    Console.Error.WriteLine($"Kafka consumer close error output failed. Exception={outputException}");
                }
            }
            finally
            {
                consumer.Dispose();
            }
        }

        /// <summary>
        /// <para>zh-cn:生成 Kafka 消息 Key。</para>
        /// <para>en-us:Generates a Kafka message key.</para>
        /// </summary>
        /// <typeparam name="TKey">
        /// <para>zh-cn:消息 Key 类型。</para>
        /// <para>en-us>The message key type.</para>
        /// </typeparam>
        /// <param name="context">
        /// <para>zh-cn:Key 生成上下文。</para>
        /// <para>en-us>The key generation context.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:生成后的消息 Key。</para>
        /// <para>en-us>The generated message key.</para>
        /// </returns>
        private TKey GenerateKey<TKey>(IMessageQueueKeyGenerationContext context)
        {
            var generator = AppCore.GetService<IMessageQueueKeyGenerator<TKey>>();
            if (generator != null)
            {
                return generator.Generate(context);
            }

            var generatorObject = GetKeyGenerators().FirstOrDefault(generator => generator.GetKeyType() == typeof(TKey));
            if (generatorObject != null)
            {
                return (TKey)generatorObject.Generate(context);
            }

            throw new InvalidOperationException($"No message queue key generator was registered for key type {typeof(TKey).FullName}.");
        }

        /// <summary>
        /// <para>zh-cn:解析本次发布或订阅应使用的消息 Key 类型。</para>
        /// <para>en-us:Resolves the message key type that should be used for the current publish or subscribe operation.</para>
        /// </summary>
        /// <param name="configuredKeyType">
        /// <para>zh-cn:配置中显式指定的 Key 类型。</para>
        /// <para>en-us:The key type explicitly configured by the configuration object.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:解析后的消息 Key 类型。</para>
        /// <para>en-us>The resolved message key type.</para>
        /// </returns>
        private Type ResolveKeyType(Type configuredKeyType)
        {
            if (configuredKeyType != null)
            {
                return configuredKeyType;
            }

            var keyTypes = GetKeyGenerators().Select(generator => generator.GetKeyType()).Distinct().ToArray();
            if (keyTypes.Length == 1)
            {
                return keyTypes[0];
            }

            throw new InvalidOperationException("Message queue key type is not configured and cannot be inferred from registered key generators.");
        }

        /// <summary>
        /// <para>zh-cn:获取已注册的非泛型消息 Key 生成器集合。</para>
        /// <para>en-us:Gets the registered non-generic message key generator collection.</para>
        /// </summary>
        /// <returns>
        /// <para>zh-cn:消息 Key 生成器集合。</para>
        /// <para>en-us>The message key generator collection.</para>
        /// </returns>
        private IEnumerable<IMessageQueueKeyGenerator> GetKeyGenerators()
        {
            var serviceProvider = AppCore.GetServiceProvider(typeof(IMessageQueueKeyGenerator));
            return serviceProvider?.GetServices<IMessageQueueKeyGenerator>() ?? Enumerable.Empty<IMessageQueueKeyGenerator>();
        }

        /// <summary>
        /// <para>zh-cn:获取消费者恢复标准实现。</para>
        /// <para>en-us:Gets the consumer recovery implementation.</para>
        /// </summary>
        /// <returns>
        /// <para>zh-cn:消费者恢复标准实现。</para>
        /// <para>en-us>The consumer recovery implementation.</para>
        /// </returns>
        private IMessageQueueConsumerRecoveryStandard GetConsumerRecoveryStandard()
        {
            return AppCore.GetService<IMessageQueueConsumerRecoveryStandard>();
        }

        /// <summary>
        /// <para>zh-cn:获取失败补偿标准实现。</para>
        /// <para>en-us:Gets the failure compensation implementation.</para>
        /// </summary>
        /// <returns>
        /// <para>zh-cn:失败补偿标准实现。</para>
        /// <para>en-us>The failure compensation implementation.</para>
        /// </returns>
        private IMessageQueueFailureCompensationStandard GetFailureCompensationStandard()
        {
            return AppCore.GetService<IMessageQueueFailureCompensationStandard>();
        }

        /// <summary>
        /// <para>zh-cn:获取消费者执行策略。</para>
        /// <para>en-us:Gets the consumer execution options.</para>
        /// </summary>
        /// <returns>
        /// <para>zh-cn:消费者执行策略。</para>
        /// <para>en-us>The consumer execution options.</para>
        /// </returns>
        private IMessageQueueConsumerExecutionOptions GetConsumerExecutionOptions()
        {
            var options = AppCore.GetService<IMessageQueueConsumerExecutionOptions>();
            if (options == null)
            {
                throw new InvalidOperationException($"No {nameof(IMessageQueueConsumerExecutionOptions)} was registered.");
            }
            return options;
        }

        /// <summary>
        /// <para>zh-cn:构建生产者池缓存 Key。</para>
        /// <para>en-us:Builds the producer pool cache key.</para>
        /// </summary>
        /// <param name="topicName">
        /// <para>zh-cn:主题名称。</para>
        /// <para>en-us>The topic name.</para>
        /// </param>
        /// <param name="keyType">
        /// <para>zh-cn:消息 Key 类型。</para>
        /// <para>en-us>The message key type.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:生产者池缓存 Key。</para>
        /// <para>en-us>The producer pool cache key.</para>
        /// </returns>
        private string BuildProducerCacheKey(string topicName, Type keyType)
        {
            return $"{topicName}_{keyType.FullName}";
        }

        #endregion
    }
}
