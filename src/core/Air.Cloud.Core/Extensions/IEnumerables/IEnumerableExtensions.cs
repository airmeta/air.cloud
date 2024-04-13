/*
 * Copyright (c) 2024 星曳数据
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 *
 * This file is provided under the Mozilla Public License Version 2.0,
 * and the "NO WARRANTY" clause of the MPL is hereby expressly
 * acknowledged.
 */
using Air.Cloud.Core.Extensions.IEnumerables;
using Air.Cloud.Core.Extensions.Linqs;

using System.Linq.Expressions;

namespace Air.Cloud.Core.Extensions.IEnumerables
{
    [IgnoreScanning]
    public static class IEnumerableExtensions
    {
        /// <summary>
        /// 根据条件成立再构建 Where 查询
        /// </summary>
        /// <typeparam name="TSource">泛型类型</typeparam>
        /// <param name="sources">集合对象</param>
        /// <param name="condition">布尔条件</param>
        /// <param name="expression">表达式</param>
        /// <returns>新的集合对象</returns>
        public static IQueryable<TSource> Where<TSource>(this IQueryable<TSource> sources, bool condition, Expression<Func<TSource, bool>> expression)
        {
            return condition ? Queryable.Where(sources, expression) : sources;
        }

        /// <summary>
        /// 根据条件成立再构建 Where 查询，支持索引器
        /// </summary>
        /// <typeparam name="TSource">泛型类型</typeparam>
        /// <param name="sources">集合对象</param>
        /// <param name="condition">布尔条件</param>
        /// <param name="expression">表达式</param>
        /// <returns>新的集合对象</returns>
        public static IQueryable<TSource> Where<TSource>(this IQueryable<TSource> sources, bool condition, Expression<Func<TSource, int, bool>> expression)
        {
            return condition ? Queryable.Where(sources, expression) : sources;
        }

        /// <summary>
        /// 与操作合并多个表达式
        /// </summary>
        /// <typeparam name="TSource">泛型类型</typeparam>
        /// <param name="sources">集合对象</param>
        /// <param name="expressions">表达式数组</param>
        /// <returns>新的集合对象</returns>
        public static IQueryable<TSource> Where<TSource>(this IQueryable<TSource> sources, params Expression<Func<TSource, bool>>[] expressions)
        {
            if (expressions == null || !expressions.Any()) return sources;
            if (expressions.Length == 1) return Queryable.Where(sources, expressions[0]);

            var expression = LinqExpressionExtensions.Or<TSource>();
            foreach (var _expression in expressions)
            {
                expression = expression.Or(_expression);
            }
            return Queryable.Where(sources, expression);
        }

        /// <summary>
        /// 与操作合并多个表达式，支持索引器
        /// </summary>
        /// <typeparam name="TSource">泛型类型</typeparam>
        /// <param name="sources">集合对象</param>
        /// <param name="expressions">表达式数组</param>
        /// <returns>新的集合对象</returns>
        public static IQueryable<TSource> Where<TSource>(this IQueryable<TSource> sources, params Expression<Func<TSource, int, bool>>[] expressions)
        {
            if (expressions == null || !expressions.Any()) return sources;
            if (expressions.Length == 1) return Queryable.Where(sources, expressions[0]);

            var expression = LinqExpressionExtensions.IndexOr<TSource>();
            foreach (var _expression in expressions)
            {
                expression = expression.Or(_expression);
            }
            return Queryable.Where(sources, expression);
        }

        /// <summary>
        /// 根据条件成立再构建 WhereOr 查询
        /// </summary>
        /// <typeparam name="TSource">泛型类型</typeparam>
        /// <param name="sources">集合对象</param>
        /// <param name="conditionExpressions">条件表达式</param>
        /// <returns>新的集合对象</returns>
        public static IQueryable<TSource> Where<TSource>(this IQueryable<TSource> sources, params (bool condition, Expression<Func<TSource, bool>> expression)[] conditionExpressions)
        {
            var expressions = new List<Expression<Func<TSource, bool>>>();
            foreach (var (condition, expression) in conditionExpressions)
            {
                if (condition) expressions.Add(expression);
            }
            return sources.Where(expressions.ToArray());
        }

        /// <summary>
        /// 根据条件成立再构建 WhereOr 查询，支持索引器
        /// </summary>
        /// <typeparam name="TSource">泛型类型</typeparam>
        /// <param name="sources">集合对象</param>
        /// <param name="conditionExpressions">条件表达式</param>
        /// <returns>新的集合对象</returns>
        public static IQueryable<TSource> Where<TSource>(this IQueryable<TSource> sources, params (bool condition, Expression<Func<TSource, int, bool>> expression)[] conditionExpressions)
        {
            var expressions = new List<Expression<Func<TSource, int, bool>>>();
            foreach (var (condition, expression) in conditionExpressions)
            {
                if (condition) expressions.Add(expression);
            }
            return sources.Where(expressions.ToArray());
        }

        /// <summary>
        /// 根据条件成立再构建 Where 查询
        /// </summary>
        /// <typeparam name="TSource">泛型类型</typeparam>
        /// <param name="sources">集合对象</param>
        /// <param name="condition">布尔条件</param>
        /// <param name="expression">表达式</param>
        /// <returns>新的集合对象</returns>
        public static IEnumerable<TSource> Where<TSource>(this IEnumerable<TSource> sources, bool condition, Func<TSource, bool> expression)
        {
            return condition ? sources.Where(expression) : sources;
        }

        /// <summary>
        /// 根据条件成立再构建 Where 查询，支持索引器
        /// </summary>
        /// <typeparam name="TSource">泛型类型</typeparam>
        /// <param name="sources">集合对象</param>
        /// <param name="condition">布尔条件</param>
        /// <param name="expression">表达式</param>
        /// <returns>新的集合对象</returns>
        public static IEnumerable<TSource> Where<TSource>(this IEnumerable<TSource> sources, bool condition, Func<TSource, int, bool> expression)
        {
            return condition ? sources.Where(expression) : sources;
        }
    }
}
