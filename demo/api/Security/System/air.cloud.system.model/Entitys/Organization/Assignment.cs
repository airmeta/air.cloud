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

namespace air.cloud.system.model.Entitys.Organization
{
    /// <summary>
    /// <para>zh-cn:职位模型</para>
    /// <para>en-us:Assignment Model</para>
    /// </summary>
    [Table("ASSIGNMENT")]
    public class Assignment: AllEntityBase
    {
        /// <summary>
        /// <para>zh-cn:职位名称</para>
        /// <para>en-us:Assignment Name</para>
        /// </summary>
        [Column("NAME")]
        public string Name { get; set; }

        /// <summary>
        /// <para>zh-cn:职位描述</para>
        /// <para>en-us:Assignment Description</para>
        /// </summary>
        [Column("DESCRIPTION")]
        public string Description { get; set; }

        /// <summary>
        /// <para>zh-cn:所属部门ID</para>
        /// <para>en-us:Department Id</para>
        /// </summary>
        [Column("DEPARTMENT_ID")]
        public string DepartmentId { get; set; }

        /// <summary>
        /// <para>zh-cn:创建应用ID</para>
        /// <para>en-us:App Id</para>
        /// </summary>
        [Column("APP_ID")]
        public string AppId { get; set; }

    }
}
