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

using Microsoft.Extensions.Configuration;

using Air.Cloud.EntityFrameWork.Core.Options;

namespace Air.Cloud.EntityFrameWork.Core.Internal;

/// <summary>
/// <para>zh-cn:数据源连接字符串解析器，仅负责把配置引用解析为原始连接字符串。</para>
/// <para>en-us:Data source connection string resolver that only resolves configuration references to raw connection strings.</para>
/// </summary>
internal static class DataSourceConnectionStringResolver
{
    /// <summary>
    /// Air.Cloud 命名连接字符串的正式配置节点。
    /// <para>zh-cn:该成员的数据库访问行为。</para>
    /// <para>en-us:Database access behavior provided by this member.</para>
    /// </summary>
    internal const string ConfigurationPath = "DataSourceSettings:ConnectionStrings";

    /// <summary>
    /// <para>zh-cn:按统一规则解析连接元数据。该方法不解析连接字符串内部字段。</para>
    /// <para>en-us:Resolves connection metadata using the unified rules without parsing fields inside a connection string.</para>
    /// </summary>
    /// <param name="configuration">
    /// <para>zh-cn:应用配置根。</para>
    /// <para>en-us:The application configuration root.</para>
    /// </param>
    /// <param name="options">
    /// <para>zh-cn:已绑定的数据源选项；正式命名连接通过其 GetConnectionString 方法读取。</para>
    /// <para>en-us:The bound data source options; canonical named connections are read through its GetConnectionString method.</para>
    /// </param>
    /// <param name="connectionMetadata">
    /// <para>zh-cn:连接名称、兼容配置路径或原始连接字符串。</para>
    /// <para>en-us:A connection name, a legacy configuration path, or a raw connection string.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:解析后的原始连接字符串；没有输入时返回空字符串，未解析名称按兼容行为原样透传。</para>
    /// <para>en-us:The resolved raw connection string; an empty string for missing input, and unresolved names are passed through for compatibility.</para>
    /// </returns>
    internal static string Resolve(IConfiguration configuration, DataSourceOptions? options, string? connectionMetadata)
    {
        var metadata = connectionMetadata?.Render() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(metadata)) return string.Empty;

        // 连接串保持为原始字符串，不在 Core 中拆解 Provider 专属格式。
        if (metadata.Contains('=')) return metadata;

        // Air.Cloud 正式约定由已绑定的 DataSourceOptions 提供，并调用其现有 API。
        var airCloudConnectionString = options?.GetConnectionString(metadata);
        if (!string.IsNullOrWhiteSpace(airCloudConnectionString)) return airCloudConnectionString;

        // 服务注册阶段可能还没有建立 IOptions；此处只做同一节点的等价兜底。
        airCloudConnectionString = configuration[$"{ConfigurationPath}:{metadata}"];
        if (!string.IsNullOrWhiteSpace(airCloudConnectionString)) return airCloudConnectionString;

        // 兼容完整配置路径，包括历史的 ConnectionStrings:<name> 和自定义路径。
        if (metadata.Contains(':'))
        {
            return configuration[metadata] ?? metadata;
        }

        // 兼容官方根节点 ConnectionStrings:<name>。
        var officialConnectionString = configuration.GetConnectionString(metadata);
        if (!string.IsNullOrWhiteSpace(officialConnectionString)) return officialConnectionString;

        // 兼容历史上把任意一级配置键当作连接名称的调用。
        var legacyConnectionString = configuration[metadata];
        return !string.IsNullOrWhiteSpace(legacyConnectionString)
            ? legacyConnectionString
            : metadata;
    }
}
