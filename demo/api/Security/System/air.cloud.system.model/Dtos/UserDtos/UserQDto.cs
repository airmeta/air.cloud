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
using air.cloud.security.common.Base.Dtos;

namespace air.cloud.system.model.Dtos.UserDtos
{
    /// <summary>
    /// <para>zh-cn:用户查询传输对象</para>
    /// <para>en-us:User Query Dto</para>
    /// </summary>
    public class UserQDto:BaseQDto
    {
        /// <summary>
        /// <para>zh-cn:部门编号</para>
        /// <para>en-us:Department Id</para>
        /// </summary>
        public string? DepartmentId { get; set; }

        /// <summary>
        /// <para>zh-cn:角色编号</para>
        /// <para>en-us:Role Id</para>
        /// </summary>
        public string? RoleId { get; set; }

    }
}
