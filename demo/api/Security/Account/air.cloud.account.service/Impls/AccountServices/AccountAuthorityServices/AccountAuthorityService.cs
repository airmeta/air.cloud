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
using air.cloud.account.service.Services.AccountServices.AccountAuthorityServices;
using air.cloud.security.common.Auths;
using air.cloud.security.common.Enums;
using air.cloud.security.common.Exceptions;
using air.cloud.security.common.Model;
using air.cloud.system.model.Domains.AppInfoDomains;
using air.cloud.system.model.Domains.EntityAssociationDomains;
using air.cloud.system.model.Domains.MenuDomains;
using air.cloud.system.model.Domains.OrganizationDomains;
using air.cloud.system.model.Domains.UserDomains;
using air.cloud.system.model.Dtos.AccountDtos;
using air.cloud.system.model.Dtos.UserDtos;
using air.cloud.system.model.Entitys.Organization;
using air.cloud.system.model.Entitys.Roles;
using air.cloud.system.model.Entitys.Users;

using Air.Cloud.Core;
using Air.Cloud.Core.App;
using Air.Cloud.Core.Extensions;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace air.cloud.account.service.Impls.AccountServices.AccountAuthorityServices
{
    /// <summary>
    /// <para>zh-cn:应用登录服务</para>
    /// <para>en-us:App Login Service</para>
    /// </summary>
    [Route("v1/identity/authority")]
    public  class AccountAuthorityService: IAccountAuthorityService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IAppInfoDomain appInfoDomain;
        private readonly IEntityAssociationDomain entityAssociationDomain;
        private readonly IDepartmentDomain departmentDomain;
        private readonly IUserAccountStore userAccount;
        private readonly IUserGroupDomain userGroupDomain;

        private readonly IMenuDomain menuDomain;
        public AccountAuthorityService(IHttpContextAccessor httpContextAccessor,
            IEntityAssociationDomain entityAssociationDomain,
            IAppInfoDomain appInfoDomain,
            IDepartmentDomain departmentDomain,
            IUserAccountStore userAccount,
            IUserGroupDomain userGroupDomain,
            IMenuDomain menuDomain)
        {
            this._httpContextAccessor = httpContextAccessor;
            this.appInfoDomain = appInfoDomain;
            this.departmentDomain = departmentDomain;
            this.entityAssociationDomain = entityAssociationDomain;
            this.userAccount = userAccount;
            this.menuDomain = menuDomain;
            this.userGroupDomain = userGroupDomain;
        }

        /// <summary>
        /// 第四步: 获取对应App的用户权限信息
        /// </summary>
        /// <returns></returns>
        [HttpGet("account")]
        public async Task<UserAccountInfo> GetAuthority()
        {
            string AppId = _httpContextAccessor.HttpContext.Request.Headers["APPID"];
            if (AppId.IsNullOrEmpty()) throw new AuthException("ACCOUNT_CHECK_FAIL", "APPID传输异常");
            UserAccountFactory userAccountFactory = await userAccount.GetUserAccountAsync();
            if (userAccountFactory==null) throw new AuthException("CANNOT_ACCESS_RESOURCE", "未授权的访问");
            var LoginApp = await appInfoDomain.GetAppInfoAsync(userAccountFactory.LoginAppId);
            if (LoginApp == null) throw new AuthException("ACCOUNT_CHECK_FAIL", "用户登录应用信息异常");
            var App = await appInfoDomain.GetAppInfoAsync(AppId);
            if (App == null) throw new AuthException("ACCOUNT_CHECK_FAIL", "用户访问应用信息异常");
            //读取Ticket下的App是否具有此权限
            //用户关联应用信息
            IList<AccountAppIdsRDto> appIds = await appInfoDomain.GetUserAccountAppIdsAsync(userAccountFactory.Id);
            if (!appIds.Any(s => s.AppId == AppId)) throw new AuthException("CANNOT_ACCESS_RESOURCE", "未授权的访问");
            //查询所有实体关联
            var EntityAssociations = await entityAssociationDomain.GetEntityAssociationsAsync(userAccountFactory.Id, AppId, new AssociationTypeEnum[]
            {
                AssociationTypeEnum.用户与用户组,
                AssociationTypeEnum.用户与角色组,
                AssociationTypeEnum.用户与角色,
                AssociationTypeEnum.用户与部门
            });

            var RoleIds = EntityAssociations.Where(e => e.AssociationType == AssociationTypeEnum.用户与角色).Select(e => e.TargetEntityId).ToList();

            var RoleGroupIds = EntityAssociations.Where(e => e.AssociationType == AssociationTypeEnum.用户与角色组).Select(e => e.TargetEntityId).ToList();

            //查询用户关联的角色组关联的角色

            var RoleGroupChildRoles = await entityAssociationDomain.GetEntityAssociationsAsync(RoleGroupIds, AppId, AssociationTypeEnum.角色与角色组);

            IList<Menu> menus = await menuDomain.GetMenusByRoleIdsAsync(RoleIds.Union(RoleGroupChildRoles.Select(s => s.SourceEntityId)).ToList(), AppId);

            #region 加载用户关联信息
            var associations = EntityAssociations.Where(e => e.AssociationType == AssociationTypeEnum.用户与部门).ToList();
            Department department = await departmentDomain.GetDepartmentAsync(associations.FirstOrDefault()?.TargetEntityId, App.Id);
            #endregion
            var UserGroupIds = EntityAssociations.Where(e => e.AssociationType == AssociationTypeEnum.用户与用户组).Select(e => e.TargetEntityId).ToList();
            IList<UserGroup> groups = await userGroupDomain.QueryUserGroupsAsync(UserGroupIds);
            AppRealization.Output.Print("用户信息读取完成", AppRealization.JSON.Serialize(userAccountFactory));
            UserAccountInfo accountInfo = new UserAccountInfo()
            {
                Account = userAccountFactory.Account,
                Id = userAccountFactory.Id,
                User = new UserRDto()
                {
                    Id = userAccountFactory.Id,
                    Account = userAccountFactory.Account,
                    AppUserId = userAccountFactory.AppUserId,
                    Email = userAccountFactory.Email,
                    IdCardNo = userAccountFactory.IdCardNo,
                    PhoneNumber = userAccountFactory.PhoneNumber,
                    UserName = userAccountFactory.UserName,
                    DepartmentName = department?.DepartmentName,
                    DepartmentId = department?.Id
                },
                Authoritys= menus.Select(item=>new AuthorityItem()
                {
                    SortNumber = item.SortNumber,
                    Children = new List<AuthorityItem>(),
                    Component = item.Component,
                    Icon = item.Icon,
                    Path = item.Path,
                    Title = item.Title,
                    Type = item.Type,
                    Id = item.Id,
                    Hide = item.Hide,
                    Authority = item.Authority,
                    Meta = string.Empty,
                    ParentId=item.ParentId
                }).ToList(),
                UserGroups = groups.Select(s => s.GroupName).ToList(),
                CurrentAppId = App.AppId,
                CurrentAppName = App.AppName
            };
            return accountInfo;
        }

        /// <summary>
        /// <para>zh-cn:获取当前登录用户信息 根据请求头中的Ticket信息</para>
        /// <para>en-us:Get current logged in user information based on the Ticket information in the request header</para>
        /// </summary>
        /// <returns>
        ///  <para>zh-cn:用户信息和当前访问应用的信息</para>
        ///  <para>en-us:User information and current access application information</para>
        /// </returns>
        /// <exception cref="AuthException">
        ///  <para>zh-cn:未授权的访问异常</para>
        ///  <para>en-us:Unauthorized access exception</para>
        /// </exception>
        [HttpGet("me")]
        public async Task<object> GetCurrentUserInfo()
        {
            string AppId = _httpContextAccessor.HttpContext.Request.Headers["APPID"];
            if (AppId.IsNullOrEmpty()) throw new AuthException("ACCOUNT_CHECK_FAIL", "APPID传输异常");
            UserAccountFactory userAccountFactory = await userAccount.GetUserAccountAsync();
            if (userAccountFactory == null) throw new AuthException("CANNOT_ACCESS_RESOURCE", "未授权的访问");
            var LoginApp = await appInfoDomain.GetAppInfoAsync(userAccountFactory.LoginAppId);
            if (LoginApp == null) throw new AuthException("ACCOUNT_CHECK_FAIL", "用户登录应用信息异常");
            var App = await appInfoDomain.GetAppInfoAsync(AppId);
            if (App == null) throw new AuthException("ACCOUNT_CHECK_FAIL", "用户访问应用信息异常");
            return new
            {
                //登录的参数
                Account = new
                {
                    AccountStatus = AccountStatusEnum.登录成功,
                    EnableMFA = IsOrNotEnum.否,
                    ExpiredAt = userAccountFactory.ExpiredAt,
                    Payload = userAccountFactory.GetAccountPublicPayLoad(),
                    Ticket = userAccountFactory.Ticket
                },
                App = new
                {
                    //init的参数 用户初始化客户端信息
                    AppName = App.AppName,
                    AppId = App.Id,
                    Client = new ClientConfig
                    {
                        Ticket = userAccountFactory.Ticket,
                        ExpiredSeconds = 30 * 60,
                        AppEntryptType = App.AppEncryptType,
                        PrivateKey = App.PrivateKey,
                        EnableMFA = App.EnableMFA
                    }
                }
            };

        }

        /// <summary>
        /// <para>zh-cn:获取跳转到第三方应用的链接地址</para>
        /// <para>en-us:Get redirect URL to third-party application</para>
        /// </summary>
        /// <param name="AppId">
        /// <para>zh-cn:应用ID</para>
        /// <para>en-us:Application ID</para>
        /// </param>
        /// <returns></returns>
        [HttpGet("go/{AppId}")]
        public async Task<string> GetRedirectUrl(string AppId)
        {
            string RandomKey = AppCore.Guid();
            UserAccountFactory userAccountFactory = await userAccount.GetUserAccountAsync();
            string Ticket = userAccountFactory.Ticket;
            await AppRealization.RedisCache.String.SetAsync(RandomKey, Ticket,new TimeSpan(0,0,95));
            var app = await appInfoDomain.GetAppInfoAsync(AppId);
            string Url = app.AppRedirectUrl+"?key="+RandomKey;
            return Url;
        }

        /// <summary>
        /// <para>zh-cn:拒绝授权，删除Redis中对应的Key，防止自动登录</para>
        /// <para>en-us:Reject authorization, delete the corresponding Key in Redis to prevent auto login</para>
        /// </summary>
        /// <param name="Key"></param>
        /// <returns></returns>
        [HttpGet("reject/{Key}")]
        public async Task<bool> RejectAuthorizationKey(string Key)
        {
            await AppRealization.RedisCache.Key.DeleteAsync(Key);
            return true;
        }



    }
}
