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
using Microsoft.Extensions.Options;

using System.Collections.Concurrent;

namespace Air.Cloud.Core.Plugins.LogFiltering;

/// <summary>
/// <para>zh-cn:默认 Air.Cloud 日志过滤插件，按请求路径过滤健康检查等低级别框架噪音日志，并保留 Warning 及以上日志。</para>
/// <para>en-us:Default Air.Cloud log filtering plugin that filters low-level framework-noise logs such as health checks by request path while keeping Warning and above.</para>
/// </summary>
public class DefaultAppLogFilterPlugin : IAppLogFilterPlugin
{
    private readonly ConcurrentDictionary<string, byte> _ignorePaths = new(StringComparer.OrdinalIgnoreCase);
    private readonly AppLogFilterOptions _options;

    /// <summary>
    /// <para>zh-cn:使用默认选项创建默认日志过滤插件，主要用于插件扫描或手动创建场景。</para>
    /// <para>en-us:Creates the default log filtering plugin with default options, mainly for plugin scanning or manual creation scenarios.</para>
    /// </summary>
    public DefaultAppLogFilterPlugin()
        : this(null)
    {
    }

    /// <summary>
    /// <para>zh-cn:创建默认日志过滤插件，并从配置选项中加载初始忽略路径。</para>
    /// <para>en-us:Creates the default log filtering plugin and loads initial ignored paths from options.</para>
    /// </summary>
    /// <param name="options">
    /// <para>zh-cn:日志过滤选项；为空时使用默认配置。</para>
    /// <para>en-us:The log filtering options; default values are used when null.</para>
    /// </param>
    public DefaultAppLogFilterPlugin(IOptions<AppLogFilterOptions> options = null)
    {
        _options = options?.Value ?? new AppLogFilterOptions();

        foreach (var path in _options.IgnorePaths)
        {
            AddIgnorePath(path);
        }
    }

    /// <inheritdoc/>
    public IReadOnlyCollection<string> IgnorePaths => _ignorePaths.Keys.ToArray();

    /// <inheritdoc/>
    public void AddIgnorePath(string path)
    {
        var normalized = NormalizePath(path);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return;
        }

        _ignorePaths.TryAdd(normalized, 0);
    }

    /// <inheritdoc/>
    public bool ShouldIgnore(AppLogFilterContext context)
    {
        if (context == null)
        {
            return false;
        }

        if (context.LogLevel == LogLevel.None || context.LogLevel > _options.MaximumIgnoredLevel)
        {
            return false;
        }

        if (!IsCategoryAllowed(context.CategoryName))
        {
            return false;
        }

        if (_ignorePaths.IsEmpty)
        {
            return false;
        }

        foreach (var path in _ignorePaths.Keys)
        {
            if (ContainsPath(context.Content, path))
            {
                return true;
            }
        }

        if (!_options.EnableHttpContextPathMatching)
        {
            return false;
        }

        var requestPath = NormalizePath(context.RequestPath);
        return !string.IsNullOrWhiteSpace(requestPath) && _ignorePaths.ContainsKey(requestPath);
    }

    /// <summary>
    /// <para>zh-cn:规范化请求路径，支持完整 URL、查询字符串和未带斜杠的路径。</para>
    /// <para>en-us:Normalizes request paths, supporting full URLs, query strings, and paths without a leading slash.</para>
    /// </summary>
    /// <param name="path">
    /// <para>zh-cn:待规范化的路径。</para>
    /// <para>en-us:The path to normalize.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:规范化后的路径；输入为空时返回空字符串。</para>
    /// <para>en-us:The normalized path; returns an empty string for empty input.</para>
    /// </returns>
    public static string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        var normalized = path.Trim();
        if (Uri.TryCreate(normalized, UriKind.Absolute, out var uri))
        {
            normalized = uri.AbsolutePath;
        }

        var queryIndex = normalized.IndexOf('?', StringComparison.Ordinal);
        if (queryIndex >= 0)
        {
            normalized = normalized[..queryIndex];
        }

        if (!normalized.StartsWith("/", StringComparison.Ordinal))
        {
            normalized = $"/{normalized}";
        }

        return normalized.TrimEnd('/');
    }

    private bool IsCategoryAllowed(string categoryName)
    {
        if (_options.CategoryPrefixes.Count == 0)
        {
            return true;
        }

        return _options.CategoryPrefixes.Any(prefix =>
            !string.IsNullOrWhiteSpace(prefix)
            && categoryName?.StartsWith(prefix, StringComparison.Ordinal) == true);
    }

    private static bool ContainsPath(string content, string path)
    {
        if (string.IsNullOrWhiteSpace(content) || string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        var index = content.IndexOf(path, StringComparison.OrdinalIgnoreCase);
        while (index >= 0)
        {
            var end = index + path.Length;
            if (end >= content.Length || IsPathBoundary(content[end]))
            {
                return true;
            }

            index = content.IndexOf(path, end, StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    private static bool IsPathBoundary(char value)
    {
        return value is ' ' or '?' or '#' or '"' or '\'' or '/' or '\\' or '-' or '&';
    }
}
