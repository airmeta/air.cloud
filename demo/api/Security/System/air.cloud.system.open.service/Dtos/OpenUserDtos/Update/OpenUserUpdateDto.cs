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
namespace air.cloud.system.open.service.Dtos.OpenUserDtos.Update
{
    /// <summary>
    /// <para>zh-cn:开放用户更新数据传输对象</para>
    /// <para>en-us:Open User Update Data Transfer Object</para>
    /// </summary>
    public class OpenUserUpdateDto
    {
        /// <summary>
        /// <para>zh-cn:三方应用中的用户编号</para>
        /// <para>en-us:User Id in Third-Party Application</para>
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
        /// <para>zh-cn:密码(明文)</para>
        /// <para>en-us:Password (Plain Text)</para>
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

    }
}
