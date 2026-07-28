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
using air.cloud.security.common.Enums;

using Microsoft.EntityFrameworkCore;

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace air.cloud.system.model.Entitys.Roles
{
    /// <summary>
    /// <para>菜单实体</para>
    /// </summary>
    [Table("SYS_MENU")]
    public  class Menu:AllEntityBase
    {
        #region 字段
        /// <summary>
        /// 菜单分类
        /// </summary>
        [Description("菜单分类")]
        [Column("TYPE")]
        public MenuTypeEnum Type { get; set; } = MenuTypeEnum.菜单;

        /// <summary>
        /// 上级菜单ID
        /// </summary>
        [Column("PARENTID"), Unicode(false), MaxLength(32)]
        public string? ParentId { get; set; }

        /// <summary>
        /// 菜单名
        /// </summary>
        [Description("菜单名")]
        [Column("TITLE"), Unicode(false), MaxLength(30)]
        public string Title { get; set; }

        /// <summary>
        /// 图标
        /// </summary>
        [Description("图标")]
        [Column("ICON"), MaxLength(50), Unicode(false)]
        public string Icon { get; set; }

        /// <summary>
        /// 前端路由地址
        /// </summary>
        [Description("路由地址")]
        [Column("PATH"), Unicode(false), MaxLength(100)]
        public string Path { get; set; }

        /// <summary>
        /// 前端组件地址
        /// </summary>
        [Description("组件地址")]
        [Column("COMPONENT"), Unicode(false), MaxLength(100)]
        public string? Component { get; set; }

        /// <summary>
        /// 是否隐藏（1是，0否）
        /// </summary>
        [Description("是否隐藏")]
        [Column("HIDE")]
        public IsOrNotEnum Hide { get; set; }

        /// <summary>
        /// 排序号
        /// </summary>
        [Description("排序")]
        [Column("SORT_NUMBER")]
        public int? SortNumber { get; set; }


        /// <summary>
        /// 权限信息
        /// </summary>
        [Column("AUTHORITY")]
        [Description("权限信息")]
        public string? Authority { get; set; }


        /// <summary>
        /// 应用程序信息
        /// </summary>

        [Column("APP_ID")]
        public string AppId { get; set; }
        #endregion
    }

    
}
