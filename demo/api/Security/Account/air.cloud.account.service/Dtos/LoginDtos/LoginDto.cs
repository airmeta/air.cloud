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
namespace air.cloud.account.service.Dtos.LoginDtos
{
    /// <summary>
    /// <para>zh-cn:登录传输对象</para>
    /// <para>en-us:Login Data Transfer Object</para>
    /// </summary>
    public class LoginDto
    {
        /// <summary>
        /// <para>zh-cn:登录内容(调用方做加密)</para>
        /// <para>en-us:Login Content (encrypted by caller)</para>
        /// </summary>
        public string Content { get; set; }

    }

    public class LoginPayloadDto
    {
        /// <summary>
        /// 用户账号
        /// </summary>
        public string Account { get; set; }
        /// <summary>
        /// 用户密码
        /// </summary>
        public string Password { get; set; }
        /// <summary>
        /// 验证码
        /// </summary>
        public string Code { get; set; }
    }
}
