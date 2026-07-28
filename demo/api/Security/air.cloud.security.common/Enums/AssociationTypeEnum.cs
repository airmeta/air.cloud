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
namespace air.cloud.security.common.Enums
{
    /// <summary>
    /// 实体映射关系类型枚举
    /// </summary>
    public enum  AssociationTypeEnum
    {
            用户与用户组,
            用户与角色组,
            用户与角色,
            用户与部门,
            用户与任职,

            用户组与角色组,
            用户组与角色,

            角色与角色组,
            角色与菜单权限,
            角色与动作权限,
            角色与应用,



            部门与区域,
            部门与应用,
    }
}
