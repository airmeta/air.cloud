using Air.Cloud.Core.App;
using Air.Cloud.Core.Standard.KVCenter;
using Air.Cloud.Modules.Consul.Model;
using Air.Cloud.Modules.Consul.Service;
using Air.Cloud.Modules.Consul.Util;

using Consul;

using System.Text;

namespace Air.Cloud.IntegrationTest.Modules.Consul;

/// <summary>
/// <para>zh-cn:Consul 真实 KV 集成测试，开启后会连接配置中的 Consul 实例并执行真实 KV 写读删。</para>
/// <para>en-us:Real Consul KV integration tests. When enabled, they connect to the configured Consul instance and execute real KV write/read/delete.</para>
/// </summary>
/// <remarks>
/// <para>zh-cn:默认通过 ConsulIntegration:RunConsulTests 关闭；开启后 Consul 不可用应视为集成环境失败。</para>
/// <para>en-us:Disabled by ConsulIntegration:RunConsulTests by default; when enabled, unavailable Consul should be treated as integration-environment failure.</para>
/// </remarks>
public class ConsulRealKvIntegrationTests
{
    /// <summary>
    /// <para>zh-cn:验证 Consul KV 标准可以在真实 Consul 上完成添加、读取、按前缀查询和删除。</para>
    /// <para>en-us:Verifies the Consul KV standard can add, read, query by prefix, and delete on a real Consul instance.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "Consul")]
    public async Task Consul_should_add_query_get_and_delete_kv_value()
    {
        if (!IsEnabled())
        {
            return;
        }

        ConfigureConsulClient();
        IKVCenterStandard kvCenter = new ConsulKVCenterDependency();
        var prefix = GetPrefix();
        var key = $"{prefix}/kv-{Guid.NewGuid():N}";
        var value = $"consul-value-{Guid.NewGuid():N}";

        try
        {
            Assert.True(await kvCenter.AddOrUpdateAsync(key, value));

            var item = await kvCenter.GetAsync<ConsulKvCenterServiceInformation>(key);
            Assert.NotNull(item);
            Assert.Equal(key, item.Key);
            Assert.Equal(value, item.Value);

            var items = await kvCenter.QueryAsync<ConsulKvCenterServiceInformation>(prefix);
            Assert.Contains(items, current => current.Key == key && current.Value == value);
        }
        finally
        {
            await kvCenter.DeleteAsync(key);
        }

        var deleted = await kvCenter.GetAsync<ConsulKvCenterServiceInformation>(key);
        Assert.Null(deleted);
    }

    /// <summary>
    /// <para>zh-cn:验证真实 Consul KV 对同一个 Key 的 AddOrUpdate 会覆盖旧值，并且读取时返回最新值。</para>
    /// <para>en-us:Verifies AddOrUpdate overwrites an existing key in real Consul KV and reads return the latest value.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "Consul")]
    public async Task Consul_should_overwrite_existing_kv_value()
    {
        if (!IsEnabled())
        {
            return;
        }

        ConfigureConsulClient();
        IKVCenterStandard kvCenter = new ConsulKVCenterDependency();
        var key = $"{GetPrefix()}/kv-overwrite-{Guid.NewGuid():N}";

        try
        {
            Assert.True(await kvCenter.AddOrUpdateAsync(key, "value-v1"));
            Assert.True(await kvCenter.AddOrUpdateAsync(key, "value-v2"));

            var item = await kvCenter.GetAsync<ConsulKvCenterServiceInformation>(key);

            Assert.NotNull(item);
            Assert.Equal("value-v2", item.Value);
        }
        finally
        {
            await kvCenter.DeleteAsync(key);
        }
    }

    /// <summary>
    /// <para>zh-cn:验证真实 Consul KV 前缀查询会返回同一前缀下的多个 Key，并隔离其他前缀。</para>
    /// <para>en-us:Verifies real Consul KV prefix query returns multiple keys under the same prefix and isolates other prefixes.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "Consul")]
    public async Task Consul_should_query_multiple_kv_values_by_prefix()
    {
        if (!IsEnabled())
        {
            return;
        }

        ConfigureConsulClient();
        IKVCenterStandard kvCenter = new ConsulKVCenterDependency();
        var prefix = $"{GetPrefix()}/kv-prefix-{Guid.NewGuid():N}";
        var otherKey = $"{GetPrefix()}/kv-other-{Guid.NewGuid():N}";
        var firstKey = $"{prefix}/one";
        var secondKey = $"{prefix}/two";

        try
        {
            Assert.True(await kvCenter.AddOrUpdateAsync(firstKey, "1"));
            Assert.True(await kvCenter.AddOrUpdateAsync(secondKey, "2"));
            Assert.True(await kvCenter.AddOrUpdateAsync(otherKey, "3"));

            var items = await kvCenter.QueryAsync<ConsulKvCenterServiceInformation>(prefix);

            Assert.Equal(2, items.Count);
            Assert.Contains(items, item => item.Key == firstKey && item.Value == "1");
            Assert.Contains(items, item => item.Key == secondKey && item.Value == "2");
            Assert.DoesNotContain(items, item => item.Key == otherKey);
        }
        finally
        {
            await kvCenter.DeleteAsync(firstKey);
            await kvCenter.DeleteAsync(secondKey);
            await kvCenter.DeleteAsync(otherKey);
        }
    }

    /// <summary>
    /// <para>zh-cn:验证真实 Consul KV 删除不存在 Key 时返回 false，调用方可以区分“未删除任何数据”。</para>
    /// <para>en-us:Verifies deleting a missing key from real Consul KV returns false so callers can distinguish no-op deletion.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "Consul")]
    public async Task Consul_should_return_false_when_deleting_missing_key()
    {
        if (!IsEnabled())
        {
            return;
        }

        ConfigureConsulClient();
        IKVCenterStandard kvCenter = new ConsulKVCenterDependency();
        var key = $"{GetPrefix()}/kv-missing-{Guid.NewGuid():N}";

        await kvCenter.DeleteAsync(key);

        Assert.False(await kvCenter.DeleteAsync(key));
    }

    /// <summary>
    /// <para>zh-cn:验证 Consul 远程配置可以从真实 KV 加载 JSON，并且 KV 更新后 ReloadOnChange 能刷新配置值。</para>
    /// <para>en-us:Verifies Consul remote configuration can load JSON from real KV and ReloadOnChange refreshes configuration values after KV updates.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "Consul")]
    public async Task Consul_configuration_should_load_and_refresh_kv_json_value()
    {
        if (!IsEnabled())
        {
            return;
        }

        ConfigureConsulClient();
        var serviceName = $"air.cloud.integration.{Guid.NewGuid():N}";
        var servicePath = $"{serviceName.Replace(".", "/")}/{AppConst.SystemEnvironmentConfigFileFullName}";
        var firstValue = $"config-v1-{Guid.NewGuid():N}";
        var secondValue = $"config-v2-{Guid.NewGuid():N}";

        try
        {
            await PutRawKvAsync(servicePath, BuildJsonConfiguration(firstValue));

            var options = new ConsulServiceOptions
            {
                ConsulAddress = GetRequiredConfiguration("ConsulIntegration:Address"),
                ServiceName = serviceName,
                EnableCommonConfig = false
            };
            var configuration = ConfigurationLoader.LoadRemoteConfiguration(options).Item1;

            Assert.Equal(firstValue, configuration["Feature:Flag"]);

            await PutRawKvAsync(servicePath, BuildJsonConfiguration(secondValue));
            var refreshed = await WaitUntilAsync(
                () => string.Equals(configuration["Feature:Flag"], secondValue, StringComparison.Ordinal),
                TimeSpan.FromSeconds(20));

            Assert.True(refreshed, "Consul configuration did not refresh to the latest KV value within the timeout.");
            Assert.Equal(secondValue, configuration["Feature:Flag"]);
        }
        finally
        {
            await DeleteRawKvAsync(servicePath);
        }
    }

    /// <summary>
    /// <para>zh-cn:验证开启公共配置后，ConfigurationLoader 会同时加载服务配置与公共配置两个配置源。</para>
    /// <para>en-us:Verifies ConfigurationLoader loads both service configuration and common configuration when common config is enabled.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "Consul")]
    public async Task Consul_configuration_should_load_common_kv_json_value()
    {
        if (!IsEnabled())
        {
            return;
        }

        ConfigureConsulClient();
        var serviceName = $"air.cloud.integration.{Guid.NewGuid():N}";
        var commonRoute = $"{GetPrefix()}/common-{Guid.NewGuid():N}";
        var servicePath = $"{serviceName.Replace(".", "/")}/{AppConst.SystemEnvironmentConfigFileFullName}";
        var commonPath = $"{commonRoute}/{AppConst.CommonEnvironmentConfigFileFullName}";
        var serviceValue = $"service-{Guid.NewGuid():N}";
        var commonValue = $"common-{Guid.NewGuid():N}";

        try
        {
            await PutRawKvAsync(servicePath, BuildJsonConfiguration(serviceValue));
            await PutRawKvAsync(commonPath, BuildJsonConfiguration(commonValue));

            var options = new ConsulServiceOptions
            {
                ConsulAddress = GetRequiredConfiguration("ConsulIntegration:Address"),
                ServiceName = serviceName,
                EnableCommonConfig = true,
                CommonConfigFileRoute = commonRoute
            };
            var (serviceConfiguration, commonConfiguration) = ConfigurationLoader.LoadRemoteConfiguration(options);

            Assert.Equal(serviceValue, serviceConfiguration["Feature:Flag"]);
            Assert.NotNull(commonConfiguration);
            Assert.Equal(commonValue, commonConfiguration["Feature:Flag"]);
        }
        finally
        {
            await DeleteRawKvAsync(servicePath);
            await DeleteRawKvAsync(commonPath);
        }
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

    private static string BuildJsonConfiguration(string featureFlag)
    {
        return $$"""
        {
          "Feature": {
            "Flag": "{{featureFlag}}"
          }
        }
        """;
    }

    private static async Task PutRawKvAsync(string key, string value)
    {
        var result = await ConsulServerCenterDependency.ConsulClient.KV.Put(new KVPair(key)
        {
            Value = Encoding.UTF8.GetBytes(value)
        });

        Assert.Equal(System.Net.HttpStatusCode.OK, result.StatusCode);
    }

    private static async Task DeleteRawKvAsync(string key)
    {
        await ConsulServerCenterDependency.ConsulClient.KV.Delete(key);
    }

    private static async Task<bool> WaitUntilAsync(Func<bool> predicate, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow.Add(timeout);
        while (DateTime.UtcNow < deadline)
        {
            if (predicate())
            {
                return true;
            }

            await Task.Delay(250);
        }

        return predicate();
    }
}
