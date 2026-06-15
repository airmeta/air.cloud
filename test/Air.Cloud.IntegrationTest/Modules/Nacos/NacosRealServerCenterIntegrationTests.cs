using Air.Cloud.Core.App;
using Air.Cloud.Modules.Nacos.Model;
using Air.Cloud.Modules.Nacos.Service;
using Air.Cloud.Modules.Nacos.Util;

using Microsoft.Extensions.DependencyInjection;

using Nacos.V2;
using Nacos.V2.DependencyInjection;

namespace Air.Cloud.IntegrationTest.Modules.Nacos;

/// <summary>
/// <para>zh-cn:Nacos 真实服务中心集成测试，开启后会连接配置中的 Nacos 实例并执行真实服务注册、发现和注销。</para>
/// <para>en-us:Real Nacos server-center integration tests. When enabled, they connect to the configured Nacos instance and execute real service registration, discovery, and deregistration.</para>
/// </summary>
public class NacosRealServerCenterIntegrationTests
{
    /// <summary>
    /// <para>zh-cn:验证 Nacos 服务中心标准可以在真实 Nacos 中注册服务，并通过服务列表查询到该服务。</para>
    /// <para>en-us:Verifies the Nacos server center standard can register a service in real Nacos and discover it from the service list.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "Nacos")]
    public async Task Nacos_server_center_should_register_and_query_service()
    {
        if (!IsEnabled())
        {
            return;
        }

        using var provider = BuildServiceProvider();
        var serverCenter = new NacosServerCenterDependency(provider.GetRequiredService<INacosNamingService>());
        var registration = CreateRegistration("query");

        try
        {
            Assert.True(await serverCenter.RegisterAsync(registration));

            var discovered = await WaitUntilAsync(async () =>
            {
                var services = await serverCenter.QueryAsync<NacosServerCenterServiceOptions>();
                return services.Any(service => string.Equals(service.ServiceName, registration.ServiceName, StringComparison.Ordinal));
            }, TimeSpan.FromSeconds(10));

            Assert.True(discovered, "Registered Nacos service was not visible in the service list within the timeout.");
        }
        finally
        {
            await serverCenter.UnregisterAsync(registration.ServiceName, registration.ServiceKey, registration.ServiceAddress, registration.GroupName);
        }
    }

    /// <summary>
    /// <para>zh-cn:验证真实 Nacos 中按服务名获取详情时可以返回刚注册的服务实例。</para>
    /// <para>en-us:Verifies getting a service by name from real Nacos returns the registered service instance details.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "Nacos")]
    public async Task Nacos_server_center_should_get_registered_service_details_by_name()
    {
        if (!IsEnabled())
        {
            return;
        }

        using var provider = BuildServiceProvider();
        var serverCenter = new NacosServerCenterDependency(provider.GetRequiredService<INacosNamingService>());
        var registration = CreateRegistration("detail");

        try
        {
            Assert.True(await serverCenter.RegisterAsync(registration));

            var discovered = await WaitUntilAsync(async () =>
            {
                var details = await serverCenter.GetAsync(registration.ServiceName);
                var service = Assert.IsType<NacosServerCenterServiceOptions>(details);
                return service.ServerDetails.Any(detail => string.Equals(detail.ServiceID, registration.ServiceKey, StringComparison.Ordinal)
                    && string.Equals(detail.ServiceName, registration.ServiceName, StringComparison.Ordinal)
                    && string.Equals(detail.ServiceAddress, "127.0.0.1", StringComparison.Ordinal)
                    && detail.ServicePort == 6099);
            }, TimeSpan.FromSeconds(10));

            Assert.True(discovered, "Registered Nacos service detail was not visible within the timeout.");
        }
        finally
        {
            await serverCenter.UnregisterAsync(registration.ServiceName, registration.ServiceKey, registration.ServiceAddress, registration.GroupName);
        }
    }

    private static ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddNacosV2Naming(NacosOptionsBuilder.Build(GetOptions()));
        return services.BuildServiceProvider();
    }

    private static bool IsEnabled()
    {
        return string.Equals(
            AppConfigurationLoader.InnerConfiguration["NacosIntegration:RunNacosTests"],
            "true",
            StringComparison.OrdinalIgnoreCase);
    }

    private static NacosServerCenterServiceRegisterOptions CreateRegistration(string scenario)
    {
        var suffix = Guid.NewGuid().ToString("N");
        var group = AppConfigurationLoader.InnerConfiguration["NacosIntegration:ServiceGroup"] ?? "DEFAULT_GROUP";
        return new NacosServerCenterServiceRegisterOptions
        {
            ServiceName = $"{GetPrefix()}-server-center-{scenario}",
            ServiceKey = $"{GetPrefix()}-server-center-{scenario}-{suffix}",
            ServiceAddress = "http://127.0.0.1:6099",
            HealthCheckRoute = "/health",
            Timeout = TimeSpan.FromSeconds(2),
            DeregisterCriticalServiceAfter = TimeSpan.FromMinutes(1),
            HealthCheckTimeStep = TimeSpan.FromSeconds(5),
            GroupName = group,
            ClusterName = "DEFAULT"
        };
    }

    private static string GetPrefix()
    {
        var prefix = AppConfigurationLoader.InnerConfiguration["NacosIntegration:KeyPrefix"];
        return string.IsNullOrWhiteSpace(prefix) ? "air-cloud-it" : prefix;
    }

    private static NacosServiceOptions GetOptions()
    {
        var address = AppConfigurationLoader.InnerConfiguration["NacosIntegration:Address"];
        if (string.IsNullOrWhiteSpace(address))
        {
            throw new InvalidOperationException("NacosIntegration:Address is required.");
        }

        return new NacosServiceOptions
        {
            ServerAddress = address,
            Namespace = AppConfigurationLoader.InnerConfiguration["NacosIntegration:Namespace"],
            UserName = AppConfigurationLoader.InnerConfiguration["NacosIntegration:UserName"],
            Password = AppConfigurationLoader.InnerConfiguration["NacosIntegration:Password"],
            ServiceGroup = AppConfigurationLoader.InnerConfiguration["NacosIntegration:ServiceGroup"] ?? "DEFAULT_GROUP"
        };
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
