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
using air.cloud.auth.service.Services.AuthStoreServices;
using air.cloud.security.common.Auths;
using air.cloud.security.common.Model;
using air.cloud.security.common.Utils;

using Air.Cloud.Core;
using Air.Cloud.Core.App;
using Air.Cloud.Core.Extensions;
using Air.Cloud.Core.Plugins.Security.MD5;
using Air.Cloud.Core.Standard.DistributedLock.Attributes;
using Air.Cloud.Core.Standard.Taxin.Attributes;

using Microsoft.AspNetCore.Mvc;

namespace air.cloud.auth.service.Impls.AuthStoreServices
{
    [Route("v1/auth/store")]
    public class AuthStoreService : IAuthStoreService
    {
        /// <summary>
        /// <para>zh-cn:未登录的票据载荷标识</para>
        /// <para>en-us:Not logged in ticket payload identification</para>
        /// </summary>
        public const string NOT_LOGIN_PAYLOAD = "NOT_LOGIN";

        private readonly IUserAccountStore userAccount;

        public AuthStoreService(IUserAccountStore userAccount)
        {
            this.userAccount = userAccount;
        }

        [HttpGet("Ticket/temp")]
        [TaxinService("skymirrorshield_create_temp_Ticket")]
        public async Task<TicketCreateResult> CreateTempTicketAsync()
        {
            int StoreSeconds = 30*60*60;
            string Ticket = AppCore.Guid();
            await AppRealization.RedisCache.String.SetAsync($"Client:Id:{Ticket}", Ticket, new TimeSpan(0, 0, StoreSeconds));
            await AppRealization.RedisCache.String.SetAsync($"Client:Token:{Ticket}", NOT_LOGIN_PAYLOAD, new TimeSpan(0, 0, StoreSeconds));
            return new TicketCreateResult
            {
                Ticket = Ticket,
                ExpiredAt = DateTime.Now.AddSeconds(StoreSeconds),
                Payload = null
            };
        }

        [HttpPost("Ticket/create")]
        [TaxinService("skymirrorshield_create_Ticket")]
        public async Task<TicketCreateResult> TicketCreateAsync(UserAccountFactory userAccountFactory)
        {
            int StoreSeconds = 30 * 60 * 60;
            string Ticket = userAccountFactory.Ticket;
            if (Ticket.IsNullOrEmpty())
            {
                Ticket = AppCore.Guid();
                userAccountFactory.Ticket = Ticket;
            }
            userAccountFactory.ExpiredAt = DateTime.Now.AddSeconds(StoreSeconds);
            string Payload = AppRealization.JSON.Serialize(userAccountFactory);
            await AppRealization.RedisCache.String.SetAsync($"Client:Id:{Ticket}", Ticket, new TimeSpan(0, 0, StoreSeconds));
            await AppRealization.RedisCache.String.SetAsync($"Client:Token:{Ticket}", Payload, new TimeSpan(0, 0, StoreSeconds));
            return new TicketCreateResult
            {
                Ticket = Ticket,
                ExpiredAt = DateTime.Now.AddSeconds(StoreSeconds),
                Payload = userAccountFactory.GetAccountPublicPayLoad()
            };
        }

        [HttpGet("Ticket/payload/{Ticket}")]
        [TaxinService("skymirrorshield_get_account_payload")]
        public async Task<UserAccountFactory> GetAccountPayloadInfoAsync(string Ticket)
        {
            return UserAccountFactoryUtil.GetUserAccount(Ticket);
        }

        [HttpGet("Ticket/fork/{Ticket}")]
        [TaxinService("skymirrorshield_fork_Ticket")]
        [DistributedLock]
        public async Task<TicketCreateResult> ForkAppTicketByTicket(string Ticket)
        {
            UserAccountFactory userAccountFactory = await userAccount.GetUserAccountAsync(Ticket);
            if (userAccountFactory == null)
            {
                return new TicketCreateResult
                {
                    Code = 401,
                    Message = "身份认证无效或已过期"
                };
            }
            string AppTicket = AppCore.Guid();
            AppRealization.Lock.TryExecuteWithLock(Ticket, () =>
            {
                var AppIdExpiredAt = userAccountFactory.ExpiredAt - DateTime.Now;

                int Seconds = (int)(AppIdExpiredAt.TotalMilliseconds / 1000);

                AppRealization.RedisCache.String.Set($"Client:ForkId:{AppTicket}", Ticket, new TimeSpan(0, 0, Seconds));

                string ForkIds=AppRealization.RedisCache.String.Get($"Client:ForkIds:{Ticket}");

                if (ForkIds.IsNullOrEmpty())
                {
                    ForkIds = AppTicket;
                }
                else
                {
                    ForkIds = $"{ForkIds},{AppTicket}";
                }
                AppRealization.RedisCache.String.Set($"Client:ForkIds:{Ticket}", ForkIds, new TimeSpan(0, 0, Seconds)); 
            },new TimeSpan(0,0,10));

            return new TicketCreateResult
            {
                Ticket = AppTicket,
                ExpiredAt = userAccountFactory.ExpiredAt,
                Payload = userAccountFactory.GetAccountPublicPayLoad()
            };
        }

        /// <summary>
        /// <para>zh-cn:清理指定票据对应的登录状态。该方法会兼容主票据和应用派生票据，删除主票据、派生票据映射以及账户本地缓存；当派生票据列表不存在时按无派生票据处理，保证退出登录幂等。</para>
        /// <para>en-us:Clears the login state for the specified ticket. The method accepts both primary tickets and application fork tickets, removes primary ticket data, fork-ticket mappings, and local account caches; a missing fork-ticket list is treated as no forks so logout stays idempotent.</para>
        /// </summary>
        /// <param name="Ticket">
        /// <para>zh-cn:需要清理的主票据或应用派生票据。</para>
        /// <para>en-us:The primary ticket or application fork ticket to clear.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:票据清理结果；重复退出或没有派生票据时仍返回成功。</para>
        /// <para>en-us:The ticket clearing result; repeated logout or tickets without forks still return success.</para>
        /// </returns>
        [HttpGet("Ticket/clear/{Ticket}")]
        [TaxinService("skymirrorshield_clear_Ticket")]
        public Task<TicketCreateResult> ClearAppTicketByTicket(string Ticket)
        {
            if (Ticket.IsNullOrEmpty())
            {
                return Task.FromResult(new TicketCreateResult
                {
                    Code = 401,
                    Message = "Ticket不能为空"
                });
            }

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
            //1. 删除ForkId
            string ForkIds = AppRealization.RedisCache.String.Get($"Client:ForkIds:{LoginTicket}");

            string[] ForkIdsArray = ForkIds.IsNullOrEmpty()
                ? Array.Empty<string>()
                : ForkIds.Split(',', StringSplitOptions.RemoveEmptyEntries);

            foreach (var item in ForkIdsArray)
            {
                AppRealization.RedisCache.Key.Delete($"Client:ForkId:{item}");
                AppRealization.RedisCache.Key.Delete($"Cache:Account:{MD5Encryption.GetMd5By32(item)}");
            }
            AppRealization.RedisCache.Key.Delete($"Client:ForkId:{Ticket}");
            AppRealization.RedisCache.Key.Delete($"Client:ForkIds:{LoginTicket}");
            AppRealization.RedisCache.Key.Delete($"Client:Id:{LoginTicket}");
            AppRealization.RedisCache.Key.Delete($"Client:Token:{LoginTicket}");
            AppRealization.RedisCache.Key.Delete($"Client:Id:{Ticket}");
            AppRealization.RedisCache.Key.Delete($"Client:Token:{Ticket}");
            AppRealization.RedisCache.Key.Delete($"Cache:Account:{MD5Encryption.GetMd5By32(LoginTicket)}");
            AppRealization.RedisCache.Key.Delete($"Cache:Account:{MD5Encryption.GetMd5By32(Ticket)}");

            return Task.FromResult(new TicketCreateResult
            {
                Code = 200,
                Message = "Ticket清理成功"
            });
        }
    }
}
