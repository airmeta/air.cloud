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

namespace Air.Cloud.EntityFrameWork.Core.Entities.Attributes;

/// <summary>
/// <para>zh-cn:声明实体属性在 EF Core 关系型模型中的数据库列类型。该特性只保存期望的 CLR 类型和数据库类型名称，实际是否应用由已注册的列元数据提供器决定。</para>
/// <para>en-us:Declares the database column type for an entity property in the EF Core relational model. The attribute only stores the expected CLR type and database type name; applying it is controlled by the registered column metadata provider.</para>
/// </summary>
[IgnoreScanning, AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class ColumnTypeAttribute : Attribute
{
    /// <summary>
    /// <para>zh-cn:创建列类型声明。clrType 用于校验声明适用的属性类型，数据库类型名称会从已注册提供器的公共类型映射中解析。</para>
    /// <para>en-us:Creates a column type declaration. clrType validates the property type the declaration applies to, and the database type name is resolved from the registered provider's common type mappings.</para>
    /// </summary>
    /// <param name="clrType">
    /// <para>zh-cn:特性适用的 CLR 类型，例如 typeof(DateTime) 或 typeof(string)。可空值类型会按其基础类型匹配。</para>
    /// <para>en-us:The CLR type the attribute applies to, such as typeof(DateTime) or typeof(string). Nullable value types are matched by their underlying type.</para>
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <para>zh-cn:当 clrType 为空时抛出。</para>
    /// <para>en-us:Thrown when clrType is null.</para>
    /// </exception>
    public ColumnTypeAttribute(Type clrType)
    {
        ClrType = clrType ?? throw new ArgumentNullException(nameof(clrType));
    }

    /// <summary>
    /// <para>zh-cn:创建列类型声明。clrType 用于校验声明适用的属性类型，typeName 是要写入 EF Core 元数据的关系型数据库类型名称。</para>
    /// <para>en-us:Creates a column type declaration. clrType validates the property type the declaration applies to, and typeName is the relational database type name written into EF Core metadata.</para>
    /// </summary>
    /// <param name="clrType">
    /// <para>zh-cn:特性适用的 CLR 类型，例如 typeof(DateTime) 或 typeof(string)。可空值类型会按其基础类型匹配。</para>
    /// <para>en-us:The CLR type the attribute applies to, such as typeof(DateTime) or typeof(string). Nullable value types are matched by their underlying type.</para>
    /// </param>
    /// <param name="typeName">
    /// <para>zh-cn:数据库列类型名称，例如 timestamp without time zone、varchar 或 numeric(18,2)。</para>
    /// <para>en-us:The database column type name, such as timestamp without time zone, varchar, or numeric(18,2).</para>
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <para>zh-cn:当 clrType 为空时抛出。</para>
    /// <para>en-us:Thrown when clrType is null.</para>
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <para>zh-cn:当 typeName 为空白时抛出。</para>
    /// <para>en-us:Thrown when typeName is blank.</para>
    /// </exception>
    public ColumnTypeAttribute(Type clrType, string typeName)
    {
        ClrType = clrType ?? throw new ArgumentNullException(nameof(clrType));
        TypeName = string.IsNullOrWhiteSpace(typeName)
            ? throw new ArgumentException("Column type name cannot be empty.", nameof(typeName))
            : typeName;
    }

    /// <summary>
    /// <para>zh-cn:特性适用的 CLR 类型。提供器应用前会与实体属性类型匹配，避免错误声明覆盖不相关字段。</para>
    /// <para>en-us:The CLR type the attribute applies to. Providers match it with the entity property type before applying the metadata to avoid accidental changes to unrelated fields.</para>
    /// </summary>
    public Type ClrType { get; }

    /// <summary>
    /// <para>zh-cn:要写入 EF Core 关系型元数据的数据库列类型名称。</para>
    /// <para>en-us:The database column type name written into EF Core relational metadata.</para>
    /// </summary>
    public string? TypeName { get; }
}
