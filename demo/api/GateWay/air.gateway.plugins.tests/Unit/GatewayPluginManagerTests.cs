using air.cloud.gateway.Plugins.Extensions;
using air.cloud.gateway.Plugins.BuiltIns;
using air.cloud.gateway.Plugins.Contracts;
using air.cloud.gateway.Plugins.Runtime;
using air.gateway.plugins.sample;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace air.gateway.plugins.tests.Unit;

/// <summary>
/// <para>zh-cn:网关 DLL 插件管理器单元测试。</para>
/// <para>en-us:Unit tests for the gateway DLL plugin manager.</para>
/// </summary>
public sealed class GatewayPluginManagerTests
{
    /// <summary>
    /// <para>zh-cn:验证插件宿主关闭时，即使配置了插件条目也不会加载 DLL。</para>
    /// <para>en-us:Verifies that when the plugin host is disabled, DLLs are not loaded even if plugin entries are configured.</para>
    /// </summary>
    [Fact]
    public async Task ReloadAsync_should_keep_empty_snapshot_when_host_is_disabled()
    {
        var sampleAssemblyPath = typeof(SampleGatewayHeaderPlugin).Assembly.Location;
        var manager = CreateManager(new Dictionary<string, string?>
        {
            ["GatewayPluginSettings:Enabled"] = "false",
            ["GatewayPluginSettings:UseDefaultGatewayPlugins"] = "false",
            ["GatewayPluginSettings:PluginDirectory"] = Path.GetDirectoryName(sampleAssemblyPath),
            ["GatewayPluginSettings:Plugins:0:Name"] = "SampleGatewayHeader",
            ["GatewayPluginSettings:Plugins:0:Enabled"] = "true",
            ["GatewayPluginSettings:Plugins:0:Assembly"] = Path.GetFileName(sampleAssemblyPath),
            ["GatewayPluginSettings:Plugins:0:Type"] = typeof(SampleGatewayHeaderPlugin).FullName
        });

        await manager.ReloadAsync();

        Assert.Empty(manager.GetSnapshot());
    }

    /// <summary>
    /// <para>zh-cn:验证启用插件条目后，管理器可以从配置的 DLL 路径加载 `IGatewayRequestPlugin` 实现并按配置顺序生成快照。</para>
    /// <para>en-us:Verifies that when a plugin entry is enabled, the manager can load an `IGatewayRequestPlugin` implementation from the configured DLL path and create an ordered snapshot.</para>
    /// </summary>
    [Fact]
    public async Task ReloadAsync_should_load_enabled_plugin_from_dll()
    {
        var sampleAssemblyPath = typeof(SampleGatewayHeaderPlugin).Assembly.Location;
        var manager = CreateManager(new Dictionary<string, string?>
        {
            ["GatewayPluginSettings:Enabled"] = "true",
            ["GatewayPluginSettings:UseDefaultGatewayPlugins"] = "false",
            ["GatewayPluginSettings:PluginDirectory"] = Path.GetDirectoryName(sampleAssemblyPath),
            ["GatewayPluginSettings:Plugins:0:Name"] = "ConfiguredSample",
            ["GatewayPluginSettings:Plugins:0:Enabled"] = "true",
            ["GatewayPluginSettings:Plugins:0:Order"] = "50",
            ["GatewayPluginSettings:Plugins:0:Assembly"] = Path.GetFileName(sampleAssemblyPath),
            ["GatewayPluginSettings:Plugins:0:Type"] = typeof(SampleGatewayHeaderPlugin).FullName
        });

        await manager.ReloadAsync();

        var registration = Assert.Single(manager.GetSnapshot());
        Assert.Equal("ConfiguredSample", registration.Name);
        Assert.Equal(50, registration.Order);
        Assert.IsAssignableFrom<IGatewayRequestPlugin>(registration.Plugin);
        Assert.Equal(typeof(SampleGatewayHeaderPlugin).FullName, registration.Plugin.GetType().FullName);
    }

    /// <summary>
    /// <para>zh-cn:验证启用插件的入口类型无效且配置为加载失败即阻断时，重新加载会抛出异常。</para>
    /// <para>en-us:Verifies that reload throws when an enabled plugin has an invalid entry type and the settings require fail-fast loading.</para>
    /// </summary>
    [Fact]
    public async Task ReloadAsync_should_throw_when_enabled_plugin_type_is_invalid_and_fail_on_load_error_is_true()
    {
        var sampleAssemblyPath = typeof(SampleGatewayHeaderPlugin).Assembly.Location;
        var manager = CreateManager(new Dictionary<string, string?>
        {
            ["GatewayPluginSettings:Enabled"] = "true",
            ["GatewayPluginSettings:UseDefaultGatewayPlugins"] = "false",
            ["GatewayPluginSettings:FailOnLoadError"] = "true",
            ["GatewayPluginSettings:PluginDirectory"] = Path.GetDirectoryName(sampleAssemblyPath),
            ["GatewayPluginSettings:Plugins:0:Name"] = "BrokenSample",
            ["GatewayPluginSettings:Plugins:0:Enabled"] = "true",
            ["GatewayPluginSettings:Plugins:0:Assembly"] = Path.GetFileName(sampleAssemblyPath),
            ["GatewayPluginSettings:Plugins:0:Type"] = "air.gateway.plugins.sample.NotExistsPlugin"
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() => manager.ReloadAsync());
    }

    /// <summary>
    /// <para>zh-cn:验证未关闭默认插件时，插件管理器会从当前 Gateway 进程加载由原固定中间件迁移来的内置插件。</para>
    /// <para>en-us:Verifies that when default plugins are not disabled, the plugin manager loads built-in plugins migrated from the previous fixed middleware pipeline from the current Gateway process.</para>
    /// </summary>
    [Fact]
    public async Task ReloadAsync_should_load_default_built_in_plugins_from_current_process()
    {
        var manager = CreateManager(new Dictionary<string, string?>
        {
            ["GatewayPluginSettings:Enabled"] = "true",
            ["GatewayPluginSettings:UseDefaultGatewayPlugins"] = "true"
        });

        await manager.ReloadAsync();

        var plugins = manager.GetSnapshot();
        Assert.Contains(plugins, plugin => plugin.Name == "GatewayTraceLog" && plugin.Plugin is GatewayTraceLogPlugin);
        Assert.Contains(plugins, plugin => plugin.Name == "SkyMirrorShield" && plugin.Plugin is SkyMirrorShieldGatewayPlugin);
        Assert.Contains(plugins, plugin => plugin.Name == "Signature" && plugin.Plugin is SignatureGatewayPlugin);
        Assert.Contains(plugins, plugin => plugin.Name == "WhiteListRequest" && plugin.Plugin is WhiteListRequestGatewayPlugin);
        Assert.Contains(plugins, plugin => plugin.Name == "RemoteIp" && plugin.Plugin is RemoteIpGatewayPlugin);
    }

    private static IGatewayPluginManager CreateManager(IDictionary<string, string?> values)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpClient();
        services.AddGatewayPluginRuntime(configuration.GetSection("GatewayPluginSettings"));
        return services.BuildServiceProvider().GetRequiredService<IGatewayPluginManager>();
    }
}
