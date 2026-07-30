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
using Air.Cloud.Core.Plugins;

namespace air.cloud.gateway.Plugins.Contracts;

/// <summary>
/// <para>zh-cn:网关请求插件契约。运行期 DLL 插件需要实现该接口，并通过 `GatewayPluginSettings` 中的程序集和类型配置挂载到网关请求链路。</para>
/// <para>en-us:Contract for gateway request plugins. Runtime DLL plugins implement this interface and are mounted into the gateway request chain through the assembly and type settings in `GatewayPluginSettings`.</para>
/// </summary>
public interface IGatewayRequestPlugin : IPlugin
{
    /// <summary>
    /// <para>zh-cn:插件名称，用于日志、诊断和配置识别。配置项中的名称优先作为运行期标识，插件名称用于兜底。</para>
    /// <para>en-us:The plugin name used for logging, diagnostics, and configuration identification. The configured name is preferred as the runtime identity, and this value is used as a fallback.</para>
    /// </summary>
    string Name { get; }

    /// <summary>
    /// <para>zh-cn:插件默认顺序。配置项中的 `Order` 优先于该值；数值越小越早执行。</para>
    /// <para>en-us:The default plugin order. The configured `Order` takes precedence over this value; lower values run earlier.</para>
    /// </summary>
    int Order { get; }

    /// <summary>
    /// <para>zh-cn:执行插件逻辑。插件需要放行请求时调用 `next`；需要拦截请求时直接写入 `HttpContext.Response` 并返回。</para>
    /// <para>en-us:Executes the plugin logic. A plugin calls `next` to allow the request to continue; to block the request it writes to `HttpContext.Response` and returns directly.</para>
    /// </summary>
    /// <param name="context">
    /// <para>zh-cn:当前网关插件上下文。</para>
    /// <para>en-us:The current gateway plugin context.</para>
    /// </param>
    /// <param name="next">
    /// <para>zh-cn:后续插件或下游管道委托。</para>
    /// <para>en-us:The delegate for later plugins or the downstream pipeline.</para>
    /// </param>
    /// <param name="cancellationToken">
    /// <para>zh-cn:请求取消令牌。</para>
    /// <para>en-us:The request cancellation token.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:表示插件处理过程的任务。</para>
    /// <para>en-us:A task representing the plugin execution.</para>
    /// </returns>
    Task InvokeAsync(GatewayPluginContext context, GatewayPluginDelegate next, CancellationToken cancellationToken);
}
