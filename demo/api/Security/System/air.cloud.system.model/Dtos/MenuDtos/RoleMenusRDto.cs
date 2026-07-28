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
namespace air.cloud.system.model.Dtos.MenuDtos
{
    /// <summary>
    /// <para>zh-cn:角色菜单返回DTO</para>
    /// <para>en-us:Role Menu Return DTO</para>
    /// </summary>
    public class RoleMenusRDto:MenuSDto
    {
        /// <summary>
        /// <para>zh-cn:菜单是否选中</para>
        /// <para>en-us:Is the menu selected</para>
        /// </summary>
        public bool Checked { get; set; }
    }
}
