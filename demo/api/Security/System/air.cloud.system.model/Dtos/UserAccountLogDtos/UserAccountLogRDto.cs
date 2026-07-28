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
namespace air.cloud.system.model.Dtos.UserAccountLogDtos
{
    /// <summary>
    /// <para>zh-cn:用户账户日志返回传输对象</para>
    /// <para>en-us:User account log response DTO</para>
    /// </summary>
    public class UserAccountLogRDto
    {
        /// <summary>
        /// <para>zh-cn:日志ID</para>
        /// <para>en-us:Log Id</para>
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// <para>zh-cn:用户ID</para>
        /// <para>en-us:User Id</para>
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// <para>zh-cn:类型编码</para>
        /// <para>en-us:Type code</para>
        /// </summary>
        public string TypeCode { get; set; }

        /// <summary>
        /// <para>zh-cn:扩展字段（与数据库TEXT对应）</para>
        /// <para>en-us:Meta info (mapped to TEXT in DB)</para>
        /// </summary>
        public string? Meta { get; set; }

        /// <summary>
        /// <para>zh-cn:备注</para>
        /// <para>en-us:Remark</para>
        /// </summary>
        public string? Remark { get; set; }

        /// <summary>
        /// <para>zh-cn:创建时间</para> 
        /// <para>en-us:Create time</para>
        /// </summary>
        public DateTime? CreateTime { get; set; }
    }
}