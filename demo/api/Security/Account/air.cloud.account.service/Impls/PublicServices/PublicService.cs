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
using air.cloud.account.service.Dtos.PublicDtos;
using air.cloud.account.service.Services.PublicServices;
using air.cloud.account.service.Utils;
using air.cloud.security.common.Auths;
using air.cloud.security.common.Extensions;
using air.cloud.security.common.Model;
using air.cloud.system.model.Domains.AppInfoDomains;
using air.cloud.system.model.Entitys.Apps;

using Air.Cloud.Core;
using Air.Cloud.Core.App;
using Air.Cloud.Core.Extensions;
using Air.Cloud.WebApp.FriendlyException;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace air.cloud.account.service.Impls.PublicServices
{
    [Route("v1/pub")]
    public class PublicService : IPublicService
    {
        private readonly IAppInfoDomain appInfoDomain;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IUserAccountStore userAccountStore;

        /// <summary>
        /// <para>zh-cn:登录页路径</para>
        /// <para>en-us:Login page path</para>
        /// </summary>
        private string LoginPath => AppCore.Configuration["AppSettings:Client:LoginPath"];

        public PublicService(IAppInfoDomain appInfoDomain,IHttpContextAccessor httpContextAccessor,IUserAccountStore userAccountStore)
        {
            this.appInfoDomain = appInfoDomain; 
            this.httpContextAccessor = httpContextAccessor;
            this.userAccountStore = userAccountStore;
        }
        [HttpGet("init")]
        [HttpGet("init/{AppId}")]
        [AllowAnonymous]
        public async Task<object> InitAppStatusAsync(string AppId=null)
        {
            var HasApp = await appInfoDomain.HasAppAsync();
            string InitAppSecret = string.Empty;
            TicketCreateResult Ticket =await userAccountStore.GetTemporaryTicketAsync();
            if (!HasApp)
            {
                using (RedisLockHandler handler = new RedisLockHandler(new TimeSpan(0, 0, 3), "InitDefaultApp"))
                {
                    AppRealization.RedisCache.String.Set($"InitAppSecret", Ticket.Ticket);
                    AppRealization.Output.Print("应用程序初始化密钥", Ticket.Ticket, Air.Cloud.Core.Modules.AppPrint.AppPrintLevel.Information);
                }
                return new AppStatusDto
                {
                    HasApp = false,
                    AppName = "",
                    AppId = "",
                    Client=new ClientConfig()
                    {
                        Ticket = Ticket.Ticket,
                        ExpiredSeconds = (int)((Ticket.ExpiredAt - DateTime.Now).TotalSeconds),
                    },
                    LoginPath= LoginPath
                };
            }
            AppInformation App= AppId.IsNullOrEmpty()? await appInfoDomain.GetFirstAppAsync():await appInfoDomain.GetAppInfoAsync(AppId);
            return new AppStatusDto
            {
                HasApp=true,
                AppName = App.AppName,
                AppId = App.Id,
                Client = new ClientConfig
                {
                    Ticket = Ticket.Ticket,
                    ExpiredSeconds = (int)((Ticket.ExpiredAt - DateTime.Now).TotalSeconds),
                    AppEntryptType=App.AppEncryptType,
                    PrivateKey = App.PrivateKey,
                    EnableMFA=App.EnableMFA
                },
                LoginPath= LoginPath
            };
        }


        [HttpGet("captcha")]
        [AllowAnonymous]
        public async Task<object> GetCaptchaCodeAsync()
        {
            string UKey = httpContextAccessor.HttpContext?.Request.Headers["Ticket"];
            if (UKey.IsNullOrEmpty()) throw Oops.Oh("未鉴别的客户端信息");

            //这里不需要鉴别用户是否登录 只需要鉴别到票据是否为我方客户端颁发
            _= await userAccountStore.GetUserAccountAsync(UKey, true);

            return new
            {
                CaptchaBase64 = CaptchaCodeUtil.GenerateCaptchaCode(UKey)
            };
        }

    }
}
