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
using air.cloud.system.model.Entitys.Users;
using air.cloud.security.common.Enums;

namespace air.cloud.system.model.Dtos.UserDtos
{
    public  class UserRDto
    {

        /// <summary>
        /// 用户编号
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// <para>用户名</para>
        /// </summary>
        public string UserName { get; set; }
        /// <summary>
        /// <para>用户标识</para>
        /// </summary>
        public string AppUserId { get; set; }

        /// <summary>
        /// <para>账号</para>
        /// </summary>
        public string Account { get; set; }

        /// <summary>
        /// <para>证件号码</para>
        /// </summary>
        public string IdCardNo { get; set; }

        /// <summary>
        /// <para>邮件</para>
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// <para>电话号码</para>
        /// </summary>
        public string PhoneNumber { get; set; }

        /// <summary>
        /// <para>zh-cn:部门名称</para>
        /// <para>en-us:Department Name</para>
        /// </summary>
        /// <remarks>
        ///  <para>zh-cn:在查询用户列表时,该字段无值,仅用作登陆后的用户信息存储</para>
        ///  <para>en-us:When querying the user list, this field has no value and is only used to store user information after login</para>
        /// </remarks>
        public string DepartmentName { get; set; }

        /// <summary>
        /// <para>zh-cn:部门编号</para>
        /// <para>en-us:Department Id</para>
        /// </summary>
        /// <remarks>
        ///  <para>zh-cn:在查询用户列表时,该字段无值,仅用作登陆后的用户信息存储</para>
        ///  <para>en-us:When querying the user list, this field has no value and is only used to store user information after login</para>
        /// </remarks>
        public string DepartmentId { get; set; }

        /// <summary>
        /// <para>zh-cn:创建账户的应用</para>
        /// <para>en-us:The app that created the account</para>
        /// </summary>
        public string AccountCreateAppId { get; set; }

        /// <summary>
        /// <para>zh-cn:创建用户编号</para>
        /// <para>en-us:Create User Id</para>
        /// </summary>
        public string CreateUserId { get; set; }
        /// <summary>
        /// <para>zh-cn:创建用户名</para>
        /// <para>en-us:Create User Name</para>
        /// </summary>
        public string CreateUserName { get; set; }

        /// <summary>
        /// <para>zh-cn:创建时间</para>
        /// <para>en-us:Create Time</para>
        /// </summary>
        public DateTime? CreateTime { get; set; }

        /// <summary>
        /// <para>zh-cn:最后更新时间</para>
        /// <para>en-us:Last Update Time</para>
        /// </summary>
        public DateTime? UpdateTime { get; set; }


        /// <summary>
        /// 是否三方平台用户(0否 1是)
        /// </summary>
        /// <remarks>
        /// 从统一身份认证平台建立的账户都是 否 外部进来的都是 是
        /// </remarks>
        public IsOrNotEnum IsThirdPlatformUser { get; set; } = IsOrNotEnum.否;

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

        public UserRDto() { }

        public UserRDto(User user)
        {
            Id = user.Id;
            UserName = user.UserName;
            AppUserId = user.AppUserId;
            Account = user.Account;
            IdCardNo = user.IdCardNo;
            Email = user.Email;
            PhoneNumber = user.PhoneNumber;
            AccountCreateAppId = user.AccountCreateAppId;
            IsThirdPlatformUser = user.IsThirdPlatformUser;

        }

    }
}
