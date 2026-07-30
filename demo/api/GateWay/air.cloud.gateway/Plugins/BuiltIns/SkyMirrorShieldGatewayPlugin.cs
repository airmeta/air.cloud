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

using Air.Cloud.Core;
using Air.Cloud.Core.App;
using Air.Cloud.Modules.SkyMirrorShield.Middleware;

namespace air.cloud.gateway.Plugins.BuiltIns;

/// <summary>
/// <para>zh-cn:天镜盾内置插件。它在 `SkyMirrorShieldSettings:EnableSkyMirrorShield=true` 时复用原天镜盾中间件，否则直接放行请求。</para>
/// <para>en-us:Built-in SkyMirrorShield plugin. It reuses the original SkyMirrorShield middleware when `SkyMirrorShieldSettings:EnableSkyMirrorShield=true`; otherwise it passes the request through.</para>
/// </summary>
public sealed class SkyMirrorShieldGatewayPlugin : IGatewayRequestPlugin
{
    private readonly IHttpClientFactory httpClientFactory;

    /// <summary>
    /// <para>zh-cn:创建天镜盾内置插件。</para>
    /// <para>en-us:Creates the built-in SkyMirrorShield plugin.</para>
    /// </summary>
    /// <param name="httpClientFactory">
    /// <para>zh-cn:原天镜盾中间件用于访问票据校验和应用路由接口的 HTTP 客户端工厂。</para>
    /// <para>en-us:The HTTP client factory used by the original SkyMirrorShield middleware to access ticket verification and application-route endpoints.</para>
    /// </param>
    public SkyMirrorShieldGatewayPlugin(IHttpClientFactory httpClientFactory)
    {
        this.httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    }

    /// <inheritdoc/>
    public string Name => "SkyMirrorShield";

    /// <inheritdoc/>
    public int Order => 200;

    /// <inheritdoc/>
    public Task InvokeAsync(GatewayPluginContext context, GatewayPluginDelegate next, CancellationToken cancellationToken)
    {
        if (!bool.TryParse(AppConfigurationLoader.InnerConfiguration["SkyMirrorShieldSettings:EnableSkyMirrorShield"], out var enabled) || !enabled)
        {
            return next(context, cancellationToken);
        }

        var middleware = new SkyMirrorShieldMiddleware(
            _ => next(context, cancellationToken),
            httpClientFactory);
        return middleware.InvokeAsync(context.HttpContext);
    }
}
