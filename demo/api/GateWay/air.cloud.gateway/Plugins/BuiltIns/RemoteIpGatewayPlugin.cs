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
/// <para>zh-cn:远端 IP 标记内置插件。它复用原 `IPMiddleware`，在请求进入 Ocelot 前保留原有远端地址头处理逻辑。</para>
/// <para>en-us:Built-in remote-IP marker plugin. It reuses the original `IPMiddleware` and preserves the existing remote-address header handling before requests enter Ocelot.</para>
/// </summary>
public sealed class RemoteIpGatewayPlugin : IGatewayRequestPlugin
{
    /// <inheritdoc/>
    public string Name => "RemoteIp";

    /// <inheritdoc/>
    public int Order => 500;

    /// <inheritdoc/>
    public Task InvokeAsync(GatewayPluginContext context, GatewayPluginDelegate next, CancellationToken cancellationToken)
    {
        var middleware = new IPMiddleware(_ => next(context, cancellationToken));
        return middleware.InvokeAsync(context.HttpContext);
    }
}
