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
    /// <para>zh-cn:重置密码数据传输对象</para>
    /// <para>en-us:Reset Password Data Transfer Object</para>
    /// </summary>
    public class ResetPasswordDto
    {
        /// <summary>
        /// <para>zh-cn:用户ID</para>
        /// <para>en-us:User Id</para>
        /// </summary>
        public string UserId { get; set; }
        /// <summary>
        /// <para>zh-cn:验证码</para>
        /// <para>en-us:Verification Code</para>
        /// </summary>
        public string Code { get; set; }
    }
}
