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
namespace air.cloud.system.open.service.Dtos.OpenUserDtos.Check
{
    /// <summary>
    /// <para>zh-cn:开放用户检查结果</para>
    /// <para>en-us:Open User Check Result</para>
    /// </summary>
    public class OpenUserCheckResult
    {
        /// <summary>
        /// <para>zh-cn:是否存在</para>
        /// <para>en-us:Is Exits</para>
        /// </summary>
        public bool IsExits { get; set; }

        /// <summary>
        /// <para>zh-cn:检查结果代码</para>   
        /// <para>en-us:Check Result Code</para>
        /// </summary>
        public string Code { get; set; }

    }
}
