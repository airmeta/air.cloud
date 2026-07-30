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
namespace air.cloud.gateway.Plugins.Runtime;

/// <summary>
/// <para>zh-cn:网关插件管理器。它负责根据配置加载 DLL 插件，并向请求宿主中间件提供当前插件快照。</para>
/// <para>en-us:Gateway plugin manager. It loads DLL plugins from configuration and provides the current plugin snapshot to the request host middleware.</para>
/// </summary>
public interface IGatewayPluginManager
{
    /// <summary>
    /// <para>zh-cn:获取当前插件快照。返回集合不会在请求执行期间被修改。</para>
    /// <para>en-us:Gets the current plugin snapshot. The returned collection is not modified while a request is executing.</para>
    /// </summary>
    /// <returns>
    /// <para>zh-cn:已按执行顺序排序的插件注册集合。</para>
    /// <para>en-us:The plugin registrations sorted by execution order.</para>
    /// </returns>
    IReadOnlyList<GatewayPluginRegistration> GetSnapshot();

    /// <summary>
    /// <para>zh-cn:根据当前配置重新加载插件。加载成功后会原子替换请求快照；加载失败且配置要求失败即阻断时会抛出异常。</para>
    /// <para>en-us:Reloads plugins from the current configuration. A successful load atomically replaces the request snapshot; when loading fails and the settings require fail-fast behavior, an exception is thrown.</para>
    /// </summary>
    /// <param name="cancellationToken">
    /// <para>zh-cn:取消令牌。</para>
    /// <para>en-us>The cancellation token.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:表示重新加载过程的任务。</para>
    /// <para>en-us>A task representing the reload operation.</para>
    /// </returns>
    Task ReloadAsync(CancellationToken cancellationToken = default);
}
