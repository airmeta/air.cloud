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
using Air.Cloud.Core.Standard.DataBase.Locators;
using Air.Cloud.Core.Standard.DataBase.Model;

using System.ComponentModel.DataAnnotations.Schema;

namespace air.cloud.security.common.Base
{
    /// <summary>
    /// 自定义实体基类（新增）
    /// </summary>
    public abstract class CreateEntityBase : LocalEntityBase, IEntity<MasterDbContextLocator>
    {
        /// <summary>
        /// 创建者id
        /// </summary>
        [Column("CREATE_USER_ID")]
        public string? CreateUserId { get; set; }
        /// <summary>
        /// 创建者姓名
        /// </summary>

        [Column("CREATE_USER_NAME")]
        public string? CreateUserName { get; set; }
        /// <summary>
        /// 创建时间
        /// </summary>

        [Column("CREATE_TIME")] 
        public DateTime? CreateTime { get; set; }

    }
}
