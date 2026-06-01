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

using Microsoft.Extensions.Configuration;

using System.Collections.Concurrent;

namespace Air.Cloud.EntityFrameWork.Core.Internal;

/// <summary>
/// 数据库提供器选项
/// </summary>
[IgnoreScanning]
public static class DbProvider
{
    /// <summary>
    /// 数据库上下文 [AppDbContext] 特性缓存
    /// </summary>
    private static readonly ConcurrentDictionary<Type, AppDbContextAttribute> DbContextAppDbContextAttributes;
    /// <summary>
    /// 构造函数
    /// </summary>
    static DbProvider()
    {
        DbContextAppDbContextAttributes = new ConcurrentDictionary<Type, AppDbContextAttribute>();
    }

    /// <summary>
    /// 判断是否是特定数据库
    /// </summary>
    /// <param name="providerName"></param>
    /// <param name="dbAssemblyName"></param>
    /// <returns>bool</returns>
    public static bool IsDatabaseFor(string providerName, string dbAssemblyName)
    {
        return providerName.Equals(dbAssemblyName, StringComparison.Ordinal);
    }

    /// <summary>
    /// 获取数据库上下文连接字符串
    /// </summary>
    /// <typeparam name="TDbContext"></typeparam>
    /// <param name="connectionMetadata">支持数据库连接字符串，配置文件的 ConnectionStrings 中的Key或 配置文件的完整的配置路径，如果是内存数据库，则为数据库名称</param>
    /// <returns></returns>
    public static string GetConnectionString<TDbContext>(string? connectionMetadata = default)
        where TDbContext : DbContext
    {
        // 支持读取配置渲染
        var connStrOrConnKey = connectionMetadata?.Render() ?? string.Empty;

        // 如果没有配置数据库连接字符串，那么查找特性
        if (string.IsNullOrWhiteSpace(connStrOrConnKey))
        {
            var dbContextAttribute = GetAppDbContextAttribute(typeof(TDbContext));
            if (dbContextAttribute == null) return string.Empty;

            // 获取特性连接字符串（渲染配置模板）
            connStrOrConnKey = dbContextAttribute.ConnectionMetadata?.Render() ?? string.Empty;
        }

        // 如果都没有，则直接返回空
        if (string.IsNullOrWhiteSpace(connStrOrConnKey)) return string.Empty;

        // 如果包含 = 符号，那么认为是连接字符串
        if (connStrOrConnKey.Contains('=')) return connStrOrConnKey;
        else
        {
            // 如果包含 : 符号，那么认为是一个 Key 路径
            if (connStrOrConnKey.Contains(':')) return AppConfiguration.Configuration[connStrOrConnKey] ?? string.Empty;
            else
            {
                // 首先查找 DbConnectionString 键，如果没有找到，则当成 Key 去查找
                var connStrValue = AppConfiguration.Configuration.GetConnectionString(connStrOrConnKey);
                return (!string.IsNullOrWhiteSpace(connStrValue) ? connStrValue : AppConfiguration.Configuration[connStrOrConnKey]) ?? connStrOrConnKey;
            }
        }
    }

    /// <summary>
    /// 获取数据库上下文 [AppDbContext] 特性
    /// </summary>
    /// <param name="dbContexType"></param>
    /// <returns></returns>
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
