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
    /// <para>用户组实体</para>
    /// </summary>
    [Table("SYS_USER_GROUP")]
    public class UserGroup:AllEntityBase
    {
        /// <summary>
        /// 用户组名称
        /// </summary>
        [Column("GROUP_NAME")]
        public string GroupName { get; set; }

        /// <summary>
        /// 应用程序信息
        /// </summary>
        [Column("APP_ID")]
        public string AppId { get; set; }

        /// <summary>
        /// 描述信息
        /// </summary>
        [Column("DESCRIPTION")]
        public string? Description { get; set; }

    }
}
