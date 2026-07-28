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
    /// <para>zh-cn:账户数据传输对象</para>
    /// <para>en-us:Account Data Transfer Object</para>
    /// </summary>
    public  class AccountDto
    {  
        /// <summary>
        /// <para>zh-cn:登录内容(调用方做加密)</para>
        /// <para>en-us:Login Content (encrypted by caller)</para>
        /// </summary>
        public string Content { get; set; }

    }
}
