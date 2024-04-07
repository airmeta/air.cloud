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

namespace Air.Cloud.Core.Attributes
{
    /// <summary>
    /// 用于标注当前字段需要记录更改信息
    /// </summary>
    public class NeedEqualAttribute : Attribute
    {
        /// <summary>
        /// 用于标注当前字段需要记录更改信息
        /// </summary>
        /// <param name="propertyName">字段中文标注</param>
        public NeedEqualAttribute(string propertyName)
        {
            PropertyName = propertyName;
        }

        /// <summary>
        /// 用于标注当前字段需要记录更改信息
        /// </summary>
        /// <param name="propertyName">字段中文标注</param>
        /// <param name="datas"></param>
        public NeedEqualAttribute(string propertyName, Enum en)
        {
            Datas = en;
            PropertyName = propertyName;
        }
        public string PropertyName { get; set; }

        public Enum Datas { get; set; }
    }
}
