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
namespace air.cloud.system.model.Dtos.OrganizationDtos.DepartmentDtos
{
    /// <summary>
    /// <para>zh-cn:部门查询树状结果</para>
    /// <para>en-us:Department Query Tree Result</para>
    /// </summary>
    public class DepartmentTreeDto
    {
        /// <summary>
        /// <para>zh-cn:部门Id</para>
        /// <para>en-us:Department Id</para>
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// <para>zh-cn:部门编码</para>
        /// <para>en-us:Department Code</para>
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// <para>zh-cn:部门名称</para>
        /// <para>en-us:Department Name</para>
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// <para>zh-cn:部门描述</para>
        /// <para>en-us:Department Description</para>
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// <para>zh-cn:上级部门Id</para>
        /// <para>en-us:Parent Department Id</para>
        /// </summary>
        public string ParentId { get; set; }

        /// <summary>
        /// <para>zh-cn:应用Id</para>
        /// <para>en-us:App Id</para>
        /// </summary>
        public string AppId { get; set; }

        /// <summary>
        /// <para>zh-cn:子部门列表</para>
        /// <para>en-us:Child Department List</para>
        /// </summary>
        public List<DepartmentTreeDto> Children { get; set; }


    }
}
