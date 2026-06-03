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
using Air.Cloud.Core.Standard.DataRepository;

using Microsoft.EntityFrameworkCore.Query;

using System.Linq.Expressions;
using System.Reflection;

namespace Air.Cloud.EntityFrameWork.Core.DataRepository
{
    /// <summary>
    /// <para>zh-cn:EFCore 条件更新构建器。</para>
    /// <para>en-us:EFCore conditional update builder.</para>
    /// </summary>
    internal sealed class EntityFrameworkDataUpdateBuilder<TEntity> : IDataUpdateBuilder<TEntity>
        where TEntity : class, IPrivateEntity, new()
    {
        private readonly List<IEntityFrameworkDataUpdateSetter> setters = new();

        /// <inheritdoc />
        public IDataUpdateBuilder<TEntity> SetProperty<TProperty>(
            Expression<Func<TEntity, TProperty>> propertyExpression,
            TProperty value)
        {
            setters.Add(new EntityFrameworkDataUpdateSetter<TProperty>(propertyExpression, value));
            return this;
        }

        /// <inheritdoc />
        public IDataUpdateBuilder<TEntity> SetProperty<TProperty>(
            Expression<Func<TEntity, TProperty>> propertyExpression,
            Expression<Func<TEntity, TProperty>> valueExpression)
        {
            setters.Add(new EntityFrameworkDataUpdateSetter<TProperty>(propertyExpression, valueExpression));
            return this;
        }

        /// <summary>
        /// <para>zh-cn:生成 EFCore ExecuteUpdate 字段赋值表达式。</para>
        /// <para>en-us:Builds an EFCore ExecuteUpdate field assignment expression.</para>
        /// </summary>
        public Expression<Func<SetPropertyCalls<TEntity>, SetPropertyCalls<TEntity>>> Build()
        {
            if (setters.Count == 0)
            {
                throw new InvalidOperationException("At least one property must be configured for update.");
            }

            var parameter = Expression.Parameter(typeof(SetPropertyCalls<TEntity>), "setters");
            Expression current = parameter;
            foreach (var setter in setters)
            {
                current = setter.Apply(current);
            }

            return Expression.Lambda<Func<SetPropertyCalls<TEntity>, SetPropertyCalls<TEntity>>>(current, parameter);
        }

        private interface IEntityFrameworkDataUpdateSetter
        {
            Expression Apply(Expression current);
        }

        private sealed class EntityFrameworkDataUpdateSetter<TProperty> : IEntityFrameworkDataUpdateSetter
        {
            private readonly Expression<Func<TEntity, TProperty>> propertyExpression;
            private readonly TProperty? value;
            private readonly Expression<Func<TEntity, TProperty>>? valueExpression;

            public EntityFrameworkDataUpdateSetter(
                Expression<Func<TEntity, TProperty>> propertyExpression,
                TProperty value)
            {
                this.propertyExpression = propertyExpression;
                this.value = value;
            }

            public EntityFrameworkDataUpdateSetter(
                Expression<Func<TEntity, TProperty>> propertyExpression,
                Expression<Func<TEntity, TProperty>> valueExpression)
            {
                this.propertyExpression = propertyExpression;
                this.valueExpression = valueExpression;
            }

            public Expression Apply(Expression current)
            {
                var method = GetSetPropertyMethod(valueExpression != null).MakeGenericMethod(typeof(TProperty));
                return valueExpression == null
                    ? Expression.Call(current, method, Expression.Quote(propertyExpression), Expression.Constant(value, typeof(TProperty)))
                    : Expression.Call(current, method, Expression.Quote(propertyExpression), Expression.Quote(valueExpression));
            }
        }

        private static MethodInfo GetSetPropertyMethod(bool expressionValue)
        {
            return typeof(SetPropertyCalls<TEntity>)
                .GetMethods()
                .Where(method => method.Name == nameof(SetPropertyCalls<TEntity>.SetProperty) && method.IsGenericMethodDefinition)
                .Single(method =>
                {
                    var parameterType = method.GetParameters()[1].ParameterType;
                    var isExpressionValue = parameterType.IsGenericType
                        && parameterType.GetGenericTypeDefinition() == typeof(Expression<>);
                    return isExpressionValue == expressionValue;
                });
        }
    }
}
