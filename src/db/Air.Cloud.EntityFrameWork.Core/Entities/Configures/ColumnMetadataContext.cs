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

using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using System.Reflection;

namespace Air.Cloud.EntityFrameWork.Core.Entities.Configures;

/// <summary>
/// <para>zh-cn:列元数据提供器的执行上下文，暴露当前 DbContext、实体、属性和 EF Core 可变元数据对象，供提供器在模型构建阶段修改列类型、列名等关系型元数据。</para>
/// <para>en-us:Execution context for column metadata providers. It exposes the current DbContext, entity, property, and EF Core mutable metadata objects so providers can change relational metadata such as column type and column name during model building.</para>
/// </summary>
public sealed class ColumnMetadataContext
{
    /// <summary>
    /// <para>zh-cn:创建列元数据上下文。所有参数都必须来自当前模型构建流程，不能跨 DbContext 或跨模型缓存复用。</para>
    /// <para>en-us:Creates a column metadata context. All arguments must come from the current model-building flow and must not be reused across DbContext instances or model cache entries.</para>
    /// </summary>
    /// <param name="modelBuilder">
    /// <para>zh-cn:当前 EF Core 模型构建器。</para>
    /// <para>en-us:The current EF Core model builder.</para>
    /// </param>
    /// <param name="entityBuilder">
    /// <para>zh-cn:当前实体类型构建器。</para>
    /// <para>en-us:The current entity type builder.</para>
    /// </param>
    /// <param name="entityType">
    /// <para>zh-cn:当前实体的可变元数据。</para>
    /// <para>en-us:The mutable metadata of the current entity.</para>
    /// </param>
    /// <param name="property">
    /// <para>zh-cn:当前属性的可变元数据。</para>
    /// <para>en-us:The mutable metadata of the current property.</para>
    /// </param>
    /// <param name="propertyInfo">
    /// <para>zh-cn:当前 CLR 属性信息。</para>
    /// <para>en-us:The current CLR property information.</para>
    /// </param>
    /// <param name="dbContext">
    /// <para>zh-cn:正在构建模型的 DbContext 实例。</para>
    /// <para>en-us:The DbContext instance whose model is being built.</para>
    /// </param>
    /// <param name="dbContextLocator">
    /// <para>zh-cn:当前数据库上下文定位器类型。</para>
    /// <para>en-us:The current database context locator type.</para>
    /// </param>
    public ColumnMetadataContext(
        ModelBuilder modelBuilder,
        EntityTypeBuilder entityBuilder,
        IMutableEntityType entityType,
        IMutableProperty property,
        PropertyInfo propertyInfo,
        DbContext dbContext,
        Type dbContextLocator)
    {
        ModelBuilder = modelBuilder ?? throw new ArgumentNullException(nameof(modelBuilder));
        EntityBuilder = entityBuilder ?? throw new ArgumentNullException(nameof(entityBuilder));
        EntityType = entityType ?? throw new ArgumentNullException(nameof(entityType));
        Property = property ?? throw new ArgumentNullException(nameof(property));
        PropertyInfo = propertyInfo ?? throw new ArgumentNullException(nameof(propertyInfo));
        DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        DbContextLocator = dbContextLocator ?? throw new ArgumentNullException(nameof(dbContextLocator));
    }

    /// <summary>
    /// <para>zh-cn:当前 EF Core 模型构建器。</para>
    /// <para>en-us:The current EF Core model builder.</para>
    /// </summary>
    public ModelBuilder ModelBuilder { get; }

    /// <summary>
    /// <para>zh-cn:当前实体类型构建器。</para>
    /// <para>en-us:The current entity type builder.</para>
    /// </summary>
    public EntityTypeBuilder EntityBuilder { get; }

    /// <summary>
    /// <para>zh-cn:当前实体的可变元数据。</para>
    /// <para>en-us:The mutable metadata of the current entity.</para>
    /// </summary>
    public IMutableEntityType EntityType { get; }

    /// <summary>
    /// <para>zh-cn:当前属性的可变元数据，可用于 SetColumnType、SetColumnName 等 EF Core 关系型元数据操作。</para>
    /// <para>en-us:The mutable metadata of the current property. It can be used for EF Core relational metadata operations such as SetColumnType and SetColumnName.</para>
    /// </summary>
    public IMutableProperty Property { get; }

    /// <summary>
    /// <para>zh-cn:当前 CLR 属性信息，可用于读取属性类型和自定义特性。</para>
    /// <para>en-us:The current CLR property information, used to inspect property type and custom attributes.</para>
    /// </summary>
    public PropertyInfo PropertyInfo { get; }

    /// <summary>
    /// <para>zh-cn:正在构建模型的 DbContext 实例。</para>
    /// <para>en-us:The DbContext instance whose model is being built.</para>
    /// </summary>
    public DbContext DbContext { get; }

    /// <summary>
    /// <para>zh-cn:当前数据库上下文定位器类型。</para>
    /// <para>en-us:The current database context locator type.</para>
    /// </summary>
    public Type DbContextLocator { get; }
}
