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
using air.cloud.gateway.Plugins.Contracts;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using air.cloud.gateway.Plugins.Options;

namespace air.cloud.gateway.Plugins.Runtime;

/// <summary>
/// <para>zh-cn:网关插件宿主中间件。该中间件固定挂载到 Gateway 管道中，并在运行期根据插件管理器快照执行已启用的 DLL 插件。</para>
/// <para>en-us:Gateway plugin host middleware. It is mounted into the Gateway pipeline once and executes enabled DLL plugins at runtime based on the plugin manager snapshot.</para>
/// </summary>
public sealed class GatewayPluginHostMiddleware
{
    private readonly RequestDelegate next;
    private readonly IGatewayPluginManager pluginManager;
    private readonly IServiceProvider services;
    private readonly IConfiguration configuration;
    private readonly IOptionsMonitor<GatewayPluginSettingsOptions> optionsMonitor;
    private readonly ILogger<GatewayPluginHostMiddleware> logger;

    /// <summary>
    /// <para>zh-cn:创建网关插件宿主中间件。构造阶段不会执行插件，实际插件链在每个请求开始时从当前快照构建。</para>
    /// <para>en-us:Creates the gateway plugin host middleware. Plugins are not executed during construction; the plugin chain is built from the current snapshot at the beginning of each request.</para>
    /// </summary>
    /// <param name="next">
    /// <para>zh-cn:后续 ASP.NET Core/Ocelot 管道。</para>
    /// <para>en-us>The downstream ASP.NET Core/Ocelot pipeline.</para>
    /// </param>
    /// <param name="pluginManager">
    /// <para>zh-cn:网关插件管理器。</para>
    /// <para>en-us>The gateway plugin manager.</para>
    /// </param>
    /// <param name="services">
    /// <para>zh-cn:网关根服务容器。</para>
    /// <para>en-us>The gateway root service provider.</para>
    /// </param>
    /// <param name="configuration">
    /// <para>zh-cn:网关配置对象。</para>
    /// <para>en-us>The gateway configuration object.</para>
    /// </param>
    /// <param name="optionsMonitor">
    /// <para>zh-cn:网关插件配置监视器。</para>
    /// <para>en-us>The gateway plugin settings monitor.</para>
    /// </param>
    /// <param name="logger">
    /// <para>zh-cn:插件宿主日志记录器。</para>
    /// <para>en-us>The logger used by the plugin host.</para>
    /// </param>
    public GatewayPluginHostMiddleware(
        RequestDelegate next,
        IGatewayPluginManager pluginManager,
        IServiceProvider services,
        IConfiguration configuration,
        IOptionsMonitor<GatewayPluginSettingsOptions> optionsMonitor,
        ILogger<GatewayPluginHostMiddleware> logger)
    {
        this.next = next ?? throw new ArgumentNullException(nameof(next));
        this.pluginManager = pluginManager ?? throw new ArgumentNullException(nameof(pluginManager));
        this.services = services ?? throw new ArgumentNullException(nameof(services));
        this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        this.optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// <para>zh-cn:执行插件宿主。插件功能关闭或当前快照为空时直接放行；否则按顺序执行插件链，插件可以调用 `next` 放行或直接写响应短路请求。</para>
    /// <para>en-us:Executes the plugin host. When plugins are disabled or the snapshot is empty it passes through directly; otherwise it runs the ordered plugin chain, where plugins can call `next` to continue or write a response to short-circuit the request.</para>
    /// </summary>
    /// <param name="httpContext">
    /// <para>zh-cn:当前 HTTP 请求上下文。</para>
    /// <para>en-us>The current HTTP request context.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:表示插件链和下游管道执行过程的任务。</para>
    /// <para>en-us>A task representing execution of the plugin chain and downstream pipeline.</para>
    /// </returns>
    public Task InvokeAsync(HttpContext httpContext)
    {
        if (!optionsMonitor.CurrentValue.Enabled)
        {
            return next(httpContext);
        }

        var plugins = pluginManager.GetSnapshot();
        if (plugins.Count == 0)
        {
            return next(httpContext);
        }

        var pluginContext = new GatewayPluginContext(httpContext, services, configuration);
        GatewayPluginDelegate pipeline = (context, _) => next(context.HttpContext);

        for (var i = plugins.Count - 1; i >= 0; i--)
        {
            var registration = plugins[i];
            var currentNext = pipeline;
            pipeline = async (context, cancellationToken) =>
            {
                try
                {
                    await registration.Plugin.InvokeAsync(context, currentNext, cancellationToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Gateway plugin {PluginName} failed while processing {Method} {Path}.", registration.Name, context.HttpContext.Request.Method, context.HttpContext.Request.Path);
                    throw;
                }
            };
        }

        return pipeline(pluginContext, httpContext.RequestAborted);
    }
}
