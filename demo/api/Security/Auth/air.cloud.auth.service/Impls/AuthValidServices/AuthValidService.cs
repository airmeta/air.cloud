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
using air.cloud.auth.service.Services.AuthValidServices;
using air.security.common.Dtos.RequestValidDtos;

using Air.Cloud.Core;
using Air.Cloud.Core.Extensions;
using Air.Cloud.Core.Standard.Taxin.Attributes;

using Microsoft.AspNetCore.Mvc;

namespace air.cloud.auth.service.Impls.AuthValidServices
{
    [Route("v1/auth/valid")]
    public  class AuthValidService : IAuthValidService
    {
        [HttpGet("Ticket/{Ticket}/{ClientId}")]
        [TaxinService("skymirrorshield_valid_Ticket")]
        public async Task<RequestValidResult> ValidTicket(string Ticket,string ClientId)
        {
            string ForkStoreTicket = AppRealization.RedisCache.String.Get($"Client:ForkId:{Ticket}");
            string LoginTicket = string.Empty;
            if (!ForkStoreTicket.IsNullOrEmpty())
            {
                LoginTicket = ForkStoreTicket;
            }
            else
            {
                LoginTicket = Ticket;
            }
            string StoreTicket = AppRealization.RedisCache.String.Get($"Client:Id:{LoginTicket}");
            if (LoginTicket != StoreTicket)
            {
                return new RequestValidResult()
                {
                    Valid= false
                };
            }
            return new RequestValidResult()
            {
                Valid = true
            };
        }

    }
}
