using Air.Cloud.Core.App;
using Air.Cloud.Core.Enums;
using Air.Cloud.Core.Standard.MessageQueue;
using Air.Cloud.Modules.RocketMQ.Config;
using Air.Cloud.Modules.RocketMQ.Extensions;
using Air.Cloud.Modules.RocketMQ.Helper;
using Air.Cloud.Modules.RocketMQ.Model;

using Microsoft.Extensions.DependencyInjection;

using System.Reflection;

namespace Air.Cloud.IntegrationTest.Modules.RocketMQ;

/// <summary>
/// <para>zh-cn:RocketMQ 真实 Proxy 集成测试，启用后会连接配置中的 RocketMQ 5.x Proxy 并执行真实发布/消费往返。</para>
/// <para>en-us:Real RocketMQ Proxy integration tests. When enabled, they connect to the configured RocketMQ 5.x Proxy and execute real publish/consume round trips.</para>
/// </summary>
/// <remarks>
/// <para>zh-cn:默认通过 RocketMQIntegration:RunRocketMQTests 关闭，避免没有 RocketMQ 5.x Proxy 环境时影响普通测试流程；开启后 RocketMQ 不可用会导致测试失败。</para>
/// <para>en-us:They are disabled by RocketMQIntegration:RunRocketMQTests by default so normal test runs are not affected by missing RocketMQ 5.x Proxy environments. When enabled, unavailable RocketMQ causes test failure.</para>
/// </remarks>
public class RocketMQRealProxyIntegrationTests
{
    /// <summary>
    /// <para>zh-cn:验证 RocketMQ 模块可以通过标准发布与订阅接口完成真实消息往返。</para>
    /// <para>en-us:Verifies that the RocketMQ module can complete a real message round trip through the standard publish and subscribe interfaces.</para>
    /// </summary>
    /// <returns>
    /// <para>zh-cn:表示异步测试过程。</para>
    /// <para>en-us:Represents the asynchronous test process.</para>
    /// </returns>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "RocketMQ")]
    public async Task RocketMQ_should_publish_and_consume_message()
    {
        if (!IsEnabled())
        {
            return;
        }

        var endpoints = GetRequiredConfiguration("RocketMQIntegration:Endpoints");
        var topicName = GetRequiredConfiguration("RocketMQIntegration:Topic");
        var groupId = BuildGroupId("roundtrip");

        using var moduleHost = BuildRocketMQModuleHost(endpoints);
        var queue = moduleHost.ServiceProvider.GetRequiredService<IMessageQueueStandard>();
        var received = new TaskCompletionSource<TestRocketMQMessage>(TaskCreationOptions.RunContinuationsAsynchronously);

        queue.Subscribe<RocketMQConsumerConfig, TestRocketMQMessage>(
            new RocketMQConsumerConfigModel
            {
                TopicName = topicName,
                Config = new RocketMQConsumerConfig
                {
                    Endpoints = endpoints,
                    ConsumerGroup = groupId,
                    BatchSize = 8,
                    AwaitDuration = TimeSpan.FromSeconds(5),
                    InvisibleDuration = TimeSpan.FromSeconds(30)
                }
            },
            message =>
            {
                if (message.Content == "rocketmq-roundtrip-payload")
                {
                    received.TrySetResult(message);
                }
            },
            groupId);

        queue.Publish<RocketMQProducerConfig, TestRocketMQMessage>(
            new RocketMQProducerConfigModel
            {
                TopicName = topicName,
                Config = new RocketMQProducerConfig { Endpoints = endpoints }
            },
            new TestRocketMQMessage { Content = "rocketmq-roundtrip-payload" });

        var message = await WaitForMessageAsync(received.Task);
        Assert.Equal("rocketmq-roundtrip-payload", message.Content);
    }

    private static RocketMQModuleHost BuildRocketMQModuleHost(string endpoints)
    {
        var services = new ServiceCollection();
        services.AddOptions<RocketMQSettingsOptions>().Configure(options =>
        {
            options.Endpoints = endpoints;
            options.ProducerConfigs = new List<RocketMQProducerConfigModel>();
            options.ConsumerConfigs = new List<RocketMQConsumerConfigModel>();
        });

        var originalRootServices = AppCore.RootServices;
        var originalInternalServices = AppCore.InternalServices;
        var originalStartType = AppCore.AppStartType;
        var originalAssemblies = AppCore.Assemblies;

        AppCore.Assemblies = new[] { typeof(RocketMQModuleExtensions).Assembly.GetName() };
        services.AddRocketMQService();

        var serviceProvider = services.BuildServiceProvider();
        AppCore.RootServices = serviceProvider;
        AppCore.InternalServices = services;
        AppCore.AppStartType = AppStartupTypeEnum.WEB;

        return new RocketMQModuleHost(
            serviceProvider,
            originalRootServices,
            originalInternalServices,
            originalStartType,
            originalAssemblies);
    }

    private static async Task<T> WaitForMessageAsync<T>(Task<T> task)
    {
        var timeoutSeconds = int.Parse(GetRequiredConfiguration("RocketMQIntegration:TimeoutSeconds"));
        var completed = await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(timeoutSeconds)));
        if (completed != task)
        {
            throw new TimeoutException($"RocketMQ integration test did not receive the expected event within {timeoutSeconds} seconds.");
        }

        return await task;
    }

    private static bool IsEnabled()
    {
        return string.Equals(
            AppConfigurationLoader.InnerConfiguration["RocketMQIntegration:RunRocketMQTests"],
            "true",
            StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildGroupId(string suffix)
    {
        return $"air-cloud-rocketmq-it-{suffix}-{Guid.NewGuid():N}";
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

    private sealed class RocketMQModuleHost : IDisposable
    {
        private readonly IServiceProvider _originalRootServices;
        private readonly IServiceCollection _originalInternalServices;
        private readonly AppStartupTypeEnum _originalStartType;
        private readonly IEnumerable<AssemblyName> _originalAssemblies;

        public RocketMQModuleHost(
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

    private sealed class TestRocketMQMessage
    {
        public string Content { get; set; } = string.Empty;
    }
}
