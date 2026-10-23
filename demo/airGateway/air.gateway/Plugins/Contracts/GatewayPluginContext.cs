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
using Microsoft.Extensions.Configuration;

namespace air.cloud.gateway.Plugins.Contracts;

/// <summary>
/// <para>zh-cn:网关请求插件的运行上下文。该上下文把当前 HTTP 请求、根服务容器、配置对象和请求级共享字典传给动态插件；插件不应缓存该对象到请求之外。</para>
/// <para>en-us:Runtime context for gateway request plugins. It exposes the current HTTP request, root service provider, configuration, and request-scoped shared items to dynamic plugins; plugins should not cache this object beyond the request.</para>
/// </summary>
public sealed class GatewayPluginContext
{
    /// <summary>
    /// <para>zh-cn:创建网关插件上下文。</para>
    /// <para>en-us:Creates a gateway plugin context.</para>
    /// </summary>
    /// <param name="httpContext">
    /// <para>zh-cn:当前 HTTP 请求上下文，不能为空。</para>
    /// <para>en-us:The current HTTP request context, which cannot be null.</para>
    /// </param>
    /// <param name="services">
    /// <para>zh-cn:网关根服务容器。运行期加载的插件可以通过它解析宿主已经注册的服务，但不能向根容器追加新服务。</para>
    /// <para>en-us:The gateway root service provider. Runtime-loaded plugins can resolve services already registered by the host through it, but cannot add new services to the root container.</para>
    /// </param>
    /// <param name="configuration">
    /// <para>zh-cn:网关当前配置对象，通常来自本地 `appsettings.json` 以及启动时叠加的配置源。</para>
    /// <para>en-us:The current gateway configuration object, usually from local `appsettings.json` plus configuration sources overlaid during startup.</para>
    /// </param>
    public GatewayPluginContext(HttpContext httpContext, IServiceProvider services, IConfiguration configuration)
    {
        HttpContext = httpContext ?? throw new ArgumentNullException(nameof(httpContext));
        Services = services ?? throw new ArgumentNullException(nameof(services));
        Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    /// <summary>
    /// <para>zh-cn:当前 HTTP 请求上下文。</para>
    /// <para>en-us:The current HTTP request context.</para>
    /// </summary>
    public HttpContext HttpContext { get; }

    /// <summary>
    /// <para>zh-cn:网关根服务容器。</para>
    /// <para>en-us:The gateway root service provider.</para>
    /// </summary>
    public IServiceProvider Services { get; }

    /// <summary>
    /// <para>zh-cn:网关配置对象。</para>
    /// <para>en-us:The gateway configuration object.</para>
    /// </summary>
    public IConfiguration Configuration { get; }

    /// <summary>
    /// <para>zh-cn:同一请求内插件之间共享的数据字典。该字典只在当前请求内有效，插件可以用它传递认证结果、租户信息或诊断标记。</para>
    /// <para>en-us:A data dictionary shared by plugins in the same request. It is valid only for the current request and can be used to pass authentication results, tenant information, or diagnostics markers.</para>
    /// </summary>
    public IDictionary<string, object?> Items { get; } = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
}
