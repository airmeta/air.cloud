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
using air.gateway.Configuration;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace air.gateway.Middleware;

/// <summary>
/// <para>zh-cn:在插件宿主和 Ocelot 之前保护控制面路径，并按配置限制其监听端口。</para>
/// <para>en-us:Protects control-plane paths before the plugin host and Ocelot, enforcing the configured management listener.</para>
/// </summary>
public sealed class ControlPlaneManagementMiddleware
{
    private readonly RequestDelegate next;
    private readonly IConfiguration configuration;

    /// <summary>
    /// <para>zh-cn:创建控制面管理中间件。</para>
    /// <para>en-us:Creates the control-plane management middleware.</para>
    /// </summary>
    /// <param name="next"><para>zh-cn:后续请求管道。</para><para>en-us:The downstream request pipeline.</para></param>
    /// <param name="configuration"><para>zh-cn:网关配置。</para><para>en-us:Gateway configuration.</para></param>
    public ControlPlaneManagementMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        this.next = next ?? throw new ArgumentNullException(nameof(next));
        this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    /// <summary>
    /// <para>zh-cn:拒绝未启用管理页面的控制面路径，或拒绝来自非管理端口的控制面请求。</para>
    /// <para>en-us:Rejects control-plane paths when management is disabled or when they arrive on a non-management port.</para>
    /// </summary>
    /// <param name="context"><para>zh-cn:当前 HTTP 上下文。</para><para>en-us:The current HTTP context.</para></param>
    /// <returns><para>zh-cn:表示请求处理过程的任务。</para><para>en-us:A task representing request processing.</para></returns>
    public async Task InvokeAsync(HttpContext context)
    {
        if (!ControlPlaneManagementPolicy.IsControlPlanePath(context, configuration))
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        if (!ControlPlaneManagementPolicy.IsManagementEnabled(configuration))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        var managementPort = ControlPlaneManagementPolicy.GetManagementPort(configuration);
        if (managementPort > 0
            && ControlPlaneManagementPolicy.IsRestrictedToPort(configuration)
            && context.Connection.LocalPort != managementPort)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        await next(context).ConfigureAwait(false);
    }
}
