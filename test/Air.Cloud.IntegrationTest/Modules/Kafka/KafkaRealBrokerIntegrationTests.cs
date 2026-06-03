using Air.Cloud.Core.App;
using Air.Cloud.Core.Enums;
using Air.Cloud.Core.Standard.MessageQueue;
using Air.Cloud.Core.Standard.MessageQueue.Enums;
using Air.Cloud.Modules.Kafka.Config;
using Air.Cloud.Modules.Kafka.Contexts;
using Air.Cloud.Modules.Kafka.Extensions;
using Air.Cloud.Modules.Kafka.Model;

using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;

using System.Reflection;

namespace Air.Cloud.IntegrationTest.Modules.Kafka;

/// <summary>
/// <para>zh-cn:Kafka 真实 Broker 集成测试，启用后会连接配置中的 Kafka 集群并执行真实发布/消费往返。</para>
/// <para>en-us:Real Kafka broker integration tests. When enabled, they connect to the configured Kafka cluster and execute real publish/consume round trips.</para>
/// </summary>
/// <remarks>
/// <para>zh-cn:默认通过 KafkaIntegration:RunKafkaTests 关闭，避免没有 Kafka 环境时影响普通测试流程；开启后 Kafka 不可用会导致测试失败。</para>
/// <para>en-us:They are disabled by KafkaIntegration:RunKafkaTests by default so normal test runs are not affected by missing Kafka environments. When enabled, unavailable Kafka causes test failure.</para>
/// </remarks>
public class KafkaRealBrokerIntegrationTests
{
    private const string SeedPayload = "__air_cloud_topic_seed__";

    /// <summary>
    /// <para>zh-cn:验证默认 int Key 模式可以通过 Kafka 模块完成真实发布与消费。</para>
    /// <para>en-us:Verifies that the default int-key mode can complete a real publish and consume round trip through the Kafka module.</para>
    /// </summary>
    /// <returns>
    /// <para>zh-cn:表示异步测试过程。</para>
    /// <para>en-us:Represents the asynchronous test process.</para>
    /// </returns>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "Kafka")]
    public async Task Kafka_should_publish_and_consume_message_with_int_key()
    {
        if (!IsEnabled())
        {
            return;
        }

        var bootstrapServers = GetRequiredConfiguration("KafkaIntegration:BootstrapServers");
        var topicName = BuildTopicName("int-key");
        var groupId = BuildGroupId("int-key");

        await WithTemporaryTopicAsync(bootstrapServers, topicName, typeof(int), async () =>
        {
            var recovery = new CapturingConsumerRecoveryStandard();
            using var moduleHost = BuildKafkaModuleHost(bootstrapServers, services =>
            {
                services.AddSingleton<IMessageQueueConsumerRecoveryStandard>(recovery);
            });
            var queue = moduleHost.ServiceProvider.GetRequiredService<IMessageQueueStandard>();
            var received = new TaskCompletionSource<TestKafkaMessage>(TaskCreationOptions.RunContinuationsAsynchronously);

            queue.Subscribe<ConsumerConfig, TestKafkaMessage>(
                new ConsumerConfigModel
                {
                    TopicName = topicName,
                    KeyType = typeof(int),
                    Config = BuildConsumerConfig(bootstrapServers, groupId)
                },
                message =>
                {
                    if (message.Content == "int-key-payload")
                    {
                        received.TrySetResult(message);
                    }
                },
                groupId);

            queue.Publish<ProducerConfig, TestKafkaMessage>(
                new ProducerConfigModel
                {
                    TopicName = topicName,
                    KeyType = typeof(int),
                    Config = new ProducerConfig { BootstrapServers = bootstrapServers }
                },
                new TestKafkaMessage { Content = "int-key-payload" });

            var message = await WaitForMessageAsync(received.Task);

            Assert.Equal("int-key-payload", message.Content);
        });
    }

    /// <summary>
    /// <para>zh-cn:验证自定义 string Key 生成器可以通过 Kafka 模块完成真实发布与消费。</para>
    /// <para>en-us:Verifies that a custom string key generator can complete a real publish and consume round trip through the Kafka module.</para>
    /// </summary>
    /// <returns>
    /// <para>zh-cn:表示异步测试过程。</para>
    /// <para>en-us:Represents the asynchronous test process.</para>
    /// </returns>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "Kafka")]
    public async Task Kafka_should_publish_and_consume_message_with_string_key()
    {
        if (!IsEnabled())
        {
            return;
        }

        var bootstrapServers = GetRequiredConfiguration("KafkaIntegration:BootstrapServers");
        var topicName = BuildTopicName("string-key");
        var groupId = BuildGroupId("string-key");

        await WithTemporaryTopicAsync(bootstrapServers, topicName, typeof(string), async () =>
        {
            var recovery = new CapturingConsumerRecoveryStandard();
            using var moduleHost = BuildKafkaModuleHost(bootstrapServers, services =>
            {
                services.AddSingleton<IMessageQueueConsumerRecoveryStandard>(recovery);
                services.AddSingleton<IMessageQueueKeyGenerator<string>, TestStringKeyGenerator>();
            });
            var queue = moduleHost.ServiceProvider.GetRequiredService<IMessageQueueStandard>();
            var received = new TaskCompletionSource<TestKafkaMessage>(TaskCreationOptions.RunContinuationsAsynchronously);

            queue.Subscribe<ConsumerConfig, TestKafkaMessage>(
                new ConsumerConfigModel
                {
                    TopicName = topicName,
                    KeyType = typeof(string),
                    Config = BuildConsumerConfig(bootstrapServers, groupId)
                },
                message =>
                {
                    if (message.Content == "string-key-payload")
                    {
                        received.TrySetResult(message);
                    }
                },
                groupId);

            queue.Publish<ProducerConfig, TestKafkaMessage>(
                new ProducerConfigModel
                {
                    TopicName = topicName,
                    KeyType = typeof(string),
                    Config = new ProducerConfig { BootstrapServers = bootstrapServers }
                },
                new TestKafkaMessage { Content = "string-key-payload" });

            var message = await WaitForMessageAsync(received.Task);

            Assert.Equal("string-key-payload", message.Content);
        });
    }

    /// <summary>
    /// <para>zh-cn:验证业务处理超时时会触发 Kafka 模块的失败补偿抽象。</para>
    /// <para>en-us:Verifies that business handling timeout triggers the Kafka module failure compensation abstraction.</para>
    /// </summary>
    /// <returns>
    /// <para>zh-cn:表示异步测试过程。</para>
    /// <para>en-us:Represents the asynchronous test process.</para>
    /// </returns>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "Kafka")]
    public async Task Kafka_should_trigger_failure_compensation_when_handler_timeout()
    {
        if (!IsEnabled())
        {
            return;
        }

        var bootstrapServers = GetRequiredConfiguration("KafkaIntegration:BootstrapServers");
        var topicName = BuildTopicName("timeout");
        var groupId = BuildGroupId("timeout");
        var compensation = new CapturingFailureCompensationStandard();

        await WithTemporaryTopicAsync(bootstrapServers, topicName, typeof(int), async () =>
        {
            var recovery = new CapturingConsumerRecoveryStandard();
            using var moduleHost = BuildKafkaModuleHost(bootstrapServers, services =>
            {
                services.AddSingleton<IMessageQueueConsumerRecoveryStandard>(recovery);
                services.AddSingleton<IMessageQueueConsumerExecutionOptions>(new TestConsumerExecutionOptions(TimeSpan.FromMilliseconds(100), 0));
                services.AddSingleton<IMessageQueueFailureCompensationStandard>(compensation);
            });
            var queue = moduleHost.ServiceProvider.GetRequiredService<IMessageQueueStandard>();

            queue.Subscribe<ConsumerConfig, TestKafkaMessage>(
                new ConsumerConfigModel
                {
                    TopicName = topicName,
                    KeyType = typeof(int),
                    Config = BuildConsumerConfig(bootstrapServers, groupId)
                },
                message =>
                {
                    if (message.Content == "timeout-payload")
                    {
                        Thread.Sleep(TimeSpan.FromSeconds(2));
                    }
                },
                groupId);

            queue.Publish<ProducerConfig, TestKafkaMessage>(
                new ProducerConfigModel
                {
                    TopicName = topicName,
                    KeyType = typeof(int),
                    Config = new ProducerConfig { BootstrapServers = bootstrapServers }
                },
                new TestKafkaMessage { Content = "timeout-payload" });

            var context = await WaitForMessageAsync(compensation.Failed.Task);

            Assert.Equal(MessageQueueFailureReason.Timeout, context.FailureReason);
            Assert.True(context.IsTimeout);
            Assert.Equal("timeout-payload", ((TestKafkaMessage)context.MessageContent).Content);
        });
    }

    /// <summary>
    /// <para>zh-cn:验证业务处理异常时会触发 Kafka 模块的失败补偿抽象。</para>
    /// <para>en-us:Verifies that business handling exceptions trigger the Kafka module failure compensation abstraction.</para>
    /// </summary>
    /// <returns>
    /// <para>zh-cn:表示异步测试过程。</para>
    /// <para>en-us:Represents the asynchronous test process.</para>
    /// </returns>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "Kafka")]
    public async Task Kafka_should_trigger_failure_compensation_when_handler_throws()
    {
        if (!IsEnabled())
        {
            return;
        }

        var bootstrapServers = GetRequiredConfiguration("KafkaIntegration:BootstrapServers");
        var topicName = BuildTopicName("exception");
        var groupId = BuildGroupId("exception");
        var compensation = new CapturingFailureCompensationStandard();

        await WithTemporaryTopicAsync(bootstrapServers, topicName, typeof(int), async () =>
        {
            var recovery = new CapturingConsumerRecoveryStandard();
            using var moduleHost = BuildKafkaModuleHost(bootstrapServers, services =>
            {
                services.AddSingleton<IMessageQueueConsumerRecoveryStandard>(recovery);
                services.AddSingleton<IMessageQueueFailureCompensationStandard>(compensation);
            });
            var queue = moduleHost.ServiceProvider.GetRequiredService<IMessageQueueStandard>();

            queue.Subscribe<ConsumerConfig, TestKafkaMessage>(
                new ConsumerConfigModel
                {
                    TopicName = topicName,
                    KeyType = typeof(int),
                    Config = BuildConsumerConfig(bootstrapServers, groupId)
                },
                message =>
                {
                    if (message.Content == "exception-payload")
                    {
                        throw new InvalidOperationException("handler failed");
                    }
                },
                groupId);

            queue.Publish<ProducerConfig, TestKafkaMessage>(
                new ProducerConfigModel
                {
                    TopicName = topicName,
                    KeyType = typeof(int),
                    Config = new ProducerConfig { BootstrapServers = bootstrapServers }
                },
                new TestKafkaMessage { Content = "exception-payload" });

            var context = await WaitForMessageAsync(compensation.Failed.Task);

            Assert.Equal(MessageQueueFailureReason.Exception, context.FailureReason);
            Assert.False(context.IsTimeout);
            Assert.Equal("exception-payload", ((TestKafkaMessage)context.MessageContent).Content);
        });
    }

    /// <summary>
    /// <para>zh-cn:验证消费到错误 Key 类型的记录时会触发中断与失败补偿，并跳过该记录继续处理后续消息。</para>
    /// <para>en-us:Verifies that a record with an invalid key type triggers interruption and failure compensation, then is skipped so later messages can still be handled.</para>
    /// </summary>
    /// <returns>
    /// <para>zh-cn:表示异步测试过程。</para>
    /// <para>en-us:Represents the asynchronous test process.</para>
    /// </returns>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "Kafka")]
    public async Task Kafka_should_report_and_skip_bad_key_record_then_consume_following_message()
    {
        if (!IsEnabled())
        {
            return;
        }

        var bootstrapServers = GetRequiredConfiguration("KafkaIntegration:BootstrapServers");
        var topicName = BuildTopicName("bad-key-skip");
        var groupId = BuildGroupId("bad-key-skip");
        var recovery = new CapturingConsumerRecoveryStandard();
        var compensation = new CapturingFailureCompensationStandard();

        await WithTemporaryTopicAsync(bootstrapServers, topicName, typeof(int), async () =>
        {
            await ProduceNullKeyRecordAsync(bootstrapServers, topicName, "bad-key-payload");

            using var moduleHost = BuildKafkaModuleHost(bootstrapServers, services =>
            {
                services.AddSingleton<IMessageQueueConsumerRecoveryStandard>(recovery);
                services.AddSingleton<IMessageQueueFailureCompensationStandard>(compensation);
            });
            var queue = moduleHost.ServiceProvider.GetRequiredService<IMessageQueueStandard>();
            var received = new TaskCompletionSource<TestKafkaMessage>(TaskCreationOptions.RunContinuationsAsynchronously);

            queue.Subscribe<ConsumerConfig, TestKafkaMessage>(
                new ConsumerConfigModel
                {
                    TopicName = topicName,
                    KeyType = typeof(int),
                    Config = BuildConsumerConfig(bootstrapServers, groupId)
                },
                message =>
                {
                    if (message.Content == "after-bad-key-payload")
                    {
                        received.TrySetResult(message);
                    }
                },
                groupId);

            queue.Publish<ProducerConfig, TestKafkaMessage>(
                new ProducerConfigModel
                {
                    TopicName = topicName,
                    KeyType = typeof(int),
                    Config = new ProducerConfig { BootstrapServers = bootstrapServers }
                },
                new TestKafkaMessage { Content = "after-bad-key-payload" });

            var interruption = await WaitForMessageAsync(recovery.Interrupted.Task);
            var failure = await WaitForMessageAsync(compensation.Failed.Task);
            var message = await WaitForMessageAsync(received.Task);

            Assert.Equal(MessageQueueConsumerInterruptedReason.ConnectionError, interruption.InterruptedReason);
            Assert.Equal(MessageQueueFailureReason.ConsumerInterrupted, failure.FailureReason);
            Assert.Equal("after-bad-key-payload", message.Content);
        });
    }

    private static KafkaModuleHost BuildKafkaModuleHost(string bootstrapServers, Action<IServiceCollection>? configureServices = null)
    {
        var services = new ServiceCollection();
        services.AddOptions<KafkaSettingsOptions>().Configure(options =>
        {
            options.ClusterAddress = bootstrapServers;
            options.ProducerConfigs = new List<ProducerConfigModel>();
            options.ConsumerConfigs = new List<ConsumerConfigModel>();
        });
        services.AddSingleton<IMessageQueueConsumerRecoveryStandard, NoopConsumerRecoveryStandard>();
        services.AddSingleton<IMessageQueueFailureCompensationStandard, NoopFailureCompensationStandard>();

        configureServices?.Invoke(services);

        var originalRootServices = AppCore.RootServices;
        var originalInternalServices = AppCore.InternalServices;
        var originalStartType = AppCore.AppStartType;
        var originalAssemblies = AppCore.Assemblies;

        AppCore.Assemblies = new[] { typeof(KafkaModuleExtensions).Assembly.GetName() };
        services.AddKafkaService();

        var serviceProvider = services.BuildServiceProvider();
        AppCore.RootServices = serviceProvider;
        AppCore.InternalServices = services;
        AppCore.AppStartType = AppStartupTypeEnum.WEB;

        return new KafkaModuleHost(
            serviceProvider,
            originalRootServices,
            originalInternalServices,
            originalStartType,
            originalAssemblies);
    }

    private static ConsumerConfig BuildConsumerConfig(string bootstrapServers, string groupId)
    {
        return new ConsumerConfig
        {
            BootstrapServers = bootstrapServers,
            GroupId = groupId,
            EnableAutoCommit = false,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            AllowAutoCreateTopics = true,
            PartitionAssignmentStrategy = PartitionAssignmentStrategy.Range,
            MaxPollIntervalMs = 300000,
            SessionTimeoutMs = 10000
        };
    }

    private static async Task WithTemporaryTopicAsync(string bootstrapServers, string topicName, Type keyType, Func<Task> action)
    {
        await EnsureTopicAutoCreatedAsync(bootstrapServers, topicName, keyType);
        await action.Invoke();
    }

    private static async Task EnsureTopicAutoCreatedAsync(string bootstrapServers, string topicName, Type keyType)
    {
        var value = $"{{\"Content\":\"{SeedPayload}\"}}";

        if (keyType == typeof(string))
        {
            using var producer = new ProducerBuilder<string, string>(new ProducerConfig
            {
                BootstrapServers = bootstrapServers,
                MessageTimeoutMs = 10000
            }).Build();

            await producer.ProduceAsync(topicName, new Message<string, string>
            {
                Key = "seed",
                Value = value
            });

            producer.Flush(TimeSpan.FromSeconds(10));
            return;
        }

        using (var producer = new ProducerBuilder<int, string>(new ProducerConfig
        {
            BootstrapServers = bootstrapServers,
            MessageTimeoutMs = 10000
        }).Build())
        {
            await producer.ProduceAsync(topicName, new Message<int, string>
            {
                Key = 0,
                Value = value
            });

            producer.Flush(TimeSpan.FromSeconds(10));
        }
    }

    private static async Task ProduceNullKeyRecordAsync(string bootstrapServers, string topicName, string content)
    {
        using var producer = new ProducerBuilder<Null, string>(new ProducerConfig
        {
            BootstrapServers = bootstrapServers,
            MessageTimeoutMs = 10000
        }).Build();

        await producer.ProduceAsync(topicName, new Message<Null, string>
        {
            Value = $"{{\"Content\":\"{content}\"}}"
        });

        producer.Flush(TimeSpan.FromSeconds(10));
    }

    private static async Task<T> WaitForMessageAsync<T>(Task<T> task)
    {
        var timeoutSeconds = int.Parse(GetRequiredConfiguration("KafkaIntegration:TimeoutSeconds"));
        var completed = await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(timeoutSeconds)));
        if (completed != task)
        {
            throw new TimeoutException($"Kafka integration test did not receive the expected event within {timeoutSeconds} seconds.");
        }

        return await task;
    }

    private static bool IsEnabled()
    {
        return string.Equals(
            AppConfigurationLoader.InnerConfiguration["KafkaIntegration:RunKafkaTests"],
            "true",
            StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildTopicName(string suffix)
    {
        var prefix = GetRequiredConfiguration("KafkaIntegration:TopicPrefix");
        return $"{prefix}-{suffix}-{Guid.NewGuid():N}";
    }

    private static string BuildGroupId(string suffix)
    {
        return $"air-cloud-it-{suffix}-{Guid.NewGuid():N}";
    }

    private static string GetRequiredConfiguration(string key)
    {
        var value = AppConfigurationLoader.InnerConfiguration[key];
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"{key} is required.");
        }

        return value;
    }

    private sealed class KafkaModuleHost : IDisposable
    {
        private readonly IServiceProvider _originalRootServices;
        private readonly IServiceCollection _originalInternalServices;
        private readonly AppStartupTypeEnum _originalStartType;
        private readonly IEnumerable<AssemblyName> _originalAssemblies;

        public KafkaModuleHost(
            ServiceProvider serviceProvider,
            IServiceProvider originalRootServices,
            IServiceCollection originalInternalServices,
            AppStartupTypeEnum originalStartType,
            IEnumerable<AssemblyName> originalAssemblies)
        {
            ServiceProvider = serviceProvider;
            _originalRootServices = originalRootServices;
            _originalInternalServices = originalInternalServices;
            _originalStartType = originalStartType;
            _originalAssemblies = originalAssemblies;
        }

        public ServiceProvider ServiceProvider { get; }

        public void Dispose()
        {
            AppCore.RootServices = _originalRootServices;
            AppCore.InternalServices = _originalInternalServices;
            AppCore.AppStartType = _originalStartType;
            AppCore.Assemblies = _originalAssemblies;
            ServiceProvider.Dispose();
        }
    }

    private sealed class TestKafkaMessage
    {
        public string Content { get; set; } = string.Empty;
    }

    private sealed class TestStringKeyGenerator : IMessageQueueKeyGenerator<string>
    {
        public string Generate(IMessageQueueKeyGenerationContext context)
        {
            return $"{context.TopicName}-string-key";
        }
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

    private sealed class CapturingFailureCompensationStandard : IMessageQueueFailureCompensationStandard
    {
        public TaskCompletionSource<IMessageQueueFailureContext> Failed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task OnFailedAsync(IMessageQueueFailureContext context)
        {
            Failed.TrySetResult(context);
            return Task.CompletedTask;
        }
    }

    private sealed class CapturingConsumerRecoveryStandard : IMessageQueueConsumerRecoveryStandard
    {
        public TaskCompletionSource<IMessageQueueConsumerContext> Assigned { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource<IMessageQueueConsumerContext> Interrupted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task OnConsumerInterruptedAsync(IMessageQueueConsumerContext context)
        {
            Interrupted.TrySetResult(context);
            return Task.CompletedTask;
        }

        public Task OnConsumerRecoveredAsync(IMessageQueueConsumerContext context)
        {
            if (context is KafkaMessageQueueConsumerContext { NativeEventArgs: not null })
            {
                Assigned.TrySetResult(context);
            }

            return Task.CompletedTask;
        }
    }

    private sealed class NoopConsumerRecoveryStandard : IMessageQueueConsumerRecoveryStandard
    {
        public Task OnConsumerInterruptedAsync(IMessageQueueConsumerContext context)
        {
            return Task.CompletedTask;
        }

        public Task OnConsumerRecoveredAsync(IMessageQueueConsumerContext context)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class NoopFailureCompensationStandard : IMessageQueueFailureCompensationStandard
    {
        public Task OnFailedAsync(IMessageQueueFailureContext context)
        {
            return Task.CompletedTask;
        }
    }
}
