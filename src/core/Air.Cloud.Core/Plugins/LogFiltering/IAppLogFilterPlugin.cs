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
using Air.Cloud.Core.Standard;
using Air.Cloud.Core.Standard.DynamicServer;

namespace Air.Cloud.Core.Plugins.LogFiltering;

/// <summary>
/// <para>zh-cn:Air.Cloud 统一日志过滤插件，允许模块或业务在日志输出前动态登记忽略路径并决定是否跳过输出。</para>
/// <para>en-us:Air.Cloud unified log filtering plugin that lets modules or applications dynamically register ignored paths and decide whether output should be skipped.</para>
/// </summary>
public interface IAppLogFilterPlugin : IPlugin, ISingleton
{
    /// <summary>
    /// <para>zh-cn:读取当前已登记的忽略路径快照。</para>
    /// <para>en-us:Reads a snapshot of the currently registered ignored paths.</para>
    /// </summary>
    IReadOnlyCollection<string> IgnorePaths { get; }

    /// <summary>
    /// <para>zh-cn:登记一个需要忽略的请求路径；空值会被忽略，路径会被规范化为以斜杠开头且不包含查询字符串。</para>
    /// <para>en-us:Registers a request path to ignore; empty values are skipped and the path is normalized to start with a slash and exclude the query string.</para>
    /// </summary>
    /// <param name="path">
    /// <para>zh-cn:请求路径，可以是不带斜杠的相对路径、带查询字符串的路径或完整 URL。</para>
    /// <para>en-us:The request path, which can be a relative path without slash, a path with query string, or a full URL.</para>
    /// </param>
    void AddIgnorePath(string path);

    /// <summary>
    /// <para>zh-cn:根据日志上下文判断是否跳过 Air.Cloud 统一输出；实现应默认保留 Warning、Error 和 Critical。</para>
    /// <para>en-us:Determines whether Air.Cloud unified output should be skipped for the log context; implementations should keep Warning, Error, and Critical by default.</para>
    /// </summary>
    /// <param name="context">
    /// <para>zh-cn:日志过滤上下文。</para>
    /// <para>en-us:The log filtering context.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:返回 true 表示跳过输出；返回 false 表示继续输出。</para>
    /// <para>en-us:Returns true to skip output; false to continue output.</para>
    /// </returns>
    bool ShouldIgnore(AppLogFilterContext context);
}
