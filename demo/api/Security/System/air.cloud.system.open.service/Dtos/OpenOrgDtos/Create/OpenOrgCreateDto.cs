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
namespace air.cloud.system.open.service.Dtos.OpenOrgDtos.Create
{
    /// <summary>
    /// <para>zh-cn:开放组织创建数据传输对象</para>
    /// <para>en-us:Open Organization Create Data Transfer Object</para>
    /// </summary>
    public class OpenOrgCreateDto
    {
        /// <summary>
        /// <para>zh-cn:部门名称</para>
        /// <para>en-us:Department Name</para>
        /// </summary>
        public string DepartmentName { get; set; }

        /// <summary>
        /// <para>zh-cn:部门描述</para>
        /// <para>en-us:Department Description</para>
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// <para>zh-cn:部门编码</para>
        /// <para>en-us:Department Code</para>
        /// </summary>
        /// <remarks>
        ///   <para>zh-cn:部门编码用于唯一标识一个部门, 通常由字母和数字组成, 例如 "HR001" 代表人力资源部. 全局不重复</para>
        ///   <para>en-us:Department Code is used to uniquely identify a department, typically consisting of letters and numbers, such as "HR001" representing the Human Resources Department. It must be globally unique.</para>
        /// </remarks>
        public string DepartmentCode { get; set; }

        /// <summary>
        /// <para>zh-cn:上级部门ID</para>
        /// <para>en-us:Parent Department ID</para>
        /// </summary>
        /// <remarks>
        ///   <para>zh-cn: parentDepartmentId 用于表示当前部门的上级部门的唯一标识符.如果一个部门没有上级部门, 则该字段的值为 0000000000000000000000000000000.</para>
        ///   <para>en-us: parentDepartmentId is used to indicate the unique identifier of the parent department of the current department. If a department has no parent department, the value of this field is
        /// </remarks>
        public string ParentDepartmentId { get; set; }
        /// <summary>
        /// <para>zh-cn:管理区域 行政区划: 市:4位代码 区:6位代码 街道: 9位代码 社居委: 12位代码</para>
        /// <para>en-us:List of managed regions, separated by commas</para>
        /// </summary>
        public string? ManagedRegions { get; set; }
    }
}
