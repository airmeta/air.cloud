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
using air.cloud.account.service.Dtos.AccountDtos;

using Air.Cloud.Core.Standard.DynamicServer;

namespace air.cloud.account.service.Services.AccountServices.AccountServices
{
    /// <summary>
    /// <para>zh-cn:账户服务接口</para>
    /// <para>en-us:Account Service Interface</para>
    /// </summary>
    public interface IAccountService:IDynamicService
    {

        /// <summary>
        /// <para>zh-cn:修改密码</para>
        /// <para>en-us:Change Password</para>
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        public Task<bool> ChangePasswordAsync(AccountDto dto);


        public Task<string> ResetPasswordAsync(AccountDto dto);
        

    }
}
