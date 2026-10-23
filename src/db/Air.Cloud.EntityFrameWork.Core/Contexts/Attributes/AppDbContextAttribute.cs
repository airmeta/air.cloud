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
/// <para>zh-cn:为应用数据库上下文声明连接元数据、读库定位器和模型命名规则。</para>
/// <para>en-us:Declares connection metadata, read-context locators, and model naming rules for an application DbContext.</para>
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class AppDbContextAttribute : Attribute
{
    /// <summary>
    /// <para>zh-cn:创建使用默认连接配置的数据库上下文特性。</para>
    /// <para>en-us:Creates the attribute using the default connection configuration.</para>
    /// </summary>
    /// <param name="slaveDbContextLocators"><para>zh-cn:可用于只读查询的从库上下文定位器类型。</para><para>en-us:Context-locator types for read-only database queries.</para></param>
    public AppDbContextAttribute(params Type[] slaveDbContextLocators)
    {
        SlaveDbContextLocators = slaveDbContextLocators ?? Array.Empty<Type>();
    }

    /// <summary>
    /// <para>zh-cn:创建使用指定连接元数据和从库定位器的数据库上下文特性。</para>
    /// <para>en-us:Creates the attribute with the specified connection metadata and read-context locators.</para>
    /// </summary>
    /// <param name="connectionMetadata">
    /// <para>zh-cn:新配置应传入 `DataSourceSettings:ConnectionStrings` 下的连接名称；完整连接字符串和历史配置路径仅用于兼容。</para>
    /// <para>en-us:New configuration should pass a connection name under `DataSourceSettings:ConnectionStrings`; raw connection strings and historical configuration paths are compatibility options.</para>
    /// </param>
    /// <param name="slaveDbContextLocators"><para>zh-cn:可用于只读查询的从库上下文定位器类型。</para><para>en-us:Context-locator types for read-only database queries.</para></param>
    public AppDbContextAttribute(string connectionMetadata, params Type[] slaveDbContextLocators)
    {
        ConnectionMetadata = connectionMetadata ?? string.Empty;
        SlaveDbContextLocators = slaveDbContextLocators ?? Array.Empty<Type>();
    }

    /// <summary>
    /// <para>zh-cn:用于解析数据库连接的名称、配置路径或兼容的原始连接字符串。</para>
    /// <para>en-us:Name, configuration path, or compatible raw connection string used to resolve the database connection.</para>
    /// </summary>
    /// <remarks>
    /// <para>zh-cn:连接元数据最终由 DbProvider 解析为一个原始连接字符串，不会拆解连接字符串内容。</para>
    /// <para>en-us:DbProvider resolves the connection metadata to one raw connection string without parsing its contents.</para>
    /// </remarks>
    public string ConnectionMetadata { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:指定数据库上下文使用缓存模型还是动态模型。</para>
    /// <para>en-us:Specifies whether the database context uses a cached or dynamic model.</para>
    /// </summary>
    public DbContextMode Mode { get; set; } = DbContextMode.Cached;

    /// <summary>
    /// <para>zh-cn:应用于该上下文实体表名的统一前缀。</para>
    /// <para>en-us:Common prefix applied to entity table names in this context.</para>
    /// </summary>
    /// <remarks>前缀不能包含 . 和特殊符号，可使用下划线或短杆线</remarks>
    public string TablePrefix { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:应用于该上下文实体表名的统一后缀。</para>
    /// <para>en-us:Common suffix applied to entity table names in this context.</para>
    /// </summary>
    /// <remarks>后缀不能包含 . 和特殊符号，可使用下划线或短杆线</remarks>
    public string TableSuffix { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:当前上下文可用的从库定位器类型集合。</para>
    /// <para>en-us:The set of read-context locator types available to the current context.</para>
    /// </summary>
    public Type[] SlaveDbContextLocators { get; set; }
}
