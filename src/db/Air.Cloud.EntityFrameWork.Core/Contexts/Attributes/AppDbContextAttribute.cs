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

using Air.Cloud.EntityFrameWork.Core.Contexts.Enums;

namespace Air.Cloud.EntityFrameWork.Core.Contexts.Attributes;

/// <summary>
/// 数据库上下文配置特性
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class AppDbContextAttribute : Attribute
{
    /// <summary>
    /// 默认构造函数
    /// </summary>
    /// <param name="slaveDbContextLocators"></param>
    public AppDbContextAttribute(params Type[] slaveDbContextLocators)
    {
        SlaveDbContextLocators = slaveDbContextLocators ?? Array.Empty<Type>();
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="connectionMetadata">
    /// <para>zh-cn:新配置应传入 `DataSourceSettings:ConnectionStrings` 下的连接名称；完整连接字符串和历史配置路径仅用于兼容。</para>
    /// <para>en-us:New configuration should pass a connection name under `DataSourceSettings:ConnectionStrings`; raw connection strings and historical configuration paths are compatibility options.</para>
    /// </param>
    /// <param name="slaveDbContextLocators"></param>
    public AppDbContextAttribute(string connectionMetadata, params Type[] slaveDbContextLocators)
    {
        ConnectionMetadata = connectionMetadata ?? string.Empty;
        SlaveDbContextLocators = slaveDbContextLocators ?? Array.Empty<Type>();
    }

    /// <summary>
    /// 数据库连接元数据
    /// </summary>
    /// <remarks>
    /// <para>zh-cn:连接元数据最终由 DbProvider 解析为一个原始连接字符串，不会拆解连接字符串内容。</para>
    /// <para>en-us:DbProvider resolves the connection metadata to one raw connection string without parsing its contents.</para>
    /// </remarks>
    public string ConnectionMetadata { get; set; } = string.Empty;

    /// <summary>
    /// 数据库上下文模式
    /// </summary>
    public DbContextMode Mode { get; set; } = DbContextMode.Cached;

    /// <summary>
    /// 表统一前缀
    /// </summary>
    /// <remarks>前缀不能包含 . 和特殊符号，可使用下划线或短杆线</remarks>
    public string TablePrefix { get; set; } = string.Empty;

    /// <summary>
    /// 表统一后缀
    /// </summary>
    /// <remarks>后缀不能包含 . 和特殊符号，可使用下划线或短杆线</remarks>
    public string TableSuffix { get; set; } = string.Empty;

    /// <summary>
    /// 指定从库定位器
    /// </summary>
    public Type[] SlaveDbContextLocators { get; set; }
}
