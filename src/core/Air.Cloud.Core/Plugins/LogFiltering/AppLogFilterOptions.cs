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
using Air.Cloud.Core.App.Options;
using Air.Cloud.Core.Attributes;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Air.Cloud.Core.Plugins.LogFiltering;

/// <summary>
/// <para>zh-cn:Air.Cloud 日志过滤配置，用于控制健康检查等框架噪音日志的忽略路径、分类范围和级别上限。</para>
/// <para>en-us:Air.Cloud log filtering options used to control ignored paths, category scope, and maximum level for framework-noise logs such as health checks.</para>
/// </summary>
[ConfigurationInfo("AppLogFilter")]
public sealed class AppLogFilterOptions : IConfigurableOptions<AppLogFilterOptions>
{
    /// <summary>
    /// <para>zh-cn:Air.Cloud 自定义 ILogger Provider 默认允许输出的最低级别；默认 Trace 表示保持既有输出行为。</para>
    /// <para>en-us:The default minimum level allowed by the Air.Cloud custom ILogger provider; the Trace default preserves existing output behavior.</para>
    /// </summary>
    public LogLevel MinimumLevel { get; set; } = LogLevel.Trace;

    /// <summary>
    /// <para>zh-cn:按日志分类前缀覆盖最低输出级别。键按前缀匹配，最长前缀优先，例如 `Ocelot` 可匹配 `Ocelot.Requester.Middleware`。</para>
    /// <para>en-us:Minimum-level overrides by logger category prefix. Keys are matched as prefixes with the longest prefix winning; for example, `Ocelot` matches `Ocelot.Requester.Middleware`.</para>
    /// </summary>
    public IDictionary<string, LogLevel> CategoryMinimumLevels { get; set; } = new Dictionary<string, LogLevel>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// <para>zh-cn:按日志分类前缀设置最低输出级别的别名配置。该属性用于兼容“最小级别覆盖”命名；后期配置时会合并到 `CategoryMinimumLevels`，相同前缀以 `CategoryMinimumLevels` 为准。</para>
    /// <para>en-us:Alias configuration for minimum output levels by logger category prefix. This property supports the "minimum level overrides" naming; during post-configuration it is merged into `CategoryMinimumLevels`, with `CategoryMinimumLevels` winning duplicate prefixes.</para>
    /// </summary>
    public IDictionary<string, LogLevel> MinimumLevelOverrides { get; set; } = new Dictionary<string, LogLevel>(StringComparer.OrdinalIgnoreCase);

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

    /// <summary>
    /// <para>zh-cn:规范化日志过滤配置。该方法会补齐空集合，并把分类最低级别覆盖转换为不区分大小写的字典，保证配置绑定、手动构造和远程配置读取后的匹配行为一致。</para>
    /// <para>en-us:Normalizes log-filter options. It restores null collections and converts category minimum-level overrides to a case-insensitive dictionary so configuration binding, manual construction, and remote configuration reads match consistently.</para>
    /// </summary>
    /// <param name="options">
    /// <para>zh-cn:已绑定的日志过滤配置。</para>
    /// <para>en-us:The bound log-filter options.</para>
    /// </param>
    /// <param name="configuration">
    /// <para>zh-cn:配置节对象；当前实现仅用于符合框架可配置选项生命周期。</para>
    /// <para>en-us:The configuration section; currently used only to participate in the framework configurable-options lifecycle.</para>
    /// </param>
    public void PostConfigure(AppLogFilterOptions options, IConfiguration configuration)
    {
        options.IgnorePaths ??= new List<string>();
        options.CategoryPrefixes ??= new List<string>();
        options.CategoryMinimumLevels = NormalizeCategoryMinimumLevels(options.CategoryMinimumLevels);
        options.MinimumLevelOverrides = NormalizeCategoryMinimumLevels(options.MinimumLevelOverrides);

        foreach (var item in options.MinimumLevelOverrides)
        {
            if (!options.CategoryMinimumLevels.ContainsKey(item.Key))
            {
                options.CategoryMinimumLevels[item.Key] = item.Value;
            }
        }
    }

    private static Dictionary<string, LogLevel> NormalizeCategoryMinimumLevels(IDictionary<string, LogLevel> categoryMinimumLevels)
    {
        var result = new Dictionary<string, LogLevel>(StringComparer.OrdinalIgnoreCase);
        if (categoryMinimumLevels is null)
        {
            return result;
        }

        foreach (var item in categoryMinimumLevels)
        {
            if (!string.IsNullOrWhiteSpace(item.Key))
            {
                result[item.Key.Trim()] = item.Value;
            }
        }

        return result;
    }
}
