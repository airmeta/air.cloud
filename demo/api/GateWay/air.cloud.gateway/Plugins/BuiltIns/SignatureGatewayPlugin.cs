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
/// <para>zh-cn:请求签名校验内置插件。它复用原 `SignatureMiddleware` 的签名、时间戳、Nonce、应用存在性和重复签名校验逻辑。</para>
/// <para>en-us:Built-in request signature validation plugin. It reuses the original `SignatureMiddleware` logic for signature, timestamp, nonce, application existence, and replay checks.</para>
/// </summary>
public sealed class SignatureGatewayPlugin : IGatewayRequestPlugin
{
    private readonly IHttpClientFactory httpClientFactory;

    /// <summary>
    /// <para>zh-cn:创建请求签名校验内置插件。</para>
    /// <para>en-us:Creates the built-in request signature validation plugin.</para>
    /// </summary>
    /// <param name="httpClientFactory">
    /// <para>zh-cn:原签名中间件查询应用信息时使用的 HTTP 客户端工厂。</para>
    /// <para>en-us:The HTTP client factory used by the original signature middleware when querying application information.</para>
    /// </param>
    public SignatureGatewayPlugin(IHttpClientFactory httpClientFactory)
    {
        this.httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    }

    /// <inheritdoc/>
    public string Name => "Signature";

    /// <inheritdoc/>
    public int Order => 300;

    /// <inheritdoc/>
    public Task InvokeAsync(GatewayPluginContext context, GatewayPluginDelegate next, CancellationToken cancellationToken)
    {
        var middleware = new SignatureMiddleware(
            _ => next(context, cancellationToken),
            httpClientFactory);
        return middleware.InvokeAsync(context.HttpContext);
    }
}
