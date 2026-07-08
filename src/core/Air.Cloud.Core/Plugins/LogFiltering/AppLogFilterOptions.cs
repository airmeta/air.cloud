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
using Microsoft.Extensions.Logging;

namespace Air.Cloud.Core.Plugins.LogFiltering;

/// <summary>
/// <para>zh-cn:Air.Cloud 日志过滤配置，用于控制健康检查等框架噪音日志的忽略路径、分类范围和级别上限。</para>
/// <para>en-us:Air.Cloud log filtering options used to control ignored paths, category scope, and maximum level for framework-noise logs such as health checks.</para>
/// </summary>
public sealed class AppLogFilterOptions
{
    /// <summary>
    /// <para>zh-cn:允许被忽略的最高日志级别；高于该级别的日志会保留，默认只忽略 Information 及以下。</para>
    /// <para>en-us:The highest log level that can be ignored; logs above this level are kept, defaulting to Information and below.</para>
    /// </summary>
    public LogLevel MaximumIgnoredLevel { get; set; } = LogLevel.Information;

    /// <summary>
    /// <para>zh-cn:需要忽略的请求路径集合，支持配置中心或模块启动时动态写入。</para>
    /// <para>en-us:The request paths to ignore, supporting dynamic writes from config centers or module startup.</para>
    /// </summary>
    public IList<string> IgnorePaths { get; set; } = new List<string>();

    /// <summary>
    /// <para>zh-cn:允许路径忽略规则生效的日志分类前缀；为空时表示不限制分类。</para>
    /// <para>en-us:The category prefixes where path ignore rules can apply; an empty list means no category restriction.</para>
    /// </summary>
    public IList<string> CategoryPrefixes { get; set; } = new List<string>
    {
        "Microsoft.AspNetCore.Hosting.Diagnostics",
        "Microsoft.AspNetCore.ResponseCaching.ResponseCachingMiddleware"
    };

    /// <summary>
    /// <para>zh-cn:是否允许使用当前 HTTP 请求上下文的 Path 参与判断，用于过滤日志内容本身不带路径的框架日志。</para>
    /// <para>en-us:Whether the current HTTP request Path can participate in matching, used to filter framework logs whose content does not include the path.</para>
    /// </summary>
    public bool EnableHttpContextPathMatching { get; set; } = true;
}
