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
using Microsoft.AspNetCore.Mvc.Controllers;

using System.Text;

namespace Air.Cloud.WebApp.Extensions;

/// <summary>
/// Http 鎷撳睍绫?
/// </summary>
[IgnoreScanning]
public static class HttpContextExtensions
{
    /// <summary>
    /// 鑾峰彇璇锋眰璺敱绔偣鐨勭壒鎬т俊鎭?
    /// </summary>
    /// <typeparam name="TAttribute">鐗规€х被鍨?/typeparam>
    /// <param name="httpContext">璇锋眰涓婁笅鏂?/param>
    /// <returns></returns>
    public static TAttribute GetMetadata<TAttribute>(this HttpContext httpContext)
        where TAttribute : class
    {
        return httpContext.GetEndpoint()?.Metadata?.GetMetadata<TAttribute>();
    }

    /// <summary>
    /// 鑾峰彇 鎺у埗鍣?Action 鎻忚堪鍣?
    /// </summary>
    /// <param name="httpContext"></param>
    /// <returns></returns>
    public static ControllerActionDescriptor GetControllerActionDescriptor(this HttpContext httpContext)
    {
        return httpContext.GetEndpoint()?.Metadata?.FirstOrDefault(u => u is ControllerActionDescriptor) as ControllerActionDescriptor;
    }

    /// <summary>
    /// 鑾峰彇鏈満 IPv4鍦板潃
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public static string GetLocalIpAddressToIPv4(this HttpContext context)
    {
        return context.Connection.LocalIpAddress?.MapToIPv4()?.ToString();
    }

    /// <summary>
    /// 鑾峰彇鏈満 IPv6鍦板潃
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public static string GetLocalIpAddressToIPv6(this HttpContext context)
    {
        return context.Connection.LocalIpAddress?.MapToIPv6()?.ToString();
    }

    /// <summary>
    /// 鑾峰彇杩滅▼ IPv4鍦板潃
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public static string GetRemoteIpAddressToIPv4(this HttpContext context)
    {
        return context.Connection.RemoteIpAddress?.MapToIPv4()?.ToString();
    }

    /// <summary>
    /// 鑾峰彇杩滅▼ IPv6鍦板潃
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public static string GetRemoteIpAddressToIPv6(this HttpContext context)
    {
        return context.Connection.RemoteIpAddress?.MapToIPv6()?.ToString();
    }

    /// <summary>
    /// 鑾峰彇瀹屾暣璇锋眰鍦板潃
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public static string GetRequestUrlAddress(this HttpRequest request)
    {
        return new StringBuilder()
                .Append(request.Scheme)
                .Append("://")
                .Append(request.Host)
                .Append(request.PathBase)
                .Append(request.Path)
                .Append(request.QueryString)
                .ToString();
    }

    /// <summary>
    /// 鑾峰彇鏉ユ簮鍦板潃
    /// </summary>
    /// <param name="request"></param>
    /// <param name="refererHeaderKey"></param>
    /// <returns></returns>
    public static string GetRefererUrlAddress(this HttpRequest request, string refererHeaderKey = "Referer")
    {
        return request.Headers[refererHeaderKey].ToString();
    }
}