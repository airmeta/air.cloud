using Air.Cloud.Core.App;
using Air.Cloud.Core.Standard.ServerCenter;
using Air.Cloud.Modules.Consul.Model;
using Air.Cloud.Modules.Consul.Service;

using Consul;

using System.Reflection;

namespace Air.Cloud.IntegrationTest.Modules.Consul;

/// <summary>
/// <para>zh-cn:Consul 真实服务中心集成测试，开启后会连接配置中的 Consul 实例并执行真实服务注册、发现和注销。</para>
/// <para>en-us:Real Consul server center integration tests. When enabled, they connect to the configured Consul instance and execute real service registration, discovery, and deregistration.</para>
/// </summary>
/// <remarks>
/// <para>zh-cn:默认通过 ConsulIntegration:RunConsulTests 控制；开启后 Consul 不可用应视为集成环境失败。</para>
/// <para>en-us:Controlled by ConsulIntegration:RunConsulTests; when enabled, unavailable Consul should be treated as integration-environment failure.</para>
/// </remarks>
public class ConsulRealServerCenterIntegrationTests
{
    /// <summary>
    /// <para>zh-cn:验证 Consul 服务中心标准可以在真实 Consul 中注册服务，并通过服务列表查询到该服务。</para>
    /// <para>en-us:Verifies the Consul server center standard can register a service in real Consul and discover it from the service list.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "Consul")]
    public async Task Consul_server_center_should_register_and_query_service()
    {
        if (!IsEnabled())
        {
            return;
        }

        ConfigureConsulClient();
        var serverCenter = new ConsulServerCenterDependency();
        var registration = CreateRegistration("query");

        try
        {
            Assert.True(await serverCenter.RegisterAsync(registration));

            var discovered = await WaitUntilAsync(async () =>
            {
                var services = await serverCenter.QueryAsync<ConsulServerCenterServiceOptions>();
                return services.Any(service => string.Equals(service.ServiceName, registration.ServiceName, StringComparison.Ordinal));
            }, TimeSpan.FromSeconds(10));

            Assert.True(discovered, "Registered Consul service was not visible in the service catalog within the timeout.");
        }
        finally
        {
            await serverCenter.Unregister(registration.ServiceKey);
        }
    }

    /// <summary>
    /// <para>zh-cn:验证真实 Consul 中按服务名获取详情时可以返回刚注册的服务实例。</para>
    /// <para>en-us:Verifies getting a service by name from real Consul returns the registered service instance details.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "Consul")]
    public async Task Consul_server_center_should_get_registered_service_details_by_name()
    {
        if (!IsEnabled())
        {
            return;
        }

        ConfigureConsulClient();
        var serverCenter = new ConsulServerCenterDependency();
        var registration = CreateRegistration("detail");

        try
        {
            Assert.True(await serverCenter.RegisterAsync(registration));

            var discovered = await WaitUntilAsync(async () =>
            {
                var details = await serverCenter.GetAsync(registration.ServiceName);
                var serverDetails = ExtractServerDetails(details);
                return serverDetails.Any(detail => string.Equals(detail.ServiceID, registration.ServiceKey, StringComparison.Ordinal)
                    && string.Equals(detail.ServiceName, registration.ServiceName, StringComparison.Ordinal)
                    && string.Equals(detail.ServiceAddress, "127.0.0.1", StringComparison.Ordinal)
                    && detail.ServicePort == 5099);
            }, TimeSpan.FromSeconds(10));

            Assert.True(discovered, "Registered Consul service detail was not visible in the service catalog within the timeout.");
        }
        finally
        {
            await serverCenter.Unregister(registration.ServiceKey);
        }
    }

    /// <summary>
    /// <para>zh-cn:验证健康检查路由未以斜杠开头时，Consul 注册实现会补齐斜杠并完成真实注册。</para>
    /// <para>en-us:Verifies the Consul registration implementation prefixes a missing slash on the health-check route and completes real registration.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "Consul")]
    public async Task Consul_server_center_should_normalize_health_check_route()
    {
        if (!IsEnabled())
        {
            return;
        }

        ConfigureConsulClient();
        var serverCenter = new ConsulServerCenterDependency();
        var registration = CreateRegistration("health");
        registration.HealthCheckRoute = "healthz";

        try
        {
            Assert.True(await serverCenter.RegisterAsync(registration));

            Assert.Equal("/healthz", registration.HealthCheckRoute);

            var discovered = await WaitUntilAsync(async () =>
            {
                var details = await serverCenter.GetAsync(registration.ServiceName);
                return ExtractServerDetails(details).Any(detail => string.Equals(detail.ServiceID, registration.ServiceKey, StringComparison.Ordinal));
            }, TimeSpan.FromSeconds(10));

            Assert.True(discovered, "Registered Consul service with normalized health-check route was not visible in the service catalog within the timeout.");
        }
        finally
        {
            await serverCenter.Unregister(registration.ServiceKey);
        }
    }

    /// <summary>
    /// <para>zh-cn:验证 Consul 服务中心实现可以注销已注册服务，并且服务详情最终不再包含该实例。</para>
    /// <para>en-us:Verifies the Consul server center implementation can deregister a registered service and the service detail eventually no longer contains that instance.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "Consul")]
    public async Task Consul_server_center_should_unregister_registered_service()
    {
        if (!IsEnabled())
        {
            return;
        }

        ConfigureConsulClient();
        var serverCenter = new ConsulServerCenterDependency();
        var registration = CreateRegistration("unregister");

        Assert.True(await serverCenter.RegisterAsync(registration));
        Assert.True(await WaitUntilAsync(async () =>
        {
            var details = await serverCenter.GetAsync(registration.ServiceName);
            return ExtractServerDetails(details).Any(detail => string.Equals(detail.ServiceID, registration.ServiceKey, StringComparison.Ordinal));
        }, TimeSpan.FromSeconds(10)));

        Assert.True(await serverCenter.Unregister(registration.ServiceKey));

        var removed = await WaitUntilAsync(async () =>
        {
            var details = await serverCenter.GetAsync(registration.ServiceName);
            return ExtractServerDetails(details).All(detail => !string.Equals(detail.ServiceID, registration.ServiceKey, StringComparison.Ordinal));
        }, TimeSpan.FromSeconds(10));

        Assert.True(removed, "Deregistered Consul service was still visible in the service catalog within the timeout.");
    }

    private static void ConfigureConsulClient()
    {
        var address = GetRequiredConfiguration("ConsulIntegration:Address");
        ConsulServerCenterDependency.ConsulClient = new ConsulClient(configuration =>
        {
            configuration.Address = new Uri(address);
        });
    }

    private static bool IsEnabled()
    {
        return string.Equals(
            AppConfigurationLoader.InnerConfiguration["ConsulIntegration:RunConsulTests"],
            "true",
            StringComparison.OrdinalIgnoreCase);
    }

    private static ConsulServerCenterServiceRegisterOptions CreateRegistration(string scenario)
    {
        var prefix = GetPrefix().Replace("/", "-", StringComparison.Ordinal);
        var suffix = Guid.NewGuid().ToString("N");
        return new ConsulServerCenterServiceRegisterOptions
        {
            ServiceName = $"{prefix}-server-center-{scenario}",
            ServiceKey = $"{prefix}-server-center-{scenario}-{suffix}",
            ServiceAddress = "http://127.0.0.1:5099",
            HealthCheckRoute = "/health",
            Timeout = TimeSpan.FromSeconds(2),
            DeregisterCriticalServiceAfter = TimeSpan.FromMinutes(1),
            HealthCheckTimeStep = TimeSpan.FromSeconds(5)
        };
    }

    private static string GetPrefix()
    {
        var prefix = AppConfigurationLoader.InnerConfiguration["ConsulIntegration:KeyPrefix"];
        if (string.IsNullOrWhiteSpace(prefix))
        {
            prefix = "air-cloud-it";
        }

        return prefix.TrimEnd('/');
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

    private static IList<ServerDetailOptions> ExtractServerDetails(object serviceDetails)
    {
        var property = serviceDetails.GetType().GetProperty("ServerDetails", BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(property);

        var details = Assert.IsAssignableFrom<IEnumerable<ServerDetailOptions>>(property.GetValue(serviceDetails));
        return details.ToList();
    }

    private static async Task<bool> WaitUntilAsync(Func<Task<bool>> predicate, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow.Add(timeout);
        while (DateTime.UtcNow < deadline)
        {
            if (await predicate())
            {
                return true;
            }

            await Task.Delay(250);
        }

        return await predicate();
    }
}
