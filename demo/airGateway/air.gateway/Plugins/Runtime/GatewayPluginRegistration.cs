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
using air.cloud.gateway.Plugins.Contracts;

namespace air.cloud.gateway.Plugins.Runtime;

/// <summary>
/// <para>zh-cn:运行期已加载网关插件的只读注册信息。该对象会进入请求插件快照，插件管理器通过原子替换快照避免请求执行期间集合被修改。</para>
/// <para>en-us:Read-only registration information for a loaded gateway plugin. It is placed into request plugin snapshots, and the plugin manager atomically replaces snapshots so the collection is not modified while requests are executing.</para>
/// </summary>
public sealed class GatewayPluginRegistration
{
    /// <summary>
    /// <para>zh-cn:创建已加载插件注册信息。</para>
    /// <para>en-us:Creates a loaded plugin registration.</para>
    /// </summary>
    /// <param name="name">
    /// <para>zh-cn:插件运行期名称。</para>
    /// <para>en-us:The runtime plugin name.</para>
    /// </param>
    /// <param name="order">
    /// <para>zh-cn:插件执行顺序。</para>
    /// <para>en-us:The plugin execution order.</para>
    /// </param>
    /// <param name="assemblyPath">
    /// <para>zh-cn:插件主程序集绝对路径。</para>
    /// <para>en-us:The absolute path of the plugin main assembly.</para>
    /// </param>
    /// <param name="typeName">
    /// <para>zh-cn:插件入口类型全名。</para>
    /// <para>en-us:The full name of the plugin entry type.</para>
    /// </param>
    /// <param name="plugin">
    /// <para>zh-cn:插件实例。</para>
    /// <para>en-us:The plugin instance.</para>
    /// </param>
    public GatewayPluginRegistration(string name, int order, string assemblyPath, string typeName, IGatewayRequestPlugin plugin)
    {
        Name = string.IsNullOrWhiteSpace(name) ? plugin.Name : name;
        Order = order;
        AssemblyPath = assemblyPath;
        TypeName = typeName;
        Plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));
    }

    /// <summary>
    /// <para>zh-cn:插件运行期名称。</para>
    /// <para>en-us:The runtime plugin name.</para>
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// <para>zh-cn:插件执行顺序。</para>
    /// <para>en-us:The plugin execution order.</para>
    /// </summary>
    public int Order { get; }

    /// <summary>
    /// <para>zh-cn:插件主程序集绝对路径。</para>
    /// <para>en-us:The absolute path of the plugin main assembly.</para>
    /// </summary>
    public string AssemblyPath { get; }

    /// <summary>
    /// <para>zh-cn:插件入口类型全名。</para>
    /// <para>en-us:The full name of the plugin entry type.</para>
    /// </summary>
    public string TypeName { get; }

    /// <summary>
    /// <para>zh-cn:插件实例。</para>
    /// <para>en-us:The plugin instance.</para>
    /// </summary>
    public IGatewayRequestPlugin Plugin { get; }
}
