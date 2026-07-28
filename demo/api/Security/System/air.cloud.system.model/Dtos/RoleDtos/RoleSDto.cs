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
namespace air.cloud.system.model.Dtos.RoleDtos
{
    /// <summary>
    /// <para>zh-cn:角色保存模型</para>
    /// <para>en-us:Role Save Model</para>
    /// </summary>
    public class RoleSDto
    {
        /// <summary>
        /// <para>zh-cn:角色标识</para>
        /// <para>en-us:Role Id</para>
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// <para>zh-cn:角色名称</para>
        /// <para>en-us:Role Name</para>
        /// </summary>
        public string RoleName { get; set; }

        /// <summary>
        /// <para>zh-cn:角色描述</para> 
        /// <para>en-us:Role Description</para>
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// <para>zh-cn:应用标识</para>
        /// <para>en-us:Application Id</para>
        /// </summary>
        public string AppId { get; set; }
        /// <summary>
        ///  <para>zh-cn:从指定角色复制菜单权限,动作权限  可空</para>
        ///  <para>en-us:Copy menu permissions and action permissions from the specified role  Optional</para>
        /// </summary>
        public string TargetId { get; set; }

    }
}
