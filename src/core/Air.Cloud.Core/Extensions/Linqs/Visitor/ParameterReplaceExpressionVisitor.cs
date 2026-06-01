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
using System.Linq.Expressions;

namespace Air.Cloud.Core.Extensions.Linqs.Visitor
{
    /// <summary>
    /// <para>zh-cn:表达式参数替换访问器，用于组合 Lambda 表达式时统一参数引用。</para>
    /// <para>en-us:Expression visitor that replaces parameters so composed lambda expressions share the same parameter references.</para>
    /// </summary>
    public class ParameterReplaceExpressionVisitor : ExpressionVisitor
    {
        /// <summary>
        /// 参数表达式映射集合
        /// </summary>
        private readonly Dictionary<ParameterExpression, ParameterExpression> parameterExpressionSetter;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="parameterExpressionSetter">参数表达式映射集合</param>
        public ParameterReplaceExpressionVisitor(Dictionary<ParameterExpression, ParameterExpression> parameterExpressionSetter)
        {
            this.parameterExpressionSetter = parameterExpressionSetter ?? new Dictionary<ParameterExpression, ParameterExpression>();
        }

        /// <summary>
        /// 替换表达式参数
        /// </summary>
        /// <param name="parameterExpressionSetter">参数表达式映射集合</param>
        /// <param name="expression">表达式</param>
        /// <returns>新的表达式</returns>
        public static Expression ReplaceParameters(Dictionary<ParameterExpression, ParameterExpression> parameterExpressionSetter, Expression expression)
        {
            return new ParameterReplaceExpressionVisitor(parameterExpressionSetter).Visit(expression);
        }

        /// <summary>
        /// 重写基类参数访问器
        /// </summary>
        /// <param name="parameterExpression"></param>
        /// <returns></returns>
        protected override Expression VisitParameter(ParameterExpression parameterExpression)
        {
            if (parameterExpressionSetter.TryGetValue(parameterExpression, out var replacement))
            {
                parameterExpression = replacement;
            }

            return base.VisitParameter(parameterExpression);
        }
    }
}
