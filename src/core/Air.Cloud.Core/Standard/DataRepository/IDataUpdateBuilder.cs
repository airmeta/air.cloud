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
using Air.Cloud.Core.Standard.DataBase.Model;

using System.Linq.Expressions;

namespace Air.Cloud.Core.Standard.DataRepository
{
    /// <summary>
    /// <para>zh-cn:通用数据更新构建器，用于描述条件更新中的字段赋值。</para>
    /// <para>en-us:Generic data update builder used to describe field assignments in conditional updates.</para>
    /// </summary>
    public interface IDataUpdateBuilder<TEntity>
        where TEntity : class, IPrivateEntity, new()
    {
        /// <summary>
        /// <para>zh-cn:设置属性为固定值。</para>
        /// <para>en-us:Sets a property to a constant value.</para>
        /// </summary>
        IDataUpdateBuilder<TEntity> SetProperty<TProperty>(
            Expression<Func<TEntity, TProperty>> propertyExpression,
            TProperty value);

        /// <summary>
        /// <para>zh-cn:设置属性为表达式值。</para>
        /// <para>en-us:Sets a property to an expression value.</para>
        /// </summary>
        IDataUpdateBuilder<TEntity> SetProperty<TProperty>(
            Expression<Func<TEntity, TProperty>> propertyExpression,
            Expression<Func<TEntity, TProperty>> valueExpression);
    }
}
