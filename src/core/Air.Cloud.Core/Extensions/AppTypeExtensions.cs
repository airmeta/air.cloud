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
    public static partial class Extensions
    {
        /// <summary>
        /// 判断指定类型是否为数值类型
        /// </summary>
        /// <param name="_this">要检查的类型</param>
        /// <returns>是否是数值类型</returns>
        public static bool IsNumeric(this Type _this)
        {
            return _this == typeof(Byte)
                || _this == typeof(Int16)
                || _this == typeof(Int32)
                || _this == typeof(Int64)
                || _this == typeof(SByte)
                || _this == typeof(UInt16)
                || _this == typeof(UInt32)
                || _this == typeof(UInt64)
                || _this == typeof(double)
                || _this == typeof(Single);
        }
    }
}
