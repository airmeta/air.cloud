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
using air.cloud.security.common.Base;

namespace air.cloud.system.model.Entitys.Users
{
    /// <summary>
    /// <para>zh-cn:用户账户操作日志</para>
    /// <para>en-us:User account operation log</para>
    /// </summary>
    [Table("SYS_USER_ACCOUNT_LOG")]
    public class UserAccountLog : AllEntityBase
    {
        /// <summary>
        /// <para>zh-cn:用户ID</para>
        /// <para>en-us:User Id</para>
        /// </summary>
        [Column("USER_ID")]
        public string UserId { get; set; }

        /// <summary>
        /// <para>zh-cn:类型编码</para>
        /// <para>en-us:Type code</para>
        /// </summary>
        [Column("TYPE_CODE")]
        public string TypeCode { get; set; }

        /// <summary>
        /// <para>zh-cn:扩展字段（TEXT）</para>
        /// <para>en-us:Meta information (TEXT)</para>
        /// </summary>
        [Column("META", TypeName = "TEXT")]
        public string? Meta { get; set; }

        /// <summary>
        /// <para>zh-cn:备注</para>
        /// <para>en-us:Remark</para>
        /// </summary>
        [Column("REMARK")]
        public string? Remark { get; set; }
    }
}