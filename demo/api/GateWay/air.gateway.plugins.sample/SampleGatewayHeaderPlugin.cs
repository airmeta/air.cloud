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

namespace air.gateway.plugins.sample;

/// <summary>
/// <para>zh-cn:示例网关插件。该插件在响应中追加 `X-Air-Gateway-Plugin` 响应头，用于验证 DLL 插件已被 Gateway 动态加载并执行。</para>
/// <para>en-us:Sample gateway plugin. It appends the `X-Air-Gateway-Plugin` response header to verify that the DLL plugin has been dynamically loaded and executed by Gateway.</para>
/// </summary>
public sealed class SampleGatewayHeaderPlugin : IGatewayRequestPlugin
{
    /// <inheritdoc/>
    public string Name => "SampleGatewayHeader";

    /// <inheritdoc/>
    public int Order => 1000;

    /// <inheritdoc/>
    public async Task InvokeAsync(GatewayPluginContext context, GatewayPluginDelegate next, CancellationToken cancellationToken)
    {
        context.HttpContext.Response.OnStarting(() =>
        {
            context.HttpContext.Response.Headers["X-Air-Gateway-Plugin"] = Name;
            return Task.CompletedTask;
        });

        await next(context, cancellationToken);
    }
}
