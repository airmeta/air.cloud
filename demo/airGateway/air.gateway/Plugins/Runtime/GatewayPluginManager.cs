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
using System.Collections.Immutable;
using System.Reflection;

using air.cloud.gateway.Plugins.Contracts;
using air.cloud.gateway.Plugins.Options;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace air.cloud.gateway.Plugins.Runtime;

/// <summary>
/// <para>zh-cn:默认网关插件管理器。它在宿主启动时加载配置中启用的外部 DLL 插件，并通过不可变快照供请求插件宿主读取。</para>
/// <para>en-us:Default gateway plugin manager. It loads enabled external DLL plugins from configuration during host startup and exposes immutable snapshots to the request plugin host.</para>
/// </summary>
public sealed class GatewayPluginManager : IGatewayPluginManager, IHostedService
{
    private readonly IServiceProvider services;
    private readonly IOptionsMonitor<GatewayPluginSettingsOptions> optionsMonitor;
    private readonly ILogger<GatewayPluginManager> logger;
    private ImmutableArray<GatewayPluginRegistration> snapshot = ImmutableArray<GatewayPluginRegistration>.Empty;

    /// <summary>
    /// <para>zh-cn:创建网关插件管理器。管理器不会在构造阶段加载 DLL，而是在 `StartAsync` 或显式 `ReloadAsync` 时加载，避免服务容器尚未完成构建。</para>
    /// <para>en-us:Creates a gateway plugin manager. The manager does not load DLLs in the constructor; it loads them in `StartAsync` or explicit `ReloadAsync` so the service provider is fully built first.</para>
    /// </summary>
    /// <param name="services">
    /// <para>zh-cn:网关根服务容器。</para>
    /// <para>en-us>The gateway root service provider.</para>
    /// </param>
    /// <param name="optionsMonitor">
    /// <para>zh-cn:网关插件配置监视器。</para>
    /// <para>en-us>The gateway plugin settings monitor.</para>
    /// </param>
    /// <param name="logger">
    /// <para>zh-cn:插件加载日志记录器。</para>
    /// <para>en-us>The logger used for plugin loading.</para>
    /// </param>
    public GatewayPluginManager(
        IServiceProvider services,
        IOptionsMonitor<GatewayPluginSettingsOptions> optionsMonitor,
        ILogger<GatewayPluginManager> logger)
    {
        this.services = services ?? throw new ArgumentNullException(nameof(services));
        this.optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        return ReloadAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public IReadOnlyList<GatewayPluginRegistration> GetSnapshot()
    {
        return snapshot;
    }

    /// <inheritdoc/>
    public Task ReloadAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var options = optionsMonitor.CurrentValue;
        if (!options.Enabled)
        {
            snapshot = ImmutableArray<GatewayPluginRegistration>.Empty;
            logger.LogInformation("Gateway plugin host is disabled.");
            return Task.CompletedTask;
        }

        var pluginDirectory = ResolvePluginDirectory(options.PluginDirectory);
        var loadedPlugins = new List<GatewayPluginRegistration>();
        var errors = new List<Exception>();

        foreach (var descriptor in options.Plugins.Where(plugin => plugin.Enabled))
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                loadedPlugins.Add(LoadPlugin(pluginDirectory, descriptor));
            }
            catch (Exception ex)
            {
                errors.Add(ex);
                logger.LogError(ex, "Failed to load gateway plugin {PluginName} from {Assembly}.", descriptor.Name, descriptor.Assembly);
            }
        }

        if (errors.Count > 0 && options.FailOnLoadError)
        {
            throw new InvalidOperationException("One or more enabled gateway plugins failed to load.", new AggregateException(errors));
        }

        snapshot = loadedPlugins
            .OrderBy(plugin => plugin.Order)
            .ThenBy(plugin => plugin.Name, StringComparer.OrdinalIgnoreCase)
            .ToImmutableArray();

        logger.LogInformation("Gateway plugin host loaded {PluginCount} enabled plugin(s).", snapshot.Length);
        return Task.CompletedTask;
    }

    private GatewayPluginRegistration LoadPlugin(string pluginDirectory, GatewayPluginDescriptorOptions descriptor)
    {
        ValidateDescriptor(descriptor);

        var assemblyPath = ResolveAssemblyPath(pluginDirectory, descriptor.Assembly);
        if (!File.Exists(assemblyPath))
        {
            throw new FileNotFoundException($"Gateway plugin assembly '{assemblyPath}' does not exist.", assemblyPath);
        }

        var context = new GatewayPluginLoadContext($"air.gateway.plugin:{descriptor.Name}:{Guid.NewGuid():N}", assemblyPath);
        var assembly = context.LoadFromAssemblyPath(assemblyPath);
        PreloadReferencedAssemblies(context, assembly, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
        var pluginType = assembly.GetType(descriptor.Type, throwOnError: false, ignoreCase: false);
        if (pluginType == null)
        {
            throw new TypeLoadException($"Gateway plugin type '{descriptor.Type}' was not found in '{assemblyPath}'.");
        }

        if (!typeof(IGatewayRequestPlugin).IsAssignableFrom(pluginType))
        {
            throw new InvalidOperationException($"Gateway plugin type '{descriptor.Type}' must implement {typeof(IGatewayRequestPlugin).FullName}.");
        }

        var plugin = (IGatewayRequestPlugin)ActivatorUtilities.CreateInstance(services, pluginType);
        var order = descriptor.Order ?? plugin.Order;
        var name = string.IsNullOrWhiteSpace(descriptor.Name) ? plugin.Name : descriptor.Name;
        logger.LogInformation("Loaded gateway plugin {PluginName} from {AssemblyPath}.", name, assemblyPath);
        return new GatewayPluginRegistration(name, order, assemblyPath, descriptor.Type, plugin);
    }

    private static void PreloadReferencedAssemblies(GatewayPluginLoadContext context, Assembly assembly, ISet<string> visitedAssemblies)
    {
        foreach (var reference in assembly.GetReferencedAssemblies())
        {
            if (!visitedAssemblies.Add(reference.FullName))
            {
                continue;
            }

            var loadedAssembly = context.LoadFromAssemblyName(reference);
            PreloadReferencedAssemblies(context, loadedAssembly, visitedAssemblies);
        }
    }

    private static void ValidateDescriptor(GatewayPluginDescriptorOptions descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        if (string.IsNullOrWhiteSpace(descriptor.Assembly))
        {
            throw new InvalidOperationException("Enabled gateway plugin must configure Assembly.");
        }

        if (string.IsNullOrWhiteSpace(descriptor.Type))
        {
            throw new InvalidOperationException("Enabled gateway plugin must configure Type.");
        }
    }

    private static string ResolvePluginDirectory(string? pluginDirectory)
    {
        var directory = string.IsNullOrWhiteSpace(pluginDirectory) ? "plugins" : pluginDirectory;
        return Path.GetFullPath(Path.IsPathRooted(directory)
            ? directory
            : Path.Combine(AppContext.BaseDirectory, directory));
    }

    private static string ResolveAssemblyPath(string pluginDirectory, string assembly)
    {
        return Path.GetFullPath(Path.IsPathRooted(assembly)
            ? assembly
            : Path.Combine(pluginDirectory, assembly));
    }
}
