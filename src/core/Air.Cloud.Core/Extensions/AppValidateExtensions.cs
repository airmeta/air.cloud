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
namespace Air.Cloud.Core.Extensions
{
    /// <summary>
    /// 验证扩展
    /// </summary>
    public static partial class Extensions
    {
        /// <summary>
        /// 判断指定的序列对象 <paramref name="_this"/> 是否为 Null 或不包含任何元素。
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <param name="_this">被判断的序列 <see cref="IEnumerable"/> 对象。</param>
        /// <returns>如果序列对象 <paramref name="_this"/> 为 Null 或者不包含任何元素，则返回 true；否则返回 false。</returns>
        public static bool IsNullOrEmpty<TSource>(this IEnumerable<TSource> _this)
        {
            return _this == null || _this.Count() == 0;
        }

        /// <summary>
        /// 检测空值,为null则抛出ArgumentNullException异常
        /// </summary>
        /// <param name="obj">对象</param>
        /// <param name="parameterName">参数名</param>
        public static void CheckNull(this object obj, string parameterName)
        {
            if (obj == null)
                throw new ArgumentNullException(parameterName);
        }

        /// <summary>
        /// 是否为空
        /// </summary>
        /// <param name="value">值</param>
        public static bool IsEmpty(this string value)
        {
            return string.IsNullOrWhiteSpace(value);
        }

        /// <summary>
        /// 是否为空
        /// </summary>
        /// <param name="value">值</param>
        public static bool IsEmpty(this Guid? value)
        {
            if (value == null)
                return true;
            return IsEmpty(value.Value);
        }

        /// <summary>
        /// 是否为空
        /// </summary>
        /// <param name="value">值</param>
        public static bool IsEmpty(this Guid value)
        {
            if (value == Guid.Empty)
                return true;
            return false;
        }
    }
}
