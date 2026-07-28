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
namespace air.cloud.system.model.Dtos.UserDtos
{
    /// <summary>
    /// <para>zh-cn:用户数据传输对象</para>
    /// <para>en-us:User Data Transfer Object</para>
    /// </summary>
    public class UserSDto
    {
        /// <summary>
        /// <para>zh-cn:用户编号</para>
        /// <para>en-us:User Id</para>  
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// <para>zh-cn:应用中的用户编号(管理端新增时不传此字段 修改时请带入详情接口返回的值)</para>
        /// <para>en-us:User Id in Application (Do not pass this field when adding in the management side. Please bring the value returned by the detail interface when modifying)</para>
        /// </summary>
        public string AppUserId { get; set; }

        /// <summary>
        /// <para>zh-cn:用户名</para>
        /// <para>en-us:User Name</para>
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// <para>zh-cn:账户</para>
        /// <para>en-us:Account</para>
        /// </summary>
        public string Account { get; set; }

        /// <summary>
        /// <para>zh-cn:密码</para>
        /// <para>en-us:Password</para>
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// <para>zh-cn:证件号码</para>
        /// <para>en-us:Id Card Number</para>
        /// </summary>
        public string IdCardNo { get; set; }

        /// <summary>
        /// <para>zh-cn:邮件</para>
        /// <para>en-us:Email</para>
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// <para>zh-cn:电话号码</para>
        /// <para>en-us:Phone Number</para>
        /// </summary>
        public string PhoneNumber { get; set; }

        /// <summary>
        /// <para>zh-cn:角色编号集合</para>
        /// <para>en-us:Role Ids Collection</para>
        /// </summary>
        public string RoleIds { get; set; }

        /// <summary>
        /// <para>zh-cn:部门编号集合</para>
        /// <para>en-us:Department Ids Collection</para>
        /// </summary>
        public string DepartmentIds { get; set; }


        /// <summary>
        /// <para>zh-cn:用户职位编号集合</para>
        /// <para>en-us:User Position Ids Collection</para>
        /// </summary>
        public string AssignmentIds { get; set; }


    }
}
