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
using air.cloud.security.common.Enums;

using Air.Cloud.Core.Standard.DataBase.Locators;
using Air.Cloud.Core.Standard.DataBase.Model;

using System.ComponentModel.DataAnnotations.Schema;

namespace air.cloud.security.common.Base
{
    /// <summary>
    /// 自定义实体基类（新增+修改+删除）
    /// </summary>
    public abstract class AllEntityBase : CreateAndUpdateEntityBase, IEntity<MasterDbContextLocator>
    {
        /// <summary>
        /// 删除人Id
        /// </summary>

        [Column("DELETE_USER_ID")]
        public string? DeleteUserId { get; set; }
        /// <summary>
        ///  删除人
        /// </summary>

        [Column("DELETE_USER_NAME")]
        public string? DeleteUserName { get; set; }
        /// <summary>
        /// 是否删除
        /// </summary>

        [Column("IS_DELETE")]
        public IsOrNotEnum IsDelete { get; set; }
        /// <summary>
        /// 删除时间
        /// </summary>

        [Column("DELETE_TIME")]
        public DateTime? DeleteTime { get; set; }
    }
}
