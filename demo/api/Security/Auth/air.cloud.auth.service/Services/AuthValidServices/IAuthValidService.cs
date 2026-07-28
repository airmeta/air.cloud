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
using air.security.common.Dtos.RequestValidDtos;

using Air.Cloud.Core.Standard.DynamicServer;

namespace air.cloud.auth.service.Services.AuthValidServices
{
    public interface IAuthValidService: IDynamicService,ITransient
    {
        /// <summary>
        /// <para>zh-cn:验证票据信息</para>
        /// </summary>
        /// <param name="Ticket"></param>
        /// <returns></returns>
        public Task<RequestValidResult> ValidTicket(string Ticket, string ClientId);

    }
}
