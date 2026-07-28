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
namespace air.cloud.security.common.Base.Dtos
{
    /// <summary>
    /// <para>zh-cn:基础查询传输对象</para>
    /// <para>en-us:Base Query Data Transfer Object</para>
    /// </summary>
    public class BaseQDto
    {
        /// <summary>
        /// <para>zh-cn:页码</para>
        /// <para>en-us:Page Number</para>  
        /// </summary>
        public int Page { get; set; } = 0;

        /// <summary>
        /// <para>zh-cn:每页数量</para>
        /// <para>en-us:Items Per Page</para>
        /// </summary>
        public int Limit { get; set; } = 0;

        /// <summary>
        /// <para>zh-cn:查询信息</para>
        /// <para>en-us:Query Information</para>    
        /// </summary>
        public string Info { get; set; }

        /// <summary>
        /// <para>zh-cn:应用标识</para>
        /// <para>en-us:Application Identifier</para>
        /// </summary>
        public string AppId { get; set; }


    }
}
