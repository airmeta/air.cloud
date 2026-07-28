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
using air.cloud.security.common.Exceptions;
using air.cloud.security.common.Model;
using air.cloud.security.common.Model.AppClientInfos;

using Air.Cloud.Core;
using Air.Cloud.Core.Extensions;
using Air.Cloud.Core.Plugins.Security.MD5;
using Air.Cloud.Core.Standard.Store;
using Air.Cloud.Core.Standard.Taxin.Client;
using Air.Cloud.WebApp.UnifyResult.Internal;

using Microsoft.AspNetCore.Http;

namespace air.cloud.security.common.Auths
{
    public class UserAccountStore : IUserAccountStore
    {
        /// <summary>
        /// <para>zh-cn:未登录的票据载荷标识</para>
        /// <para>en-us:Not logged in ticket payload identification</para>
        /// </summary>
        public const string NOT_LOGIN_PAYLOAD = "NOT_LOGIN";

        private readonly IHttpContextAccessor httpContextAccessor;

        private readonly ITaxinClientStandard taxinClientStandard;

        public UserAccountStore(IHttpContextAccessor httpContextAccessor,
            ITaxinClientStandard taxinClientStandard)
        {
            this.httpContextAccessor = httpContextAccessor;
            this.taxinClientStandard = taxinClientStandard;
        }

        /// <summary>
        /// <para>zh-cn:获取未登录的临时票据</para>
        /// <para>en-us:Get a temporary ticket for not logged in</para>
        /// </summary>
        /// <returns>
        /// <para>zh-cn:由认证服务创建、状态码为 200 的临时票据。</para>
        /// <para>en-us:A temporary ticket created by the authentication service with status code 200.</para>
        /// </returns>
        public async Task<TicketCreateResult> GetTemporaryTicketAsync()
        {
            var result = await SendTaxinAsync<TicketCreateResult>("skymirrorshield_create_temp_Ticket");
            if (result?.Code == 200)
            {
                return result;
            }
            throw new AuthException("Temp_Ticket_Create_Faild", result?.Message ?? "身份验证失败,暂时无法登录系统");
        }
        /// <summary>
        /// <para>zh-cn:从当前请求的 Ticket 请求头读取并获取账户载荷。未携带 Ticket 时不发起远程调用，直接返回认证异常。</para>
        /// <para>en-us:Reads the Ticket header from the current request and retrieves its account payload. When the header is absent, it raises an authentication exception without a remote call.</para>
        /// </summary>
        /// <returns>
        /// <para>zh-cn:当前票据对应的账户载荷。</para>
        /// <para>en-us:The account payload associated with the current ticket.</para>
        /// </returns>
        public async Task<UserAccountFactory> GetUserAccountAsync()
        {
            string Ticket = httpContextAccessor.HttpContext.Request.Headers[IAppClientInfo.CLIENT_Ticket_HEADER];
            if (Ticket == null) throw new AuthException("Ticket_Payload_Faild", "Ticket负载验证不合法");
            return await GetUserAccountAsync(Ticket);
        }
        /// <summary>
        /// <para>zh-cn:通过 Taxin 获取指定票据的账户载荷，并在剩余有效期超过一分钟时写入本地 Redis 缓存。默认拒绝空载荷；允许跳过临时票据校验时，临时票据可返回空载荷。</para>
        /// <para>en-us:Retrieves the account payload for a ticket through Taxin and caches it in local Redis when more than one minute remains. Empty payloads are rejected by default; a temporary ticket may return an empty payload when its check is skipped.</para>
        /// </summary>
        /// <param name="Ticket">
        /// <para>zh-cn:由认证服务颁发的票据。</para>
        /// <para>en-us:The ticket issued by the authentication service.</para>
        /// </param>
        /// <param name="skipTemporaryTicket">
        /// <para>zh-cn:是否允许临时票据返回空账户载荷，默认 false。</para>
        /// <para>en-us:Whether a temporary ticket may return an empty account payload; default is false.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:票据对应的账户载荷。</para>
        /// <para>en-us:The account payload associated with the ticket.</para>
        /// </returns>
        public async Task<UserAccountFactory> GetUserAccountAsync(string Ticket, bool skipTemporaryTicket = false)
        {
            var userAccountFactory = AppRealization.RedisCache.String.Get<UserAccountFactory>($"Cache:Account:{MD5Encryption.GetMd5By32(Ticket)}");
            if (userAccountFactory != null) return userAccountFactory;

            var accountInfo = await SendTaxinAsync<UserAccountFactory>("skymirrorshield_get_account_payload", new
            {
                Ticket = Ticket,
            });
            if (accountInfo == null)
            {
                if (skipTemporaryTicket)
                {
                    return null;
                }
                throw new AuthException("Ticket_Payload_Faild", "票据负载读取失败");
            }
            TimeSpan span = accountInfo.ExpiredAt - DateTime.Now;
            int CacheSeconds = (int)span.TotalSeconds - 60;
            if (CacheSeconds > 10)
            {
                await AppRealization.RedisCache.String.SetAsync($"Cache:Account:{MD5Encryption.GetMd5By32(Ticket)}", accountInfo, new TimeSpan(0, 0, CacheSeconds - 5));
            }
            return accountInfo;
        }
        /// <summary>
        /// <para>zh-cn:通过 Taxin 将账户载荷写入认证服务并创建或更新登录票据。远端调用未返回成功票据时抛出认证异常。</para>
        /// <para>en-us:Writes the account payload to the auth service through Taxin and creates or updates the login ticket. An authentication exception is raised when the remote call does not return a successful ticket.</para>
        /// </summary>
        /// <param name="userAccountFactory">
        /// <para>zh-cn:要写入票据的账户载荷。</para>
        /// <para>en-us:The account payload to store in the ticket.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:认证服务返回的登录票据。</para>
        /// <para>en-us:The login ticket returned by the authentication service.</para>
        /// </returns>
        public async Task<TicketCreateResult> SetTicketPayLoadContentAsync(UserAccountFactory userAccountFactory)
        {
            var result = await SendTaxinAsync<TicketCreateResult>("skymirrorshield_create_Ticket", userAccountFactory);
            if (result?.Code == 200)
            {
                return result;
            }
            throw new AuthException("Temp_Ticket_Create_Faild", result?.Message ?? "票据存储失败,暂时无法登录系统");
        }

        /// <summary>
        /// <para>zh-cn:通过 Taxin 为现有登录票据创建应用分叉票据。远端调用失败或返回非成功状态时抛出认证异常。</para>
        /// <para>en-us:Creates an application fork ticket for an existing login ticket through Taxin. An authentication exception is raised when the remote call fails or returns a non-success status.</para>
        /// </summary>
        /// <param name="Ticket">
        /// <para>zh-cn:待分叉的登录票据。</para>
        /// <para>en-us:The login ticket to fork.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:认证服务返回的应用分叉票据。</para>
        /// <para>en-us:The application fork ticket returned by the authentication service.</para>
        /// </returns>
        public async Task<TicketCreateResult> GetForkTicketAsync(string Ticket)
        {
            var result = await SendTaxinAsync<TicketCreateResult>("skymirrorshield_fork_Ticket", new
            {
                Ticket = Ticket
            });
            if (result?.Code == 200)
            {
                return result;
            }
            throw new AuthException("Temp_Ticket_Create_Faild", result?.Message ?? "票据存储失败,暂时无法登录系统");
        }

        public async Task<bool> LogOutUserAccountAsync(string Ticket)
        {
            var result = await SendTaxinAsync<TicketCreateResult>("skymirrorshield_clear_Ticket", new
            {
                Ticket = Ticket
            });
            if (result == null)
            {
                throw new AuthException("Temp_Ticket_Clear_Faild", "认证服务未返回票据清理结果");
            }
            return result.Code==200;
        }

        private async Task<TResult> SendTaxinAsync<TResult>(string route, object data = null) where TResult : class
        {
            var response = await taxinClientStandard.SendAsync<RESTfulResult<TResult>>(route, data);
            if (response?.Code == 200)
            {
                return response.Data;
            }

            string detail = response?.Errors?.ToString();
            if (string.IsNullOrWhiteSpace(detail))
            {
                detail = response?.Message;
            }
            if (string.IsNullOrWhiteSpace(detail))
            {
                detail = response == null ? "认证服务未返回响应" : $"认证服务返回状态码:{response.Code?.ToString() ?? "null"}";
            }
            throw new AuthException("Taxin_Request_Faild", $"Taxin请求失败:{detail}");
        }
    }
}
