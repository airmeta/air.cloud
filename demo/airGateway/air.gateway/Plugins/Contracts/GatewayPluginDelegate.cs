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
namespace air.cloud.gateway.Plugins.Contracts;

/// <summary>
/// <para>zh-cn:表示网关插件链路中的下一个处理委托。插件调用该委托表示继续执行后续插件或进入后续 ASP.NET Core/Ocelot 管道；插件不调用该委托时应自行完成响应写入。</para>
/// <para>en-us:Represents the next handler in the gateway plugin chain. A plugin calls this delegate to continue to later plugins or the downstream ASP.NET Core/Ocelot pipeline; when it does not call the delegate it should complete the response itself.</para>
/// </summary>
/// <param name="context">
/// <para>zh-cn:当前插件请求上下文。</para>
/// <para>en-us:The current plugin request context.</para>
/// </param>
/// <param name="cancellationToken">
/// <para>zh-cn:请求取消令牌，通常来自 `HttpContext.RequestAborted`。</para>
/// <para>en-us:The request cancellation token, usually from `HttpContext.RequestAborted`.</para>
/// </param>
/// <returns>
/// <para>zh-cn:表示后续插件或下游管道执行过程的任务。</para>
/// <para>en-us:A task representing execution of later plugins or the downstream pipeline.</para>
/// </returns>
public delegate Task GatewayPluginDelegate(GatewayPluginContext context, CancellationToken cancellationToken);
