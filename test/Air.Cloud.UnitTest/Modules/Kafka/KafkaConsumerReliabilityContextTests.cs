using Air.Cloud.Core.Standard.MessageQueue;
using Air.Cloud.Core.Standard.MessageQueue.Config;
using Air.Cloud.Core.Standard.MessageQueue.Enums;
using Air.Cloud.Modules.Kafka.Contexts;
using Air.Cloud.Modules.Kafka.Helper;
using Confluent.Kafka;

using System.Reflection;

namespace Air.Cloud.UnitTest.Modules.Kafka
{
    public class KafkaConsumerReliabilityContextTests
    {
        [Fact]
        public void KafkaMessageQueueConsumerContext_should_expose_lifecycle_values()
        {
            var exception = new InvalidOperationException("consumer interrupted");
            var nativeConsumer = new object();
            var nativeEventArgs = new[] { "partition-0" };
            var context = new KafkaMessageQueueConsumerContext()
            {
                TopicName = "consumer-topic",
                GroupId = "consumer-group",
                InterruptedReason = MessageQueueConsumerInterruptedReason.ResourceRevoked,
                Exception = exception,
                NativeConsumer = nativeConsumer,
                NativeEventArgs = nativeEventArgs
            };

            Assert.Equal("consumer-topic", context.TopicName);
            Assert.Equal("consumer-group", context.GroupId);
            Assert.Equal("Kafka", context.ProviderName);
            Assert.Equal(MessageQueueConsumerInterruptedReason.ResourceRevoked, context.InterruptedReason);
            Assert.Same(exception, context.Exception);
            Assert.Same(nativeConsumer, context.NativeConsumer);
            Assert.Same(nativeEventArgs, context.NativeEventArgs);
        }

        [Fact]
        public void KafkaMessageQueueFailureContext_should_expose_failure_values()
        {
            var exception = new TimeoutException("message timeout");
            var message = new TestKafkaMessage { Content = "payload" };
            var nativeConsumeResult = new object();
            var context = new KafkaMessageQueueFailureContext()
            {
                TopicName = "failure-topic",
                MessageContent = message,
                Exception = exception,
                IsTimeout = true,
                HandlingTimeout = TimeSpan.FromSeconds(1),
                HandlingElapsed = TimeSpan.FromSeconds(2),
                RetryCount = 3,
                FailureReason = MessageQueueFailureReason.Timeout,
                NativeConsumeResult = nativeConsumeResult
            };

            Assert.Equal("failure-topic", context.TopicName);
            Assert.Equal("Kafka", context.ProviderName);
            Assert.Same(message, context.MessageContent);
            Assert.Same(exception, context.Exception);
            Assert.True(context.IsTimeout);
            Assert.Equal(TimeSpan.FromSeconds(1), context.HandlingTimeout);
            Assert.Equal(TimeSpan.FromSeconds(2), context.HandlingElapsed);
            Assert.Equal(3, context.RetryCount);
            Assert.Equal(MessageQueueFailureReason.Timeout, context.FailureReason);
            Assert.Same(nativeConsumeResult, context.NativeConsumeResult);
        }

        [Fact]
        public void KafkaMessageQueueDependency_should_write_consumer_loop_error_even_when_callbacks_throw()
        {
            var dependency = new KafkaMessageQueueDependency();
            var method = typeof(KafkaMessageQueueDependency).GetMethod(
                "ReportConsumerLoopException",
                BindingFlags.Instance | BindingFlags.NonPublic)!
                .MakeGenericMethod(typeof(int));
            var originalError = Console.Error;

            using var writer = new StringWriter();
            Console.SetError(writer);
            try
            {
                method!.Invoke(dependency, new object?[]
                {
                    new ThrowingConsumerRecoveryStandard(),
                    new ThrowingFailureCompensationStandard(),
                    "consumer-loop-topic",
                    "consumer-loop-group",
                    null,
                    new object(),
                    new InvalidOperationException("key deserialize failed")
                });
            }
            finally
            {
                Console.SetError(originalError);
            }

            var output = writer.ToString();
            Assert.Contains("Kafka consumer loop error.", output);
            Assert.Contains("consumer-loop-topic", output);
            Assert.Contains("key deserialize failed", output);
            Assert.Contains("Kafka consumer interruption callback failed.", output);
            Assert.Contains("Kafka consumer failure compensation callback failed.", output);
        }

        [Fact]
        public void KafkaMessageQueueDependency_should_forward_consumer_loop_exception_to_recovery_and_compensation()
        {
            var dependency = new KafkaMessageQueueDependency();
            var method = typeof(KafkaMessageQueueDependency).GetMethod(
                "ReportConsumerLoopException",
                BindingFlags.Instance | BindingFlags.NonPublic)!
                .MakeGenericMethod(typeof(int));
            var recovery = new CapturingConsumerRecoveryStandard();
            var compensation = new CapturingFailureCompensationStandard();
            var nativeEventArgs = new object();
            var exception = new InvalidOperationException("key deserialize failed");
            var originalError = Console.Error;

            using var writer = new StringWriter();
            Console.SetError(writer);
            try
            {
                method.Invoke(dependency, new object?[]
                {
                    recovery,
                    compensation,
                    "consumer-loop-topic",
                    "consumer-loop-group",
                    null,
                    nativeEventArgs,
                    exception
                });
            }
            finally
            {
                Console.SetError(originalError);
            }

            Assert.Equal("consumer-loop-topic", recovery.Context!.TopicName);
            Assert.Equal("consumer-loop-group", recovery.Context.GroupId);
            Assert.Equal(MessageQueueConsumerInterruptedReason.ConnectionError, recovery.Context.InterruptedReason);
            Assert.Same(exception, recovery.Context.Exception);
            Assert.Same(nativeEventArgs, ((KafkaMessageQueueConsumerContext)recovery.Context).NativeEventArgs);
            Assert.Equal("consumer-loop-topic", compensation.Context!.TopicName);
            Assert.Equal(MessageQueueFailureReason.ConsumerInterrupted, compensation.Context.FailureReason);
            Assert.Same(exception, compensation.Context.Exception);
            Assert.Same(nativeEventArgs, ((KafkaMessageQueueFailureContext)compensation.Context).NativeConsumeResult);
        }

        [Fact]
        public void KafkaMessageQueueDependency_should_apply_dynamic_group_id_when_existing_consumer_config_has_no_group_id()
        {
            var dependency = new KafkaMessageQueueDependency();
            var method = typeof(KafkaMessageQueueDependency).GetMethod(
                "EnsureConsumerConfig",
                BindingFlags.Instance | BindingFlags.NonPublic)!
                .MakeGenericMethod(typeof(ConsumerConfig));
            var subscribeConfig = new TestTopicSubscribeConfig
            {
                TopicName = "fcj_workflow_audit",
                Config = new ConsumerConfig
                {
                    BootstrapServers = "192.168.100.165:9092"
                }
            };

            method.Invoke(dependency, new object?[]
            {
                subscribeConfig,
                "workflow_audit_listener"
            });

            Assert.Equal("workflow_audit_listener", subscribeConfig.Config.GroupId);
            Assert.False(subscribeConfig.Config.EnableAutoCommit);
        }

        [Fact]
        public void KafkaMessageQueueDependency_should_preserve_explicit_consumer_config_group_id()
        {
            var dependency = new KafkaMessageQueueDependency();
            var method = typeof(KafkaMessageQueueDependency).GetMethod(
                "EnsureConsumerConfig",
                BindingFlags.Instance | BindingFlags.NonPublic)!
                .MakeGenericMethod(typeof(ConsumerConfig));
            var subscribeConfig = new TestTopicSubscribeConfig
            {
                TopicName = "fcj_workflow_audit",
                Config = new ConsumerConfig
                {
                    BootstrapServers = "192.168.100.165:9092",
                    GroupId = "configured_group"
                }
            };

            method.Invoke(dependency, new object?[]
            {
                subscribeConfig,
                "workflow_audit_listener"
            });

            Assert.Equal("configured_group", subscribeConfig.Config.GroupId);
        }

        private sealed class TestKafkaMessage
        {
            public string Content { get; set; } = string.Empty;
        }

        private sealed class TestTopicSubscribeConfig : ITopicSubscribeConfig<ConsumerConfig>
        {
            public string TopicName { get; set; } = string.Empty;

            public ConsumerConfig Config { get; set; } = null!;

            public Type KeyType { get; set; } = typeof(int);
        }

        private sealed class CapturingConsumerRecoveryStandard : IMessageQueueConsumerRecoveryStandard
        {
            public IMessageQueueConsumerContext? Context { get; private set; }

            public Task OnConsumerInterruptedAsync(IMessageQueueConsumerContext context)
            {
                Context = context;
                return Task.CompletedTask;
            }

            public Task OnConsumerRecoveredAsync(IMessageQueueConsumerContext context)
            {
                return Task.CompletedTask;
            }
        }

        private sealed class CapturingFailureCompensationStandard : IMessageQueueFailureCompensationStandard
        {
            public IMessageQueueFailureContext? Context { get; private set; }

            public Task OnFailedAsync(IMessageQueueFailureContext context)
            {
                Context = context;
                return Task.CompletedTask;
            }
        }

        private sealed class ThrowingConsumerRecoveryStandard : IMessageQueueConsumerRecoveryStandard
        {
            public Task OnConsumerInterruptedAsync(IMessageQueueConsumerContext context)
            {
                throw new InvalidOperationException("consumer recovery failed");
            }

            public Task OnConsumerRecoveredAsync(IMessageQueueConsumerContext context)
            {
                throw new InvalidOperationException("consumer recovered failed");
            }
        }

        private sealed class ThrowingFailureCompensationStandard : IMessageQueueFailureCompensationStandard
        {
            public Task OnFailedAsync(IMessageQueueFailureContext context)
            {
                throw new InvalidOperationException("failure compensation failed");
            }
        }
    }
}
