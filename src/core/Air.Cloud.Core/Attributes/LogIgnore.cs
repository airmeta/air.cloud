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
    /// 不记录审计日志
    /// </summary>
    public class LogIgnore : Attribute
    {
        /// <summary>
        /// 不记录审计日志
        /// </summary>
        /// <param name="propertyValue">字段中文标注</param>
        public LogIgnore(bool? propertyValue)
        {
            if (propertyValue.HasValue)
                PropertyValue = propertyValue.Value;
            else
            {
                PropertyValue = true;
            }
        }
        /// <summary>
        /// 不记录审计日志
        /// </summary>
        public LogIgnore()
        {
            PropertyValue = true;
        }

        public bool PropertyValue { get; set; }
    }
}
