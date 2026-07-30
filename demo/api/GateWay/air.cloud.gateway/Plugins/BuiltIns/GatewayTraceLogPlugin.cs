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
using air.cloud.gateway.Middleware;
using air.cloud.gateway.Plugins.Contracts;

namespace air.cloud.gateway.Plugins.BuiltIns;

/// <summary>
/// <para>zh-cn:网关访问日志内置插件。它复用原 `TraceLogMiddleware` 的采集逻辑，并作为插件链中的第一个默认插件包裹后续校验和 Ocelot 转发。</para>
/// <para>en-us:Built-in gateway access-log plugin. It reuses the original `TraceLogMiddleware` capture logic and acts as the first default plugin wrapping later validation and Ocelot forwarding.</para>
/// </summary>
public sealed class GatewayTraceLogPlugin : IGatewayRequestPlugin
{
    /// <inheritdoc/>
    public string Name => "GatewayTraceLog";

    /// <inheritdoc/>
    public int Order => 100;

    /// <inheritdoc/>
    public Task InvokeAsync(GatewayPluginContext context, GatewayPluginDelegate next, CancellationToken cancellationToken)
    {
        var middleware = new TraceLogMiddleware(_ => next(context, cancellationToken));
        return middleware.InvokeAsync(context.HttpContext);
    }
}
