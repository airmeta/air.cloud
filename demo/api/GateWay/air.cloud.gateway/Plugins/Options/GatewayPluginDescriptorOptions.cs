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
namespace air.cloud.gateway.Plugins.Options;

/// <summary>
/// <para>zh-cn:单个网关插件的配置描述。`Assembly` 为空时从当前 Gateway 进程已加载程序集解析内置插件；`Assembly` 有值时从外部 DLL 加载插件。该描述只控制运行期加载和执行顺序，不会向 ASP.NET Core 根服务容器追加服务。</para>
/// <para>en-us:Configuration descriptor for one gateway plugin. When `Assembly` is empty the built-in plugin type is resolved from assemblies already loaded in the current Gateway process; when `Assembly` is set the plugin is loaded from an external DLL. This descriptor only controls runtime loading and execution order, and does not add services to the ASP.NET Core root container.</para>
/// </summary>
public sealed class GatewayPluginDescriptorOptions
{
    /// <summary>
    /// <para>zh-cn:插件配置名称，用于日志和诊断输出。</para>
    /// <para>en-us:The configured plugin name used in logs and diagnostics.</para>
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:是否启用该插件。关闭时不会加载插件，也不会执行插件。</para>
    /// <para>en-us:Whether this plugin is enabled. When disabled, the plugin is not loaded or executed.</para>
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// <para>zh-cn:插件执行顺序。数值越小越早执行；未显式配置时使用插件自身的默认顺序。</para>
    /// <para>en-us:The plugin execution order. Lower values run earlier; when not explicitly configured the plugin's own default order is used.</para>
    /// </summary>
    public int? Order { get; set; }

    /// <summary>
    /// <para>zh-cn:插件程序集路径。外部 DLL 插件填写该值，相对路径会基于 `GatewayPluginSettings:PluginDirectory` 解析；内置插件保持为空。</para>
    /// <para>en-us:The plugin assembly path. External DLL plugins set this value, and relative paths are resolved from `GatewayPluginSettings:PluginDirectory`; built-in plugins leave it empty.</para>
    /// </summary>
    public string Assembly { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:插件入口类型全名，该类型必须实现 `IGatewayRequestPlugin`。</para>
    /// <para>en-us:The full name of the plugin entry type, which must implement `IGatewayRequestPlugin`.</para>
    /// </summary>
    public string Type { get; set; } = string.Empty;
}
