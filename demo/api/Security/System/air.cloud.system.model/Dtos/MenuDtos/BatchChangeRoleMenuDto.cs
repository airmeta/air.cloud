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
    /// <para>zh-cn:批量修改角色菜单</para>
    /// <para>en-us:Batch Change Role Menu</para>
    /// </summary>
    public class BatchChangeRoleMenuDto
    {
        /// <summary>
        /// <para>zh-cn:角色Id</para>
        /// <para>en-us:Role Id</para>
        /// </summary>
        public string RoleId { get; set; }

        /// <summary>
        /// <para>zh-cn:菜单Id列表</para>
        /// <para>en-us:Menu Id List</para>
        /// </summary>
        public IList<string> MenuIds { get; set; }

    }
}
