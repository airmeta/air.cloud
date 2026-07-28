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
using air.cloud.account.service.Dtos.LoginDtos;
using air.cloud.system.model.Dtos.AccountDtos;

using Air.Cloud.Core.Standard.DynamicServer;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace air.cloud.account.service.Services.AccountServices.AccountLoginServices
{
    /// <summary>
    /// <para>zh-cn:账户登录服务</para>
    /// <para>en-us:Account Login Service</para>
    /// </summary>
    public interface IAccountLoginService: IDynamicService
    {
        /// <summary>
        /// <para>zh-cn:账户登录</para>
        /// <para>en-us:Account Login</para>
        /// </summary>
        /// <param name="Payload"></param>
        /// <returns></returns>
        Task<AccountLoginRDto> Login(LoginDto Payload);
        /// <summary>
        /// <para>zh-cn:账户登出</para>
        /// <para>en-us:Account Logout</para>
        /// </summary>
        /// <param name="Ticket"></param>
        /// <returns></returns>
        Task<AccountLoginOutRDto> LoginOutAsync();
        /// <summary>
        /// <para>zh-cn:查询账户可用的应用列表</para>
        /// <para>en-us:Query the list of applications available to the account</para>
        /// </summary>
        /// <returns></returns>
        Task<IList<AccountAppIdsRDto>> QueryAccountAppAsync();

        /// <summary>
        /// <para>zh-cn:跳转到三方应用时 自动登录</para>
        /// </summary>
        /// <param name="RandomKey"></param>
        /// <returns></returns>
        Task<object> AutoLogin(string RandomKey);

    }
}
