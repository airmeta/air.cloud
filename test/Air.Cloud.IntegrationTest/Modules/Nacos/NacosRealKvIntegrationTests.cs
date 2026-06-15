using Air.Cloud.Core.App;
using Air.Cloud.Core.Standard.KVCenter;
using Air.Cloud.Modules.Nacos.Model;
using Air.Cloud.Modules.Nacos.Service;
using Air.Cloud.Modules.Nacos.Util;

using Microsoft.Extensions.DependencyInjection;

using Nacos.V2;
using Nacos.V2.DependencyInjection;

namespace Air.Cloud.IntegrationTest.Modules.Nacos;

/// <summary>
/// <para>zh-cn:Nacos 真实配置中心集成测试，开启后会连接配置中的 Nacos 实例并执行真实配置写入、读取和删除。</para>
/// <para>en-us:Real Nacos config-center integration tests. When enabled, they connect to the configured Nacos instance and execute real config write, read, and delete.</para>
/// </summary>
public class NacosRealKvIntegrationTests
{
    /// <summary>
    /// <para>zh-cn:验证 Nacos KV 标准可以在真实 Nacos 配置中心完成添加、读取、查询和删除。</para>
    /// <para>en-us:Verifies the Nacos KV standard can add, read, query, and delete on a real Nacos config center.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "Nacos")]
    public async Task Nacos_should_add_query_get_and_delete_kv_value()
    {
        if (!IsEnabled())
        {
            return;
        }

        using var provider = BuildServiceProvider();
        IKVCenterStandard kvCenter = new NacosKVCenterDependency(provider.GetRequiredService<INacosConfigService>());
        var key = $"{GetPrefix()}-kv-{Guid.NewGuid():N}.json";
        var value = "{\"Feature\":{\"Flag\":\"nacos-value-" + Guid.NewGuid().ToString("N") + "\"}}";

        try
        {
            Assert.True(await kvCenter.AddOrUpdateAsync(key, value));

            var item = await kvCenter.GetAsync<NacosKvCenterServiceInformation>(key);
            Assert.NotNull(item);
            Assert.Equal(key, item.Key);
            Assert.Equal(value, item.Value);

            var items = await kvCenter.QueryAsync<NacosKvCenterServiceInformation>(key);
            Assert.Contains(items, current => current.Key == key && current.Value == value);
        }
        finally
        {
            await kvCenter.DeleteAsync(key);
        }

        var deleted = await kvCenter.GetAsync<NacosKvCenterServiceInformation>(key);
        Assert.Null(deleted);
    }

    /// <summary>
    /// <para>zh-cn:验证真实 Nacos 配置中心对同一 dataId 的重复发布会覆盖旧值。</para>
    /// <para>en-us:Verifies repeated publishing to the same real Nacos dataId overwrites the old value.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "Nacos")]
    public async Task Nacos_should_overwrite_existing_kv_value()
    {
        if (!IsEnabled())
        {
            return;
        }

        using var provider = BuildServiceProvider();
        IKVCenterStandard kvCenter = new NacosKVCenterDependency(provider.GetRequiredService<INacosConfigService>());
        var key = $"{GetPrefix()}-kv-overwrite-{Guid.NewGuid():N}.json";

        try
        {
            Assert.True(await kvCenter.AddOrUpdateAsync(key, "value-v1"));
            Assert.True(await kvCenter.AddOrUpdateAsync(key, "value-v2"));

            var item = await kvCenter.GetAsync<NacosKvCenterServiceInformation>(key);

            Assert.NotNull(item);
            Assert.Equal("value-v2", item.Value);
        }
        finally
        {
            await kvCenter.DeleteAsync(key);
        }
    }

    private static ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddNacosV2Config(NacosOptionsBuilder.Build(GetOptions()));
        return services.BuildServiceProvider();
    }

    private static bool IsEnabled()
    {
        return string.Equals(
            AppConfigurationLoader.InnerConfiguration["NacosIntegration:RunNacosTests"],
            "true",
            StringComparison.OrdinalIgnoreCase);
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
            ConfigGroup = AppConfigurationLoader.InnerConfiguration["NacosIntegration:ConfigGroup"] ?? "DEFAULT_GROUP"
        };
    }
}
