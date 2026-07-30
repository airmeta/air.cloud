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
using air.cloud.gateway.Plugins.Options;

namespace air.cloud.gateway.Plugins.BuiltIns;

/// <summary>
/// <para>zh-cn:网关内置插件默认清单。该清单承接原 Gateway Startup 中固定挂载的业务中间件顺序，供插件运行时在未显式关闭默认插件时自动加载。</para>
/// <para>en-us:Default list of gateway built-in plugins. It preserves the business middleware order previously hard-coded in Gateway Startup and lets the plugin runtime load it automatically unless default plugins are explicitly disabled.</para>
/// </summary>
public static class GatewayBuiltInPluginDefaults
{
    /// <summary>
    /// <para>zh-cn:创建默认内置插件描述。返回清单中的 `Assembly` 为空，表示插件类型来自当前 Gateway 进程已加载程序集。</para>
    /// <para>en-us:Creates default built-in plugin descriptors. Items in the returned list leave `Assembly` empty, meaning the plugin type is resolved from assemblies already loaded in the current Gateway process.</para>
    /// </summary>
    /// <returns>
    /// <para>zh-cn:按历史中间件顺序排列的内置插件配置集合。</para>
    /// <para>en-us:The built-in plugin configuration list ordered by the historical middleware pipeline.</para>
    /// </returns>
    public static IReadOnlyList<GatewayPluginDescriptorOptions> Create()
    {
        return new[]
        {
            CreateDescriptor("GatewayTraceLog", 100, typeof(GatewayTraceLogPlugin)),
            CreateDescriptor("SkyMirrorShield", 200, typeof(SkyMirrorShieldGatewayPlugin)),
            CreateDescriptor("Signature", 300, typeof(SignatureGatewayPlugin)),
            CreateDescriptor("WhiteListRequest", 400, typeof(WhiteListRequestGatewayPlugin)),
            CreateDescriptor("RemoteIp", 500, typeof(RemoteIpGatewayPlugin))
        };
    }

    private static GatewayPluginDescriptorOptions CreateDescriptor(string name, int order, Type type)
    {
        return new GatewayPluginDescriptorOptions
        {
            Name = name,
            Enabled = true,
            Order = order,
            Type = type.FullName ?? type.Name
        };
    }
}
