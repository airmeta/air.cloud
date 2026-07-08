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

using Air.Cloud.EntityFrameWork.Core.Entities.Attributes;

using System.Reflection;

namespace Air.Cloud.EntityFrameWork.Core.Entities.Configures;

/// <summary>
/// <para>zh-cn:默认列元数据提供器基类。框架不会自动注册该类型；业务项目可继承后通过 AddSingleton 注册，并通过 TypeMappings 配置 CLR 类型到数据库列类型的映射。</para>
/// <para>en-us:Default base class for column metadata providers. The framework does not register this type automatically; applications can inherit it, register the derived type with AddSingleton, and configure CLR-to-database column type mappings through TypeMappings.</para>
/// </summary>
public class DefaultColumnMetadataProvider : IColumnMetadataProvider
{
    /// <summary>
    /// <para>zh-cn:CLR 类型到数据库列类型名称的映射表。派生类可重写该属性，例如把 DateTime 映射到 timestamp without time zone。</para>
    /// <para>en-us:Mapping table from CLR types to database column type names. Derived classes can override this property, for example mapping DateTime to timestamp without time zone.</para>
    /// </summary>
    protected virtual IReadOnlyDictionary<Type, string> TypeMappings { get; } = new Dictionary<Type, string>();

    /// <summary>
    /// <para>zh-cn:应用列元数据规则。默认行为先应用 TypeMappings，再应用 ColumnTypeAttribute，因此属性级声明可以覆盖类型映射表。</para>
    /// <para>en-us:Applies column metadata rules. The default behavior applies TypeMappings first and ColumnTypeAttribute second, so property-level declarations can override the type mapping table.</para>
    /// </summary>
    /// <param name="context">
    /// <para>zh-cn:当前实体属性的模型构建上下文。</para>
    /// <para>en-us:The model-building context for the current entity property.</para>
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <para>zh-cn:当 context 为空时抛出。</para>
    /// <para>en-us:Thrown when context is null.</para>
    /// </exception>
    public virtual void Apply(ColumnMetadataContext context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        ApplyTypeMapping(context);
        ApplyColumnTypeAttribute(context);
    }

    /// <summary>
    /// <para>zh-cn:按 TypeMappings 应用当前属性的列类型。可空值类型会按其基础类型查找映射。</para>
    /// <para>en-us:Applies the column type for the current property from TypeMappings. Nullable value types are looked up by their underlying type.</para>
    /// </summary>
    /// <param name="context">
    /// <para>zh-cn:当前实体属性的模型构建上下文。</para>
    /// <para>en-us:The model-building context for the current entity property.</para>
    /// </param>
    protected virtual void ApplyTypeMapping(ColumnMetadataContext context)
    {
        var propertyType = GetNormalizedType(context.PropertyInfo.PropertyType);
        if (TypeMappings.TryGetValue(propertyType, out var columnType) && !string.IsNullOrWhiteSpace(columnType))
        {
            context.Property.SetColumnType(columnType);
        }
    }

    /// <summary>
    /// <para>zh-cn:应用 ColumnTypeAttribute 声明的列类型。只有特性声明的 CLR 类型与属性类型匹配时才会写入元数据。</para>
    /// <para>en-us:Applies the column type declared by ColumnTypeAttribute. Metadata is written only when the CLR type declared by the attribute matches the property type.</para>
    /// </summary>
    /// <param name="context">
    /// <para>zh-cn:当前实体属性的模型构建上下文。</para>
    /// <para>en-us:The model-building context for the current entity property.</para>
    /// </param>
    protected virtual void ApplyColumnTypeAttribute(ColumnMetadataContext context)
    {
        var attribute = context.PropertyInfo.GetCustomAttribute<ColumnTypeAttribute>(true);
        if (attribute is null)
        {
            return;
        }

        var propertyType = GetNormalizedType(context.PropertyInfo.PropertyType);
        var attributeType = GetNormalizedType(attribute.ClrType);
        if (propertyType != attributeType)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(attribute.TypeName))
        {
            context.Property.SetColumnType(attribute.TypeName);
            return;
        }

        if (TypeMappings.TryGetValue(attributeType, out var columnType) && !string.IsNullOrWhiteSpace(columnType))
        {
            context.Property.SetColumnType(columnType);
        }
    }

    /// <summary>
    /// <para>zh-cn:标准化类型，便于 DateTime? 与 DateTime 使用同一套类型映射规则。</para>
    /// <para>en-us:Normalizes a type so DateTime? and DateTime can use the same type mapping rule.</para>
    /// </summary>
    /// <param name="type">
    /// <para>zh-cn:待标准化的 CLR 类型。</para>
    /// <para>en-us:The CLR type to normalize.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:可空值类型的基础类型，或原始类型。</para>
    /// <para>en-us:The underlying type of a nullable value type, or the original type.</para>
    /// </returns>
    protected static Type GetNormalizedType(Type type)
    {
        return Nullable.GetUnderlyingType(type) ?? type;
    }
}
