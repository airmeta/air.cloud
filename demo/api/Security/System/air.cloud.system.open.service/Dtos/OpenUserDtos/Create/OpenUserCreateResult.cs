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
namespace air.cloud.system.open.service.Dtos.OpenUserDtos.Create
{
    /// <summary>
    /// <para>zh-cn:开放用户创建结果</para>
    /// <para>en-us:Open User Create Result</para>
    /// </summary>
    public class OpenUserCreateResult
    {
        /// <summary>
        /// <para>zh-cn:是否创建成功</para>
        /// <para>en-us:Is Creation Successful</para>
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// <para>zh-cn:创建的用户ID</para>
        /// <para>en-us:Created User ID</para>  
        /// </summary>
        public string Id { get; set; }
    }
}
