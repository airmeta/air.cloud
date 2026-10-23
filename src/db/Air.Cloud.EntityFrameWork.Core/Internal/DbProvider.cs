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

using Air.Cloud.EntityFrameWork.Core.Contexts.Attributes;

using Air.Cloud.Core.App;

using Microsoft.Extensions.Configuration;

using System.Collections.Concurrent;

namespace Air.Cloud.EntityFrameWork.Core.Internal;

/// <summary>
/// <para>zh-cn:数据库 Provider 和上下文特性解析辅助入口。</para>
/// <para>en-us:Helper entry point for resolving database Providers and context attributes.</para>
/// </summary>
[IgnoreScanning]
public static class DbProvider
{
    /// <summary>
    /// 数据库上下文 [AppDbContext] 特性缓存
    /// <para>zh-cn:该成员的数据库访问行为。</para>
    /// <para>en-us:Database access behavior provided by this member.</para>
    /// </summary>
    private static readonly ConcurrentDictionary<Type, AppDbContextAttribute> DbContextAppDbContextAttributes;
    /// <summary>
    /// 构造函数
    /// <para>zh-cn:该成员的数据库访问行为。</para>
    /// <para>en-us:Database access behavior provided by this member.</para>
    /// </summary>
    static DbProvider()
    {
        DbContextAppDbContextAttributes = new ConcurrentDictionary<Type, AppDbContextAttribute>();
    }

    /// <summary>
    /// <para>zh-cn:判断 Provider 名称是否与数据库程序集名称一致。</para>
    /// <para>en-us:Determines whether a Provider name matches a database assembly name.</para>
    /// </summary>
    /// <param name="providerName"><para>zh-cn:要比较的 Provider 名称。</para><para>en-us:The Provider name to compare.</para></param>
    /// <param name="dbAssemblyName"><para>zh-cn:数据库程序集名称。</para><para>en-us:The database assembly name.</para></param>
    /// <returns><para>zh-cn:名称完全一致时返回 true。</para><para>en-us:True when the names match exactly.</para></returns>
    public static bool IsDatabaseFor(string providerName, string dbAssemblyName)
    {
        return providerName.Equals(dbAssemblyName, StringComparison.Ordinal);
    }

    /// <summary>
    /// <para>zh-cn:兼容入口：获取数据库上下文连接字符串。</para>
    /// <para>en-us:Compatibility entry point for resolving a DbContext connection string.</para>
    /// </summary>
    /// <remarks>
    /// <para>zh-cn:新的 AddDb 和 AddDbPool 注册流程直接使用 DataSourceOptions.GetConnectionString；此方法保留给已有业务代码。</para>
    /// <para>en-us:New AddDb and AddDbPool registrations use DataSourceOptions.GetConnectionString directly; this method remains for existing application code.</para>
    /// </remarks>
    /// <typeparam name="TDbContext"><para>zh-cn:泛型参数类型</para><para>en-us:The database context locator type.</para></typeparam>
    /// <param name="connectionMetadata">
    /// <para>zh-cn:连接元数据。新配置应传入 <c>DataSourceSettings:ConnectionStrings</c> 下的名称；直接连接字符串、官方 <c>ConnectionStrings</c> 节点和完整配置路径仅用于兼容旧调用。</para>
    /// <para>en-us:Connection metadata. New configuration should pass a name under <c>DataSourceSettings:ConnectionStrings</c>; raw connection strings, the official <c>ConnectionStrings</c> section, and full configuration paths are supported only for legacy compatibility.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:解析后的原始连接字符串；没有元数据时返回空字符串，未解析的名称按历史兼容行为原样透传。</para>
    /// <para>en-us:The resolved raw connection string; an empty string is returned when metadata is absent, while unresolved names are passed through for legacy compatibility.</para>
    /// </returns>
    public static string GetConnectionString<TDbContext>(string? connectionMetadata = default)
        where TDbContext : DbContext
    {
        var metadata = GetConnectionMetadata<TDbContext>(connectionMetadata);
        var configuration = AppConfiguration.Configuration;
        var resolved = DataSourceConnectionStringResolver.Resolve(configuration, null, metadata);

        // 直接调用兼容入口时，宿主可能尚未把 InnerConfiguration 合并到 ConfigurationManager。
        // 两者使用同一解析规则，避免连接名称被错误地原样透传。
        if (string.Equals(resolved, metadata, StringComparison.Ordinal)
            && AppConfigurationLoader.InnerConfiguration != null
            && !ReferenceEquals(configuration, AppConfigurationLoader.InnerConfiguration))
        {
            resolved = DataSourceConnectionStringResolver.Resolve(AppConfigurationLoader.InnerConfiguration, null, metadata);
        }

        return resolved;
    }

    /// <summary>
    /// <para>zh-cn:选择 DbContext 的连接元数据。该方法只处理显式参数和上下文特性，不读取配置值。</para>
    /// <para>en-us:Selects DbContext connection metadata. This method handles only the explicit argument and context attribute; it does not read configuration values.</para>
    /// </summary>
    /// <typeparam name="TDbContext"><para>zh-cn:数据库上下文类型。</para><para>en-us:The generic parameter type.</para></typeparam>
    /// <param name="connectionMetadata"><para>zh-cn:显式连接元数据。</para><para>en-us:The connectionMetadata value.</para></param>
    /// <returns><para>zh-cn:选中的连接元数据。</para><para>en-us:The operation result.</para></returns>
    internal static string GetConnectionMetadata<TDbContext>(string? connectionMetadata = default)
        where TDbContext : DbContext
    {
        var metadata = connectionMetadata?.Render() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(metadata)) return metadata;

        return GetAppDbContextAttribute(typeof(TDbContext))?.ConnectionMetadata?.Render() ?? string.Empty;
    }

    /// <summary>
    /// <para>zh-cn:获取数据库上下文类型上声明的 AppDbContext 配置特性，并缓存解析结果。</para>
    /// <para>en-us:Gets and caches the AppDbContext configuration attribute declared on a database context type.</para>
    /// </summary>
    /// <param name="dbContexType"><para>zh-cn:要读取特性的数据库上下文类型。</para><para>en-us:The database context type whose attribute is read.</para></param>
    /// <returns><para>zh-cn:找到的配置特性；未声明时返回 null。</para><para>en-us:The configuration attribute, or null when none is declared.</para></returns>
    public static AppDbContextAttribute? GetAppDbContextAttribute(Type dbContexType)
    {
        if (DbContextAppDbContextAttributes.TryGetValue(dbContexType, out var cachedAttribute))
        {
            return cachedAttribute;
        }

        var appDbContextAttribute = Function(dbContexType);
        if (appDbContextAttribute != null)
        {
            DbContextAppDbContextAttributes[dbContexType] = appDbContextAttribute;
        }

        return appDbContextAttribute;

        // 本地静态函数
        static AppDbContextAttribute? Function(Type dbContextType)
        {
            if (!dbContextType.IsDefined(typeof(AppDbContextAttribute), true)) return default;

            var appDbContextAttribute = dbContextType.GetCustomAttribute<AppDbContextAttribute>(true);

            return appDbContextAttribute;
        }
    }
}
