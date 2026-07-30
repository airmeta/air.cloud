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
/// <para>zh-cn:白名单标记内置插件。它复用原 `WhiteListRequestMiddleware`，根据白名单路由向请求头写入 `WHITE_LIST_REQUEST` 标记。</para>
/// <para>en-us:Built-in whitelist marker plugin. It reuses the original `WhiteListRequestMiddleware` to write the `WHITE_LIST_REQUEST` marker header according to whitelist routes.</para>
/// </summary>
public sealed class WhiteListRequestGatewayPlugin : IGatewayRequestPlugin
{
    /// <inheritdoc/>
    public string Name => "WhiteListRequest";

    /// <inheritdoc/>
    public int Order => 400;

    /// <inheritdoc/>
    public Task InvokeAsync(GatewayPluginContext context, GatewayPluginDelegate next, CancellationToken cancellationToken)
    {
        var middleware = new WhiteListRequestMiddleware(_ => next(context, cancellationToken));
        return middleware.InvokeAsync(context.HttpContext);
    }
}
