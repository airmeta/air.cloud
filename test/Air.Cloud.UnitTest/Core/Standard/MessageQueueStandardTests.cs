using Air.Cloud.Core.App;
using Air.Cloud.Core.Standard.MessageQueue;
using Air.Cloud.Core.Standard.MessageQueue.Attributes;
using Air.Cloud.Core.Standard.MessageQueue.Enums;

namespace Air.Cloud.UnitTest.Core.Standard
{
    /// <summary>
    /// <para>zh-cn:消息队列标准层通用契约的单元测试；Kafka 具体实现已由 Kafka 测试覆盖。</para>
    /// <para>en-us:Unit tests for provider-neutral message-queue contracts; Kafka-specific behavior is covered by Kafka tests.</para>
    /// </summary>
    public class MessageQueueStandardTests
    {
        /// <summary>
        /// <para>zh-cn:验证显式消费者组不会被默认构建器修改，避免生产配置被隐式拼接破坏。</para>
        /// <para>en-us:Verifies an explicit consumer group is not changed by the default builder to avoid breaking production configuration.</para>
        /// </summary>
        [Fact]
        public void BuildGroupId_should_return_explicit_group_id()
        {
            var builder = new MessageQueueBuilderStandard();

            var groupId = builder.BuildGroupId("business-group", EnvironmentDistinguish: true, VerisonDistinguish: true);

            Assert.Equal("business-group", groupId);
        }

        /// <summary>
        /// <para>zh-cn:验证非开发环境未传消费者组时使用应用名称作为默认值。</para>
        /// <para>en-us:Verifies non-development environments use the application name when no consumer group is provided.</para>
        /// </summary>
        [Fact]
        public void BuildGroupId_should_use_application_name_outside_development()
        {
            var originalApplicationName = AppConst.ApplicationName;
            var originalEnvironment = AppConst.EnvironmentStatus;

            try
            {
                AppConst.ApplicationName = "air-cloud-app";
                AppConst.EnvironmentStatus = Air.Cloud.Core.Enums.EnvironmentEnums.Production;
                var builder = new MessageQueueBuilderStandard();

                var groupId = builder.BuildGroupId();

                Assert.Equal("air-cloud-app", groupId);
            }
            finally
            {
                AppConst.ApplicationName = originalApplicationName;
                AppConst.EnvironmentStatus = originalEnvironment;
            }
        }

        /// <summary>
        /// <para>zh-cn:验证队列描述特性保留订阅队列名称，模块可据此从配置中回填 Topic/Queue。</para>
        /// <para>en-us:Verifies the queue descriptor attribute keeps the subscribe queue name so modules can bind topic or queue configuration from it.</para>
        /// </summary>
        [Fact]
        public void AppQueueDescriptorAttribute_should_keep_subscribe_queue()
        {
            var attribute = new AppQueueDescriptorAttribute("orders.created");

            Assert.Equal("orders.created", attribute.SubscribeQueue);
        }

        /// <summary>
        /// <para>zh-cn:验证队列描述特性的回复队列和显式消费者组可以被完整保留。</para>
        /// <para>en-us:Verifies the queue descriptor attribute keeps reply queue and explicit consumer group values.</para>
        /// </summary>
        [Fact]
        public void AppQueueDescriptorAttribute_should_keep_receive_queue_and_explicit_group()
        {
            var attribute = new AppQueueDescriptorAttribute(
                SubscribeQueue: "orders.command",
                ReciveQueue: "orders.reply",
                GroupId: "orders-service");

            Assert.Equal("orders.command", attribute.SubscribeQueue);
            Assert.Equal("orders.reply", attribute.ReciveQueue);
            Assert.Equal("orders-service", attribute.GroupId);
        }

        /// <summary>
        /// <para>zh-cn:验证消息内容标准只约束 Content 属性，模块可以在此基础上追加自己的扩展字段。</para>
        /// <para>en-us:Verifies the message-content standard only requires Content so modules can add provider-specific fields on top.</para>
        /// </summary>
        [Fact]
        public void MessageContentStandard_should_preserve_content_value()
        {
            IMessageContentStandard message = new TestMessageContent
            {
                Content = "standard-message"
            };

            Assert.Equal("standard-message", message.Content);
        }

        /// <summary>
        /// <para>zh-cn:验证泛型 Key 生成器通过非泛型接口暴露 Key 类型和装箱后的 Key 值。</para>
        /// <para>en-us:Verifies a generic key generator exposes the key type and boxed key through the non-generic interface.</para>
        /// </summary>
        [Fact]
        public void Generic_key_generator_should_expose_type_and_boxed_value()
        {
            IMessageQueueKeyGenerator generator = new StringMessageQueueKeyGenerator();
            var context = new TestKeyGenerationContext
            {
                TopicName = "topic-a",
                MessageContent = new TestMessageContent { Content = "payload" },
                PublishConfig = new { TopicName = "topic-a" },
                KeyType = typeof(string)
            };

            Assert.Equal(typeof(string), generator.GetKeyType());
            Assert.Equal("topic-a-payload", generator.Generate(context));
        }

        /// <summary>
        /// <para>zh-cn:验证不同泛型 Key 生成器可以声明不同 Key 类型，供消息队列模块按 KeyType 选择。</para>
        /// <para>en-us:Verifies different generic key generators can declare different key types for modules to select by KeyType.</para>
        /// </summary>
        [Fact]
        public void Generic_key_generator_should_support_non_string_key_types()
        {
            IMessageQueueKeyGenerator generator = new GuidMessageQueueKeyGenerator();
            var context = new TestKeyGenerationContext
            {
                TopicName = "topic-guid",
                MessageContent = new TestMessageContent { Content = "payload" },
                PublishConfig = new { TopicName = "topic-guid" },
                KeyType = typeof(Guid)
            };

            var key = generator.Generate(context);

            Assert.Equal(typeof(Guid), generator.GetKeyType());
            Assert.IsType<Guid>(key);
            Assert.Equal(Guid.Parse("11111111-1111-1111-1111-111111111111"), key);
        }

        /// <summary>
        /// <para>zh-cn:验证消费执行选项可以表达“无限制超时”和重试次数，用于平台无关消费策略。</para>
        /// <para>en-us:Verifies consumer execution options can express unlimited timeout and retry count for provider-neutral consumption policy.</para>
        /// </summary>
        [Fact]
        public void ConsumerExecutionOptions_should_expose_timeout_and_retry_contract()
        {
            IMessageQueueConsumerExecutionOptions options = new TestConsumerExecutionOptions(null, 3);

            Assert.Null(options.MessageHandlingTimeout);
            Assert.Equal(3, options.MaxRetryCount);
        }

        /// <summary>
        /// <para>zh-cn:验证消费恢复和失败补偿上下文可以承载平台无关的核心字段。</para>
        /// <para>en-us:Verifies recovery and failure contexts can carry provider-neutral core fields.</para>
        /// </summary>
        [Fact]
        public async Task Recovery_and_compensation_standards_should_receive_context()
        {
            var exception = new TimeoutException("message timeout");
            var recovery = new CapturingRecoveryStandard();
            var compensation = new CapturingCompensationStandard();
            var consumerContext = new TestConsumerContext
            {
                TopicName = "topic-a",
                GroupId = "group-a",
                ProviderName = "UnitTestQueue",
                InterruptedReason = MessageQueueConsumerInterruptedReason.ResourceRevoked,
                Exception = exception
            };
            var failureContext = new TestFailureContext
            {
                TopicName = "topic-a",
                ProviderName = "UnitTestQueue",
                MessageContent = new TestMessageContent { Content = "payload" },
                Exception = exception,
                IsTimeout = true,
                HandlingTimeout = TimeSpan.FromSeconds(1),
                HandlingElapsed = TimeSpan.FromSeconds(2),
                RetryCount = 1,
                FailureReason = MessageQueueFailureReason.Timeout
            };

            await recovery.OnConsumerInterruptedAsync(consumerContext);
            await recovery.OnConsumerRecoveredAsync(consumerContext);
            await compensation.OnFailedAsync(failureContext);

            Assert.Same(consumerContext, recovery.InterruptedContext);
            Assert.Same(consumerContext, recovery.RecoveredContext);
            Assert.Same(failureContext, compensation.Context);
        }

        private sealed class StringMessageQueueKeyGenerator : IMessageQueueKeyGenerator<string>
        {
            public string Generate(IMessageQueueKeyGenerationContext context)
            {
                return $"{context.TopicName}-{((TestMessageContent)context.MessageContent).Content}";
            }
        }

        private sealed class GuidMessageQueueKeyGenerator : IMessageQueueKeyGenerator<Guid>
        {
            public Guid Generate(IMessageQueueKeyGenerationContext context)
            {
                return Guid.Parse("11111111-1111-1111-1111-111111111111");
            }
        }

        private sealed class TestKeyGenerationContext : IMessageQueueKeyGenerationContext
        {
            public string TopicName { get; init; } = string.Empty;

            public string ProviderName => "UnitTestQueue";

            public object MessageContent { get; init; } = null!;

            public Type MessageContentType => MessageContent.GetType();

            public object PublishConfig { get; init; } = null!;

            public Type PublishConfigType => PublishConfig.GetType();

            public Type KeyType { get; init; } = typeof(string);
        }

        private sealed class TestMessageContent : IMessageContentStandard
        {
            public string Content { get; set; } = string.Empty;
        }

        private sealed class TestConsumerExecutionOptions : IMessageQueueConsumerExecutionOptions
        {
            public TestConsumerExecutionOptions(TimeSpan? messageHandlingTimeout, int maxRetryCount)
            {
                MessageHandlingTimeout = messageHandlingTimeout;
                MaxRetryCount = maxRetryCount;
            }

            public TimeSpan? MessageHandlingTimeout { get; }

            public int MaxRetryCount { get; }
        }

        private sealed class TestConsumerContext : IMessageQueueConsumerContext
        {
            public string TopicName { get; init; } = string.Empty;

            public string GroupId { get; init; } = string.Empty;

            public string ProviderName { get; init; } = string.Empty;

            public MessageQueueConsumerInterruptedReason InterruptedReason { get; init; }

            public Exception Exception { get; init; } = null!;
        }

        private sealed class TestFailureContext : IMessageQueueFailureContext
        {
            public string TopicName { get; init; } = string.Empty;

            public string ProviderName { get; init; } = string.Empty;

            public object MessageContent { get; init; } = null!;

            public Exception Exception { get; init; } = null!;

            public bool IsTimeout { get; init; }

            public TimeSpan? HandlingTimeout { get; init; }

            public TimeSpan HandlingElapsed { get; init; }

            public int RetryCount { get; init; }

            public MessageQueueFailureReason FailureReason { get; init; }
        }

        private sealed class CapturingRecoveryStandard : IMessageQueueConsumerRecoveryStandard
        {
            public IMessageQueueConsumerContext? InterruptedContext { get; private set; }

            public IMessageQueueConsumerContext? RecoveredContext { get; private set; }

            public Task OnConsumerInterruptedAsync(IMessageQueueConsumerContext context)
            {
                InterruptedContext = context;
                return Task.CompletedTask;
            }

            public Task OnConsumerRecoveredAsync(IMessageQueueConsumerContext context)
            {
                RecoveredContext = context;
                return Task.CompletedTask;
            }
        }

        private sealed class CapturingCompensationStandard : IMessageQueueFailureCompensationStandard
        {
            public IMessageQueueFailureContext? Context { get; private set; }

            public Task OnFailedAsync(IMessageQueueFailureContext context)
            {
                Context = context;
                return Task.CompletedTask;
            }
        }
    }
}
