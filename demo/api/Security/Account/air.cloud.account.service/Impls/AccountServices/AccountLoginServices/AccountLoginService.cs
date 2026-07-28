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
using air.cloud.account.service.Dtos.PublicDtos;
using air.cloud.account.service.Impls.PublicServices;
using air.cloud.account.service.Services.AccountServices.AccountLoginServices;
using air.cloud.security.common.Auths;
using air.cloud.security.common.Enums;
using air.cloud.security.common.Exceptions;
using air.cloud.security.common.Model;
using air.cloud.security.common.Model.AppClientInfos;
using air.cloud.system.model.Domains.AppInfoDomains;
using air.cloud.system.model.Domains.EntityAssociationDomains;
using air.cloud.system.model.Domains.OrganizationDomains;
using air.cloud.system.model.Domains.UserDomains;
using air.cloud.system.model.Dtos.AccountDtos;
using air.cloud.system.model.Dtos.OrganizationDtos.AssignmentDtos;
using air.cloud.system.model.Dtos.UserAccountLogDtos;
using air.cloud.system.model.Entitys.Apps;
using air.cloud.system.model.Entitys.Organization;

using Air.Cloud.Core;
using Air.Cloud.Core.App;
using Air.Cloud.Core.Extensions;
using Air.Cloud.Core.Plugins.Security.MD5;
using Air.Cloud.WebApp.FriendlyException;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using static air.cloud.security.common.Model.UserAccountFactory;

using RouteAttribute = Microsoft.AspNetCore.Mvc.RouteAttribute;
namespace air.cloud.account.service.Impls.AccountServices.AccountLoginServices
{
    [Route("v1/identity")]
    public class AccountLoginService : IAccountLoginService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IAppInfoDomain appInfoDomain;
        private readonly IUserDomain userDomain;
        private readonly IDepartmentDomain departmentDomain;
        private readonly IAssignmentDomain assignmentDomain;
        private readonly IUserAccountStore userAccountStore;
        private readonly IUserAccountLogDomain userAccountLogDomain;
        public AccountLoginService(IHttpContextAccessor httpContextAccessor,
            IEntityAssociationDomain entityAssociationDomain,
            IAppInfoDomain appInfoDomain,
            IDepartmentDomain departmentDomain,
            IAssignmentDomain assignmentDomain,
            IUserDomain userDomain,
            IUserAccountLogDomain userAccountLogDomain,
            IUserAccountStore userAccountStore)
        {
            this._httpContextAccessor = httpContextAccessor;
            this.userDomain = userDomain;
            this.appInfoDomain = appInfoDomain;
            this.departmentDomain = departmentDomain;
            this.userAccountStore = userAccountStore;
            this.assignmentDomain = assignmentDomain;
            this.userAccountLogDomain = userAccountLogDomain;
        }

        /// <summary>
        /// 第三步: 查询用户关联应用列表
        /// </summary>
        /// <param name="Ticket"></param>
        /// <returns></returns>
        [HttpGet("account/apps")]
        public async Task<IList<AccountAppIdsRDto>> QueryAccountAppAsync()
        {
            UserAccountFactory userAccountFactory = await userAccountStore.GetUserAccountAsync();
            //用户关联应用信息
            IList<AccountAppIdsRDto> appIds = await appInfoDomain.GetUserAccountAppIdsAsync(userAccountFactory.Id);
            return appIds;
        }

        /// <summary>
        /// 第二步: 登录  
        /// </summary>
        /// <param name="Payload"></param>
        /// <returns></returns>
        /// <remarks>
        ///  第一步: <see cref="PublicService.InitAppStatusAsync"/>
        /// </remarks>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<AccountLoginRDto> Login(LoginDto Payload)
        {
            string AppId = _httpContextAccessor.HttpContext.Request.Headers["AppId"];

            if (AppId.IsNullOrEmpty()) throw Oops.Oh("客户端非法请求");

            if (!(await appInfoDomain.HasAppAsync())) throw Oops.Oh("平台未初始化,请执行初始化步骤");

            var App = await appInfoDomain.GetAppInfoAsync(AppId);
            string PayLoadContent = App.Decrypt(Payload.Content);
            if (string.IsNullOrEmpty(PayLoadContent)) throw Oops.Oh("请求数据解析失败");
            LoginPayloadDto payloadDto = AppRealization.JSON.Deserialize<LoginPayloadDto>(PayLoadContent);

            //校验验证码是否正确
            var user = await userDomain.GetUserAsync(payloadDto.Account);
            if (user == null) throw Oops.Oh("用户名不存在或密码验证失败");
            if (user.Password != MD5Encryption.GetMd5By32(payloadDto.Password + user.AccountCerdictKey)) throw Oops.Oh("用户名不存在或密码验证失败");
            DateTime ExpiredAt = DateTime.Now.AddSeconds(30 * 60);

            IList<Department> departments = await departmentDomain.GetUserDepartmentsAsync(user.Id);

            IList<AssignmentSDto> assignments = await assignmentDomain.GetUserAssignmentsAsync(user.Id);


            UserAccountFactory account = new UserAccountFactory()
            {
                LoginAppId = App.AppId,
                Id = user.Id,
                UserName = user.UserName ?? string.Empty,
                IdCardNo = App.Encrypt(user.IdCardNo ?? string.Empty),
                PhoneNumber = App.Encrypt(user.PhoneNumber ?? string.Empty),
                Email = user.Email ?? string.Empty,
                Ticket = AppCore.Guid(),
                ExpiredAt = ExpiredAt,
                EntryptType = App.AppEncryptType,
                PrivateKey = App.PrivateKey,
                Account = user.Account,
                AppUserId = user.AppUserId,
                Departments = departments.Select(d => new DepartmentsInfo()
                {
                    DepartmentId = d.Id,
                    DepartmentName = d.DepartmentName,
                }).ToList(),
                Assignments= assignments.Select(a => new AssignmentsInfo()
                {
                    AssignmentId = a.Id,
                    AssignmentName = a.Name,
                    DepartmentId = a.DepartmentId
                }).ToList()
            };
            TicketCreateResult Ticket = await userAccountStore.SetTicketPayLoadContentAsync(account);


            _httpContextAccessor.HttpContext.Request.Headers[IAppClientInfo.CLIENT_Ticket_HEADER] = Ticket.Ticket;

            await userAccountLogDomain.CreateUserAccountLogAsync(new UserAccountLogSDto()
            {
                Id = AppCore.Guid(),
                UserId = user.Id,
                TypeCode = UserAccountLogTypeEnum.用户登录.ToString(),
                Remark=$"账户:{user.Account};用户:{user.UserName};登录时间:{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")};登录成功"
            });
            return new AccountLoginRDto()
            {
                AccountStatus = AccountStatusEnum.登录成功,
                EnableMFA = App.EnableMFA,
                ExpiredTime = ExpiredAt.ToString("yyyy-MM-dd HH:mm:ss"),
                Payload = Ticket.Payload,
                Ticket = Ticket.Ticket,
                ClientUUID=Ticket.ClientId
            };
        }



        /// <summary>
        /// <para>zh-cn:处理第三方应用跳转时的自动登录，并为目标应用签发新的分支 Ticket。响应中的 AppId 始终使用 SYS_APP.APP_ID 业务标识，保证其与后续权限查询使用的应用标识一致。</para>
        /// <para>en-us:Processes automatic login during a third-party application redirect and issues a new fork ticket for the target application. The response AppId always uses the SYS_APP.APP_ID business identifier so it matches subsequent authorization lookups.</para>
        /// </summary>
        /// <param name="RandomKey">
        /// <para>zh-cn:由登录应用暂存的、指向原始 Ticket 的一次性随机键。</para>
        /// <para>en-us:The one-time random key stored by the login application that points to the original ticket.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:包含新 Ticket 和目标应用业务标识的自动登录结果。</para>
        /// <para>en-us:The automatic-login result containing the new ticket and target application business identifier.</para>
        /// </returns>
        [HttpGet("autologin/{RandomKey}")]
        [AllowAnonymous]
        public async Task<object> AutoLogin(string RandomKey)
        {
            string AppId = _httpContextAccessor.HttpContext.Request.Headers["APPID"];
            if (AppId.IsNullOrEmpty()) throw new AuthException("ACCOUNT_CHECK_FAIL", "APPID传输异常");
            string TicketStore = AppRealization.RedisCache.String.Get(RandomKey);
            if (TicketStore == null) throw new AuthException("CANNOT_ACCESS_RESOURCE", "未授权的访问");
            UserAccountFactory userAccountFactory = await userAccountStore.GetUserAccountAsync(TicketStore);
            if (userAccountFactory == null) throw new AuthException("CANNOT_ACCESS_RESOURCE", "未授权的访问");
            var LoginApp = await appInfoDomain.GetAppInfoAsync(userAccountFactory.LoginAppId);
            if (LoginApp == null) throw new AuthException("ACCOUNT_CHECK_FAIL", "用户登录应用信息异常");
            var App = await appInfoDomain.GetAppInfoAsync(AppId);
            if (App == null) throw new AuthException("ACCOUNT_CHECK_FAIL", "用户访问应用信息异常");

            TicketCreateResult TicketCreateResult = await userAccountStore.GetForkTicketAsync(TicketStore);

            return new
            {
                //登录的参数
                Account = new
                {
                    AccountStatus = AccountStatusEnum.登录成功,
                    EnableMFA = IsOrNotEnum.否,
                    ExpiredTime = userAccountFactory.ExpiredAt.ToString("yyyy-MM-dd HH:mm:ss"),
                    Payload = TicketCreateResult.Payload,
                    Ticket = TicketCreateResult.Ticket
                },
                App = new
                {
                    //init的参数 用户初始化客户端信息
                    AppName = App.AppName,
                    AppId = App.AppId,
                    Client = new ClientConfig
                    {
                        Ticket = TicketCreateResult.Ticket,
                        ExpiredSeconds = 30 * 60,
                        AppEntryptType = App.AppEncryptType,
                        PrivateKey = App.PrivateKey,
                        EnableMFA = App.EnableMFA
                    }
                }
            };

        }

        /// <summary>
        /// 登出
        /// </summary>
        /// <param name="Ticket"></param>
        /// <returns></returns>
        [HttpGet("logout")]
        public async Task<AccountLoginOutRDto> LoginOutAsync()
        {
            UserAccountFactory userAccountFactory = await userAccountStore.GetUserAccountAsync();
            string Ticket = _httpContextAccessor.HttpContext.Request.Headers[IAppClientInfo.CLIENT_Ticket_HEADER];
            if (Ticket == null) throw new AuthException("Ticket_Payload_Faild", "Ticket负载验证不合法");
            var result=await userAccountStore.LogOutUserAccountAsync(Ticket);
            await userAccountLogDomain.CreateUserAccountLogAsync(new UserAccountLogSDto()
            {
                Id = AppCore.Guid(),
                UserId = userAccountFactory.Id,
                TypeCode = UserAccountLogTypeEnum.用户登出.ToString(),
                Remark = $"账户:{userAccountFactory.Account};用户名:{userAccountFactory.UserName}执行登出,登录时间:{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")};执行结果:{(result?"成功":"失败")}"
            });
            return new AccountLoginOutRDto()
            {
                IsSuccess = true
            };
        }
    }
}
