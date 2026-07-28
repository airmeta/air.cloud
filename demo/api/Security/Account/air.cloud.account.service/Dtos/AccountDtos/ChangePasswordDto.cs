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
namespace air.cloud.account.service.Dtos.AccountDtos
{
    /// <summary>
    /// <para>zh-cn:修改密码传输对象</para>
    /// <para>en-us:Change password DTO</para>
    /// </summary>
    public class ChangePasswordDto
    {
        /// <summary>
        /// <para>zh-cn:用户ID</para>
        /// <para>en-us:User Id</para>
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// <para>zh-cn:旧密码</para>
        /// <para>en-us:Old Password</para>
        /// </summary>
        public string OldPassword { get; set; }
        /// <summary>
        /// <para>zh-cn:新密码</para>  
        /// <para>en-us:New Password</para>
        /// </summary>
        public string NewPassword { get; set; }

        /// <summary>
        /// <para>zh-cn:确认新密码</para>
        /// <para>en-us:Confirm New Password</para>
        /// </summary>
        public string ConfirmNewPassword { get; set; }


        /// <summary>
        /// <para>zh-cn:验证码</para>
        /// <para>en-us:Code</para>
        /// </summary>
        public string Code { get; set; }


    }
}
