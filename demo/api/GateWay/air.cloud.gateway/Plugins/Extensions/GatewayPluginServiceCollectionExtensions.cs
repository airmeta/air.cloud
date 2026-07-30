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
using air.cloud.gateway.Plugins.BuiltIns;
using air.cloud.gateway.Plugins.Options;
using air.cloud.gateway.Plugins.Runtime;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace air.cloud.gateway.Plugins.Extensions;

/// <summary>
/// <para>zh-cn:网关 DLL 插件服务注册扩展。</para>
/// <para>en-us:Service registration extensions for gateway DLL plugins.</para>
/// </summary>
public static class GatewayPluginServiceCollectionExtensions
{
    /// <summary>
    /// <para>zh-cn:注册网关插件运行时。该方法注册插件管理器和配置绑定，并在默认配置下追加 Gateway 内置插件；宿主仍需在 Gateway 管道中调用 `UseMiddleware&lt;GatewayPluginHostMiddleware&gt;()`。</para>
    /// <para>en-us:Registers the gateway plugin runtime. This method registers the plugin manager and configuration binding and appends Gateway built-in plugins by default; the host still needs to call `UseMiddleware&lt;GatewayPluginHostMiddleware&gt;()` in the Gateway pipeline.</para>
    /// </summary>
    /// <param name="services">
    /// <para>zh-cn:ASP.NET Core 依赖注入服务集合。</para>
    /// <para>en-us:The ASP.NET Core dependency injection service collection.</para>
    /// </param>
    /// <param name="configuration">
    /// <para>zh-cn:插件配置节，通常为 `GatewayPluginSettings`。</para>
    /// <para>en-us:The plugin configuration section, usually `GatewayPluginSettings`.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:传入的服务集合，便于链式调用。</para>
    /// <para>en-us:The same service collection for chaining.</para>
    /// </returns>
    public static IServiceCollection AddGatewayPluginRuntime(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<GatewayPluginSettingsOptions>()
            .Bind(configuration)
            .PostConfigure(options =>
            {
                if (string.IsNullOrWhiteSpace(options.PluginDirectory))
                {
                    options.PluginDirectory = "plugins";
                }

                if (options.UseDefaultGatewayPlugins)
                {
                    AppendDefaultGatewayPlugins(options);
                }
            });

        services.TryAddSingleton<GatewayPluginManager>();
        services.TryAddSingleton<IGatewayPluginManager>(provider => provider.GetRequiredService<GatewayPluginManager>());
        services.AddSingleton<IHostedService>(provider => provider.GetRequiredService<GatewayPluginManager>());
        services.AddHttpClient();
        return services;
    }

    private static void AppendDefaultGatewayPlugins(GatewayPluginSettingsOptions options)
    {
        var configuredTypes = new HashSet<string>(
            options.Plugins
                .Where(plugin => !string.IsNullOrWhiteSpace(plugin.Type))
                .Select(plugin => plugin.Type),
            StringComparer.Ordinal);

        foreach (var descriptor in GatewayBuiltInPluginDefaults.Create())
        {
            if (configuredTypes.Add(descriptor.Type))
            {
                options.Plugins.Add(descriptor);
            }
        }
    }
}
