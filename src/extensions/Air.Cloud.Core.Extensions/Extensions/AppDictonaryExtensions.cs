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

using System.Reflection;

namespace Air.Cloud.Core.Extensions
{
    public static partial class Extensions
    {
        public static string ToSortUrlParamString<T1, T2>(this IDictionary<T1, T2> dic)
        {
               //var dicSort = from objDic in dic orderby objDic.Value ascending select objDic;
            string str = dic.Aggregate(string.Empty, (current, i) => current + $"{i.Key}={i.Value}&");
            return str.TrimEnd('&');
        }

        /// <summary>
        /// 将对象转成字典
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static IDictionary<string, object> ToDictionary(this object input)
        {
            if (input == null) return default;
            // 如果是字典直接强转
            if (input.GetType().HasImplementedRawGeneric(typeof(IDictionary<,>)))
            {
                var dicInput = (IDictionary<string,object>)input;

                var dic = new Dictionary<string, object>();
                foreach (var key in dicInput.Keys)
                {
                    dic.Add(key.ToString(), dicInput[key]);
                }
                return dic;
            }
            // 如果是对象则反射
            var properties = input.GetType().GetProperties();
            var fields = input.GetType().GetFields();
            var members = properties.Cast<MemberInfo>().Concat(fields.Cast<MemberInfo>());
            return members.ToDictionary(m => m.Name, m => input.GetValue(m));
        }
        /// <summary>
        /// 获取对象中的属性值/字段值
        /// </summary>
        /// <param name="data">对象信息</param>
        /// <param name="m">属性信息/字段信息</param>
        /// <returns>属性值/字段值</returns>
        /// <exception cref="Exception">该member不是属性/字段</exception>
        public static object GetValue(this object data, MemberInfo m)
        {
            object obj=default;
            if (m is PropertyInfo property)obj = property.GetValue(data, null);
            else if (m is FieldInfo field) obj = field.GetValue(data);
            else throw new Exception($"获取对象属性值/字段值时出现异常,属性/字段名称:{m.Name}");
            return obj;
        }
    }
}
