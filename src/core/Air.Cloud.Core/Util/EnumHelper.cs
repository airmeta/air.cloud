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
using System.ComponentModel;

namespace Air.Cloud.Core.Util
{
    public class EnumHelper
    {
        /// <summary>
        /// 获取枚举描述
        /// </summary>
        /// <param name="type">枚举类型</param>
        /// <param name="value">枚举名称</param>
        /// <returns></returns>
        public static string GetEnumDescription(Type type, string value)
        {
            var field = type.GetField(value);
            if (field == null)
                return "";
            var desc = Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) as DescriptionAttribute;
            return desc != null ? desc.Description : "";
        }

        /// <summary>
        /// 获取枚举描述
        /// </summary>
        /// <param name="type">枚举类型</param>
        /// <param name="value">枚举hasecode</param>
        /// <returns></returns>
        public static string GetEnumDescription(Type type, int value)
        {
            var name = Enum.GetName(type, value);
            if (name == null)
                return "";
            var field = type.GetField(name);
            if (field == null)
                return "";
            var desc = Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) as DescriptionAttribute;
            return desc != null ? desc.Description : name;
        }
    }
}
