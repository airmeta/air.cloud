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
/// <para>zh-cn:网关插件运行时配置。默认启用并加载内置插件，用于保持原 Gateway 固定中间件行为；外部 DLL 插件通过 `Plugins` 追加。</para>
/// <para>en-us:Runtime settings for gateway plugins. It is enabled by default and loads built-in plugins to preserve the previous fixed Gateway middleware behavior; external DLL plugins are appended through `Plugins`.</para>
/// </summary>
public sealed class GatewayPluginSettingsOptions
{
    /// <summary>
    /// <para>zh-cn:是否启用网关插件宿主。关闭时宿主中间件直接放行请求，不加载或执行插件；关闭后也不会执行从原固定中间件迁移来的内置插件。</para>
    /// <para>en-us:Whether the gateway plugin host is enabled. When disabled, the host middleware passes requests through directly and does not load or execute plugins; built-in plugins migrated from the previous fixed middleware pipeline are also skipped.</para>
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// <para>zh-cn:是否自动加载 Gateway 内置插件。默认开启，用于承接原 `TraceLogMiddleware`、`SkyMirrorShieldMiddleware`、`SignatureMiddleware`、`WhiteListRequestMiddleware` 和 `IPMiddleware` 的执行顺序。</para>
    /// <para>en-us:Whether Gateway built-in plugins are loaded automatically. It is enabled by default to preserve the execution order of the previous `TraceLogMiddleware`, `SkyMirrorShieldMiddleware`, `SignatureMiddleware`, `WhiteListRequestMiddleware`, and `IPMiddleware` pipeline.</para>
    /// </summary>
    public bool UseDefaultGatewayPlugins { get; set; } = true;

    /// <summary>
    /// <para>zh-cn:插件根目录。相对路径基于应用程序运行目录解析，默认值为 `plugins`。</para>
    /// <para>en-us:The plugin root directory. Relative paths are resolved from the application base directory, and the default value is `plugins`.</para>
    /// </summary>
    public string PluginDirectory { get; set; } = "plugins";

    /// <summary>
    /// <para>zh-cn:启用插件加载失败时是否阻止网关启动。默认阻止启动，避免认证类插件缺失后流量意外放行。</para>
    /// <para>en-us:Whether an enabled plugin load failure should block gateway startup. The default blocks startup to avoid accidentally allowing traffic when an authentication plugin is missing.</para>
    /// </summary>
    public bool FailOnLoadError { get; set; } = true;

    /// <summary>
    /// <para>zh-cn:插件列表。只有 `Enabled=true` 的条目会被加载。</para>
    /// <para>en-us:The plugin list. Only entries with `Enabled=true` are loaded.</para>
    /// </summary>
    public IList<GatewayPluginDescriptorOptions> Plugins { get; set; } = new List<GatewayPluginDescriptorOptions>();
}
