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
namespace air.cloud.system.open.service.Dtos.OpenAsgDtos.Create
{
    /// <summary>
    /// <para>zh-cn:开放职位创建数据传输对象</para>
    /// <para>en-us:Open Assignment Create Data Transfer Object</para>
    /// </summary>
    public class OpenAsgCreateDto
    {
        /// <summary>
        /// <para>zh-cn:职位名称</para>
        /// <para>en-us:Assignment Name</para>
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// <para>zh-cn:职位描述</para>
        /// <para>en-us:Assignment Description</para>
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// <para>zh-cn:所属部门ID</para>
        /// <para>en-us:Department Id</para>
        /// </summary>
        public string DepartmentId { get; set; }

    }
}
