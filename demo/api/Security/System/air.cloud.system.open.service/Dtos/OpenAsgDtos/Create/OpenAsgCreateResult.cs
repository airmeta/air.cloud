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
namespace air.cloud.system.open.service.Dtos.OpenAsgDtos.Create
{
    /// <summary>
    /// <para>zh-cn:开放职位创建结果</para>
    /// <para>en-us:Open Assignment Create Result</para>  
    /// </summary>
    public class OpenAsgCreateResult
    {
        /// <summary>
        /// <para>zh-cn:是否创建成功</para>
        /// <para>en-us:Is Creation Successful</para>
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// <para>zh-cn:职位Id</para>
        /// <para>en-us:Assignment Id</para>
        /// </summary>
        public string Id { get; set; }

    }
}
