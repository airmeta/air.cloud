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
using Microsoft.AspNetCore.Http;

namespace air.gateway.Middleware;

/// <summary>
/// <para>zh-cn:恢复 Ocelot 下游请求中被遗漏的请求体和内容头。WebApp 会把入口请求包装为可回读流；Ocelot 17 在该流尚未填充缓存时可能按长度 0 将 POST/PUT/PATCH 内容映射为空。本处理器在实际发送下游请求前从原始 HTTP 上下文恢复内容，避免内部 JSON 调用变成无媒体类型的空请求。</para>
/// <para>en-us:Restores request content and content headers omitted from an Ocelot downstream request. WebApp wraps incoming requests in rewindable streams; Ocelot 17 can map POST/PUT/PATCH content as empty when that stream has not populated its buffer yet. Before sending downstream, this handler restores content from the original HTTP context so internal JSON calls do not become media-type-less empty requests.</para>
/// </summary>
public sealed class OcelotRequestContentRestorationDelegatingHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor httpContextAccessor;

    /// <summary>
    /// <para>zh-cn:创建 Ocelot 请求内容恢复处理器。</para>
    /// <para>en-us:Creates the Ocelot request-content restoration handler.</para>
    /// </summary>
    /// <param name="httpContextAccessor">
    /// <para>zh-cn:用于获取当前入口请求及其可回读 body 的 HTTP 上下文访问器。</para>
    /// <para>en-us:HTTP context accessor used to obtain the current incoming request and its rewindable body.</para>
    /// </param>
    public OcelotRequestContentRestorationDelegatingHandler(IHttpContextAccessor httpContextAccessor)
    {
        this.httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    /// <summary>
    /// <para>zh-cn:在 Ocelot 发送下游请求前恢复缺失或长度不匹配的内容。无 body 请求、已正确映射的内容以及无法确认有请求体的请求保持原样。</para>
    /// <para>en-us:Restores missing or length-mismatched content before Ocelot sends a downstream request. Requests without a body, correctly mapped content, and requests whose body presence cannot be established are left unchanged.</para>
    /// </summary>
    /// <param name="request">
    /// <para>zh-cn:Ocelot 即将发送的下游 HTTP 请求。</para>
    /// <para>en-us:Downstream HTTP request that Ocelot is about to send.</para>
    /// </param>
    /// <param name="cancellationToken">
    /// <para>zh-cn:下游请求取消令牌。</para>
    /// <para>en-us:Cancellation token for the downstream request.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:下游响应。</para>
    /// <para>en-us:The downstream response.</para>
    /// </returns>
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var httpRequest = httpContextAccessor.HttpContext?.Request;
        if (httpRequest is not null && ShouldRestoreContent(request, httpRequest))
        {
            if (httpRequest.Body.CanSeek)
            {
                httpRequest.Body.Position = 0;
            }

            request.Content?.Dispose();
            request.Content = new StreamContent(httpRequest.Body);
            if (httpRequest.ContentLength.HasValue)
            {
                request.Content.Headers.ContentLength = httpRequest.ContentLength.Value;
            }

            CopyContentHeaders(httpRequest, request);
        }

        return base.SendAsync(request, cancellationToken);
    }

    private static bool ShouldRestoreContent(HttpRequestMessage request, HttpRequest httpRequest)
    {
        if (!RequestCanHaveBody(httpRequest) || !HasRequestBody(httpRequest))
        {
            return false;
        }

        if (request.Content is null)
        {
            return true;
        }

        if (httpRequest.ContentLength is > 0 && request.Content.Headers.ContentLength != httpRequest.ContentLength)
        {
            return true;
        }

        return !string.IsNullOrWhiteSpace(httpRequest.ContentType) && request.Content.Headers.ContentType is null;
    }

    private static bool HasRequestBody(HttpRequest request)
    {
        return request.ContentLength is > 0
            || (!string.IsNullOrWhiteSpace(request.ContentType) && RequestCanHaveBody(request))
            || request.Headers.TransferEncoding.Any(value => string.Equals(value, "chunked", StringComparison.OrdinalIgnoreCase));
    }

    private static bool RequestCanHaveBody(HttpRequest request)
    {
        return !HttpMethods.IsGet(request.Method)
            && !HttpMethods.IsHead(request.Method)
            && !HttpMethods.IsTrace(request.Method);
    }

    private static void CopyContentHeaders(HttpRequest httpRequest, HttpRequestMessage request)
    {
        foreach (var headerName in new[]
        {
            "Content-Type", "Content-Encoding", "Content-Language", "Content-Location", "Content-MD5", "Content-Range", "Content-Disposition"
        })
        {
            if (!httpRequest.Headers.TryGetValue(headerName, out var values) || values.Count == 0)
            {
                continue;
            }

            request.Content!.Headers.Remove(headerName);
            request.Content.Headers.TryAddWithoutValidation(headerName, values.AsEnumerable());
        }
    }
}
