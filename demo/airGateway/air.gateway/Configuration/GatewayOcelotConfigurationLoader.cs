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
using air.gateway.Options;

using Air.Cloud.Modules.Consul.Model;
using Air.Cloud.Modules.Consul.Util;

namespace air.gateway.Configuration;

/// <summary>
/// <para>zh-cn:Gateway 的 Ocelot 配置加载器。该加载器把配置来源选择集中到一个位置，避免启动类直接绑定到 Consul。</para>
/// <para>en-us:Ocelot configuration loader for Gateway. It centralizes configuration-source selection so the startup class is not directly bound to Consul.</para>
/// </summary>
public static class GatewayOcelotConfigurationLoader
{
    private const string OcelotConfigurationSourceKey = "GatewaySettings:OcelotConfigurationSource";

    /// <summary>
    /// <para>zh-cn:根据 `GatewaySettings:OcelotConfigurationSource` 构建 Ocelot 配置。默认值为 `Consul`；`File` 只使用本地配置；`ConsulThenFile` 会先加载 Consul 再叠加本地配置。</para>
    /// <para>en-us:Builds the Ocelot configuration according to `GatewaySettings:OcelotConfigurationSource`. The default is `Consul`; `File` uses local configuration only; `ConsulThenFile` loads Consul first and overlays local configuration afterwards.</para>
    /// </summary>
    /// <param name="localConfiguration">
    /// <para>zh-cn:应用启动时已经加载的本地配置，通常来自 `appsettings.json`、环境变量和命令行参数。</para>
    /// <para>en-us:The local configuration already loaded during application startup, usually from `appsettings.json`, environment variables, and command-line arguments.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:可直接传入 `services.AddOcelot(...)` 的配置管理器。</para>
    /// <para>en-us:A configuration manager that can be passed directly to `services.AddOcelot(...)`.</para>
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <para>zh-cn:当本地配置为空时抛出。</para>
    /// <para>en-us:Thrown when the local configuration is null.</para>
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// <para>zh-cn:当配置源值无法识别，或需要 Consul 但 `ConsulServiceOptions` 缺少必要配置时抛出。</para>
    /// <para>en-us:Thrown when the configuration source value is unknown, or when Consul is required but `ConsulServiceOptions` misses required values.</para>
    /// </exception>
    public static ConfigurationManager Load(IConfiguration localConfiguration)
    {
        ArgumentNullException.ThrowIfNull(localConfiguration);

        var source = ResolveSource(localConfiguration);
        var configurationManager = new ConfigurationManager();

        if (source is GatewayOcelotConfigurationSource.Consul or GatewayOcelotConfigurationSource.ConsulThenFile)
        {
            AddConsulConfiguration(configurationManager, localConfiguration);
        }

        if (source is GatewayOcelotConfigurationSource.File or GatewayOcelotConfigurationSource.ConsulThenFile)
        {
            configurationManager.AddConfiguration(localConfiguration);
        }

        return configurationManager;
    }

    /// <summary>
    /// <para>zh-cn:解析 Gateway Ocelot 配置源。空值按 `Consul` 处理，非法值会给出包含原始配置值的确定性异常。</para>
    /// <para>en-us:Resolves the Gateway Ocelot configuration source. Empty values are treated as `Consul`, while invalid values produce deterministic exceptions containing the original configured value.</para>
    /// </summary>
    /// <param name="configuration">
    /// <para>zh-cn:用于读取 `GatewaySettings` 的配置对象。</para>
    /// <para>en-us:The configuration object used to read `GatewaySettings`.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:解析后的配置源枚举值。</para>
    /// <para>en-us:The resolved configuration-source enum value.</para>
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <para>zh-cn:当配置对象为空时抛出。</para>
    /// <para>en-us:Thrown when the configuration object is null.</para>
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// <para>zh-cn:当配置源值不是 `Consul`、`File` 或 `ConsulThenFile` 时抛出。</para>
    /// <para>en-us:Thrown when the configured source is not `Consul`, `File`, or `ConsulThenFile`.</para>
    /// </exception>
    public static GatewayOcelotConfigurationSource ResolveSource(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var configuredSource = configuration[OcelotConfigurationSourceKey];
        if (string.IsNullOrWhiteSpace(configuredSource))
        {
            return GatewayOcelotConfigurationSource.Consul;
        }

        if (Enum.TryParse<GatewayOcelotConfigurationSource>(configuredSource, ignoreCase: true, out var source))
        {
            return source;
        }

        throw new InvalidOperationException($"Unsupported {OcelotConfigurationSourceKey} value '{configuredSource}'. Supported values: Consul, File, ConsulThenFile.");
    }

    private static void AddConsulConfiguration(ConfigurationManager configurationManager, IConfiguration localConfiguration)
    {
        var options = new ConsulServiceOptions();
        localConfiguration.GetSection("ConsulServiceOptions").Bind(options);
        if (string.IsNullOrWhiteSpace(options.ConsulAddress) || string.IsNullOrWhiteSpace(options.ConfigurationServiceName))
        {
            throw new InvalidOperationException("Gateway Ocelot configuration source requires Consul, but ConsulServiceOptions:ConsulAddress or ConsulServiceOptions:ConfigurationServiceName is missing.");
        }

        var remoteConfiguration = ConfigurationLoader.LoadRemoteConfiguration(options);
        configurationManager.AddConfiguration(remoteConfiguration.Item1);
        if (remoteConfiguration.Item2 != null)
        {
            configurationManager.AddConfiguration(remoteConfiguration.Item2);
        }
    }
}
