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
using air.cloud.system.open.service.Dtos.OpenAccountDtos;

using Air.Cloud.Core.Standard.DynamicServer;

namespace air.cloud.system.open.service.Services.IOpenAccountServices
{
    public interface IOpenAccountService:IDynamicService,ITransient
    {

        /// <summary>
        /// <para>zh-cn:通过Ticket获取用户账户信息</para>
        /// <para>en-us:Get user account information through Ticket</para>
        /// </summary>
        /// <param name="Ticket">
        ///   <para>zh-cn:用户登录后获取的Ticket</para>
        ///   <para>en-us:Ticket obtained after user login</para>
        /// </param>
        /// <returns>
        ///   <para>zh-cn:用户账户信息</para>
        ///   <para>en-us:User account information</para>
        /// </returns>
        public Task<OpenAccountDetailRDto> GetUserAccountAsync(OpenAccountDetailQDto dto);



    }
}
