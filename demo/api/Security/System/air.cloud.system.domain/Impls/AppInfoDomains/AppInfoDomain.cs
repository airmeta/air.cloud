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
using air.cloud.security.common.Auths;
using air.cloud.security.common.Base.Dtos;
using air.cloud.security.common.Enums;
using air.cloud.security.common.Extensions;
using air.cloud.security.common.Model;
using air.cloud.system.model.Domains.AppInfoDomains;
using air.cloud.system.model.Domains.EntityAssociationDomains;
using air.cloud.system.model.Domains.RoleDomains;
using air.cloud.system.model.Domains.UserDomains;
using air.cloud.system.model.Dtos.AccountDtos;
using air.cloud.system.model.Dtos.AppInfoDtos;
using air.cloud.system.model.Entitys.Apps;

using Air.Cloud.Core.App;
using Air.Cloud.Core.Extensions;
using Air.Cloud.Core.Extensions.Linqs;
using Air.Cloud.Core.Plugins.Security.RSA;
using Air.Cloud.Core.Plugins.Security.SM2;
using Air.Cloud.Core.Standard.SkyMirror;
using Air.Cloud.EntityFrameWork.Core.Extensions;
using Air.Cloud.EntityFrameWork.Core.Repositories;
using Air.Cloud.WebApp.FriendlyException;

using Mapster;

using Microsoft.EntityFrameworkCore;

namespace air.cloud.system.domain.Impls.AppInfoDomains
{
    public class AppInfoDomain : IAppInfoDomain
    {
        private readonly IRepository<AppInformation> repository;
        private readonly IUserDomain userDomain;
        private readonly IEntityAssociationDomain entityAssociationDomain;
        private readonly IUserAccountStore userAccountStore;

        private readonly IAppRouteDomain routeDomain;

        /// <summary>
        /// <para>zh-cn:当前程序密钥私钥</para>
        /// </summary>
        private string? SecurityAppPrivateKey => AppCore.Configuration["SkyMirrorSettings:PrivateKey"];

        /// <summary>
        /// <para>zh-cn:当前程序数据加密类型</para>
        /// </summary>
        private AppEntryptTypeEnum? appEntryptType => (AppEntryptTypeEnum?)(AppCore.Configuration["SkyMirrorSettings:AppEncryptType"]?.ToInt());


        private readonly IRoleDomain roleDomain;
        public AppInfoDomain(IRepository<AppInformation> repository,
            IUserDomain userDomain,
            IRoleDomain roleDomain,
            IEntityAssociationDomain entityAssociationDomain,
            IUserAccountStore userAccountStore
            )
        {

            this.repository = repository;
            this.userDomain = userDomain;
            this.entityAssociationDomain = entityAssociationDomain;
            this.userAccountStore = userAccountStore;
            this.roleDomain = roleDomain;
        }

        /// <summary>
        /// <para>zh-cn:创建应用并生成平台侧密钥对。普通应用必须提交应用私钥，平台仅持久化它用于解密应用请求；平台自己的密钥对始终由本方法生成，不能由管理页面指定。</para>
        /// <para>en-us:Creates an application and generates the platform key pair. A regular application must submit its application private key, which the platform persists only to decrypt application requests; the platform key pair is always generated here and cannot be specified by the management page.</para>
        /// </summary>
        /// <param name="dto">
        /// <para>zh-cn:应用注册数据。默认应用使用 PrivateKey，普通应用使用 AppPrivateKey。</para>
        /// <para>en-us:Application registration data. The default application uses PrivateKey and a regular application uses AppPrivateKey.</para>
        /// </param>
        /// <param name="IsDefault">
        /// <para>zh-cn:是否创建平台默认应用。默认值为否。</para>
        /// <para>en-us:Whether to create the platform default application. Defaults to no.</para>
        /// </param>
        /// <param name="CanDelete">
        /// <para>zh-cn:是否允许删除创建的应用。默认值为是。</para>
        /// <para>en-us:Whether the created application can be deleted. Defaults to yes.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:应用持久化成功时返回 true。</para>
        /// <para>en-us:Returns true when the application has been persisted.</para>
        /// </returns>
        /// <exception cref="AppFriendlyException">
        /// <para>zh-cn:加密方式不受支持或应用私钥缺失时引发。</para>
        /// <para>en-us:Thrown when the encryption type is unsupported or the application private key is missing.</para>
        /// </exception>
        public async Task<bool> CreateAppAsync(AppInfoCreateDto dto, IsOrNotEnum IsDefault = IsOrNotEnum.否,IsOrNotEnum CanDelete=IsOrNotEnum.是)
        {
            AppInformation app = dto.Adapt<AppInformation>();
            
            if (IsDefault == IsOrNotEnum.是)
            {
                if (await HasAppAsync())
                {
                    throw Oops.Oh("只能创建一个默认应用");
                }
                app.AppPrivateKey = dto.PrivateKey;
                app.AppRedirectUrl = "";
            }
            ValidateApplicationEncryption(dto.AppEncryptType, app.AppPrivateKey);
            app.Id = AppCore.Guid();
            if (app.AppId.IsNullOrEmpty())
            {
                app.AppId = AppCore.Guid();
            }
            app.IsDefault = IsDefault;
            app.Description = dto.Description;
            app.CanDelete= CanDelete;
            app.IsEnable = dto.IsEnable;
            switch (dto.AppEncryptType)
            {
                case AppEntryptTypeEnum.RSA:
                    var keys = RsaEncryption.CreateRSAKey();
                    app.PublicKey = keys.PublicKey;
                    app.PrivateKey = keys.PrivateKey;
                    app.AppEncryptType = dto.AppEncryptType;
                    break;
                case AppEntryptTypeEnum.SM2:
                    var sm2Keys = SM2Encryption.GenerateKeyPair();
                    app.PublicKey = sm2Keys.Item1;
                    app.PrivateKey = sm2Keys.Item2;
                    break;
                default:
                    break;
            }
            await repository.InsertAsync(app);
            return true;
        }

        public async Task<bool> CreateFirstAppAsync(AppInfoFirstCreateDto dto)
        {
            string AppId = dto.DefaultAppId;
            var App = await GetAppInfoAsync(AppId);
            if (App != null) throw Oops.Oh("已完成应用初始化,无需再次创建");
            var HasApp = await HasAppAsync();
            if (HasApp)
            {
                throw Oops.Oh("已完成应用初始化,无需再次创建");
            }
            if (string.IsNullOrEmpty(SecurityAppPrivateKey) || !appEntryptType.HasValue)
            {
                throw Oops.Oh("统一身份认证服务端配置错误,缺少私钥与加密类型");
            }
            //
            var createAppResult = await CreateAppAsync(new AppInfoCreateDto()
            {
                AppName = "统一身份认证平台",
                AppRedirectUrl = string.Empty,
                AppId = AppId,
                PrivateKey = SecurityAppPrivateKey,
                AppEncryptType = appEntryptType.Value,
                IsCommonApp = IsOrNotEnum.否,
                IsEnable= IsOrNotEnum.是
            }, IsOrNotEnum.是, IsOrNotEnum.否);


            string SystemAppId = dto.AppId ?? AppCore.Guid();
            var createAppResult1 = await CreateAppAsync(new AppInfoCreateDto()
            {
                AppName = "系统管理平台",
                AppRedirectUrl = dto.AppRedirectUrl,
                AppId = SystemAppId,
                PrivateKey = dto.PrivateKey,
                AppEncryptType = dto.AppEncryptType,
                IsCommonApp = IsOrNotEnum.否,
                IsEnable = IsOrNotEnum.是
            }, IsOrNotEnum.否, IsOrNotEnum.否);
            var createUserResult = await userDomain.CreatDefaultAccountAsync(this, dto.DefaultAccount, SystemAppId);

            /** 把本服务中的所有路由信息全部初始化进AppRoute中*/

            var currentClientRoutes = ISkyMirrorShieldClientStandard.ClientEndpointDatas.Select(s => new AppRouteSDto()
            {
                AppId = SystemAppId,
                Description = s.Description,
                Route = s.Path,
                AllowAnonymous = s.IsAllowAnonymous ? IsOrNotEnum.是 : IsOrNotEnum.否,
                AuthorizationMetas = s.AuthorizeDatas,
                RequiresAuthorization = s.RequiresAuthorization ? IsOrNotEnum.是 : IsOrNotEnum.否,
                Method = s.Method
            }).ToList();

            await routeDomain.BindCurrentServiceAllRouteToAppAsync(currentClientRoutes, SystemAppId);

            return createAppResult && createUserResult;
        }
        public async Task<AppInformation> GetFirstAppAsync()
        {
            var app = await repository.DetachedEntities.FirstOrDefaultAsync(s => s.IsDefault == IsOrNotEnum.是);
            return app;
        }
        public async Task<bool> HasAppAsync(string AppId = null)
        {
            //原ORACLE的逻辑
            //string SQL = "SELECT COUNT(1) FROM USER_TABLES WHERE TABLE_NAME = 'SYS_APP'";
            //string? Total = (await repository.Database.ExecuteScalarAsync(SQL))?.ToString();
            //if (Total.IsNullOrEmpty() || Total.ToInt() == 0)
            //{
            //    repository.Database.EnsureCreated();
            //}
            if (AppId.IsNullOrEmpty())
            {
                return await repository.DetachedEntities.AnyAsync();
            }
            return await repository.DetachedEntities.AnyAsync(x => x.AppId == AppId);
        }
        public async Task<bool> DeleteAppAsync(string appId)
        {
            UserAccountFactory userAccountFactory = await userAccountStore.GetUserAccountAsync();
            var app = await repository.DetachedEntities.FirstOrDefaultAsync(x => x.AppId == appId || x.Id == appId);
            if (app == null) return true;
            if (app.CanDelete==IsOrNotEnum.否) throw Oops.Oh("此应用为基础应用,无法删除");
            //查询App下是否有用户
            var departmentAssociationApp = await entityAssociationDomain.GetEntityAssociationsAsync(app.Id, string.Empty, AssociationTypeEnum.部门与应用);
            if (departmentAssociationApp.Any()) throw Oops.Oh("应用下有关联部门，无法删除");
            var departmentAssociationRole = await entityAssociationDomain.GetEntityAssociationsAsync(app.Id, string.Empty, AssociationTypeEnum.角色与应用);
            if (departmentAssociationRole.Any()) throw Oops.Oh("应用下有关联角色，无法删除");
            app.IsDelete = IsOrNotEnum.是;
            app.DeleteTime = DateTime.Now;
            app.DeleteUserId = userAccountFactory.Id;
            app.DeleteUserName = userAccountFactory.UserName;
            await repository.DeleteAsync(app);
            return true;
        }

        public async Task<AppInformation> GetAppInfoAsync(string appId)
        {
            var app = await repository.DetachedEntities.FirstOrDefaultAsync(x => x.AppId == appId || x.Id == appId);
            return app;
        }



        public async Task<List<AppSelectRDto>> ListAllAppsAsync()
        {
            var list = await repository.DetachedEntities.Select(s => new AppSelectRDto()
            {
                Id = s.AppId,
                Name = s.AppName,
                Description = s.Description,
                IsEnable = s.IsEnable
            }).ToListAsync();
            return list;
        }
        public async Task<PageList<AppInfoResultDto>> QueryAppsAsync(BaseQDto dto)
        {
            var linq = LinqExpressionExtensions.And<AppInformation>();
            if (!string.IsNullOrEmpty(dto.AppId))
            {
                linq = linq.And(s => s.AppId == dto.AppId);
            }
            if (!string.IsNullOrEmpty(dto.Info))
            {
                linq = linq.And(s => s.AppName.Contains(dto.Info));
            }
            var query = repository.Change<AppInformation>().DetachedEntities.Where(linq);
            return await query.OrderByDescending(s => s.CreateTime).Select(s => new AppInfoResultDto
            {
                AppId=s.AppId,
                AppEncryptType=s.AppEncryptType,
                PublicKey=s.PublicKey,
                AppName=s.AppName,
                AppPrivateKey=s.AppPrivateKey,
                AppRedirectUrl = s.AppRedirectUrl,
                Description=s.Description,
                EnableMFA=s.EnableMFA,
                Id=s.Id,
                IsCommonApp=s.IsCommonApp,
                Logo=s.Logo
            }).ToPageListAsync<AppInfoResultDto>(dto.Page, dto.Limit);
        }

        /// <summary>
        /// <para>zh-cn:更新普通应用的可维护注册字段。平台生成的密钥对不会被修改或回显；更新请求必须继续提供应用私钥，以维持应用请求的解密能力。</para>
        /// <para>en-us:Updates the maintainable registration fields of a regular application. The platform-generated key pair is neither changed nor echoed; the update must continue to provide the application private key to preserve request decryption.</para>
        /// </summary>
        /// <param name="dto">
        /// <para>zh-cn:待更新的应用注册数据。</para>
        /// <para>en-us:Application registration data to update.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:找到并更新应用时返回 true；不存在时返回 false。</para>
        /// <para>en-us:Returns true when the application is found and updated; otherwise false.</para>
        /// </returns>
        /// <exception cref="AppFriendlyException">
        /// <para>zh-cn:加密方式不受支持或应用私钥缺失时引发。</para>
        /// <para>en-us:Thrown when the encryption type is unsupported or the application private key is missing.</para>
        /// </exception>
        public async Task<bool> UpdateAppAsync(AppInfoCreateDto dto)
        {
            var app = await repository.DetachedEntities.FirstOrDefaultAsync(s => s.Id == dto.Id);
            if (app == null) return false;
            ValidateApplicationEncryption(dto.AppEncryptType, dto.AppPrivateKey);
            app.AppName = dto.AppName;
            app.AppRedirectUrl = dto.AppRedirectUrl;
            app.Description = dto.Description;
            app.AppPrivateKey = dto.AppPrivateKey;
            app.AppEncryptType = dto.AppEncryptType;
            app.Logo = dto.Logo;
            app.IsCommonApp = dto.IsCommonApp;
            app.IsEnable = dto.IsEnable;
            await repository.UpdateIncludeAsync(app, new string[]
            {
                nameof(AppInformation.AppName),
                nameof(AppInformation.AppRedirectUrl),
                nameof(AppInformation.Description),
                nameof(AppInformation.AppPrivateKey),
                nameof(AppInformation.AppEncryptType),
                nameof(AppInformation.Logo),
                nameof(AppInformation.IsCommonApp),
                nameof(AppInformation.IsEnable)
            });
            return true;
        }

        private static void ValidateApplicationEncryption(AppEntryptTypeEnum encryptType, string? appPrivateKey)
        {
            if (!Enum.IsDefined(encryptType))
            {
                throw Oops.Oh("加密方式仅支持 RSA 或 SM2");
            }

            if (appPrivateKey.IsNullOrEmpty())
            {
                throw Oops.Oh("应用私钥不能为空");
            }
        }

        public async Task<AccountAppIdsRDto[]> GetUserAccountAppIdsAsync(string id)
        {
            IList<string> RoleIds = new List<string>();

            #region 用户与用户组关联查询角色
            //1. 查询出用户组
            var userGroups = await entityAssociationDomain.GetEntityAssociationsAsync(id, string.Empty, AssociationTypeEnum.用户与用户组);
            //2. 查询出角色组
            var roleGroupsFromUser = await entityAssociationDomain.GetEntityAssociationsAsync(id, string.Empty, AssociationTypeEnum.用户与角色组);
            var roleGroups = await entityAssociationDomain.GetEntityAssociationsAsync(userGroups.Select(s => s.TargetEntityId).ToList(), string.Empty, AssociationTypeEnum.用户组与角色组);
            //3. 查询出角色
            var rolesFromRoleGroups = await entityAssociationDomain.GetEntityAssociationsAsync(roleGroups.Union(roleGroupsFromUser).Select(s => s.TargetEntityId).ToList(), string.Empty, AssociationTypeEnum.角色与角色组);

            RoleIds = RoleIds.Union(rolesFromRoleGroups.Select(s => s.SourceEntityId).ToList()).ToList();
            #endregion

            #region  用户与角色
            //1. 直接查询用户与角色关联
            var rolesFromUser = await entityAssociationDomain.GetEntityAssociationsAsync(id, string.Empty, AssociationTypeEnum.用户与角色);

            RoleIds = RoleIds.Union(rolesFromUser.Select(s => s.TargetEntityId).ToList()).ToList();

            #endregion

            var apps = await entityAssociationDomain.GetEntityAssociationsAsync(RoleIds, string.Empty, AssociationTypeEnum.角色与应用);

            var userApps = await repository.DetachedEntities
                .Where(x => apps.Select(a => a.TargetEntityId).Contains(x.AppId))
                .Select(x => new AccountAppIdsRDto
                {
                    AppId = x.AppId,
                    AppName = x.AppName,
                    Description = x.Description,
                    LogoUrl = x.Logo,
                    AppRedirectUrl = x.AppRedirectUrl,
                    AppRedirectKey = "",
                    CreateTime = x.CreateTime
                })
                .ToArrayAsync();

            var associations = await entityAssociationDomain.GetEntityAssociationsAsync(id, string.Empty, AssociationTypeEnum.用户与部门);
            apps = await entityAssociationDomain.GetEntityAssociationsAsync(associations.Select(s => s.TargetEntityId).ToList(), string.Empty, AssociationTypeEnum.部门与应用);

            var departmentApps = await repository.DetachedEntities
                .Where(x => apps.Select(a => a.TargetEntityId).Contains(x.AppId))
                .Select(x => new AccountAppIdsRDto
                {
                    AppId = x.AppId,
                    AppName = x.AppName,
                    Description = x.Description,
                    LogoUrl = x.Logo,
                    AppRedirectUrl = x.AppRedirectUrl,
                    AppRedirectKey = "",
                    CreateTime = x.CreateTime
                })
                .ToArrayAsync();


            var CommonApps = await repository.DetachedEntities.Where(s => s.IsCommonApp == IsOrNotEnum.是 && s.IsDelete == IsOrNotEnum.否)
                .Select(x => new AccountAppIdsRDto
                {
                    AppId = x.AppId,
                    AppName = x.AppName,
                    Description = x.Description,
                    LogoUrl = x.Logo,
                    AppRedirectUrl = x.AppRedirectUrl,
                    AppRedirectKey = "",
                    CreateTime = x.CreateTime
                })
                .ToArrayAsync();
            return userApps.Union(departmentApps).Union(CommonApps).DistinctBy(s => s.AppId).OrderBy(s => s.CreateTime).ToArray();
        }

        #region 角色与App关联操作
        public async Task<bool> JoinAppToRoleAsync(string roleId, string appId, bool IsFirstApp = false)
        {
            if (!IsFirstApp)
            {
                var role = await roleDomain.GetRoleAsync(roleId);
                if (role == null) throw Oops.Oh("角色不存在，无法分配应用");
                var app = await GetAppInfoAsync(appId);
                if (app == null) throw Oops.Oh("应用不存在，无法分配应用");
                return await entityAssociationDomain.AddEntityAssignAsync(roleId, app.Id, AssociationTypeEnum.角色与应用, role.AppId);
            }
            else
            {
                return await entityAssociationDomain.AddEntityAssignAsync(roleId, appId, AssociationTypeEnum.角色与应用, appId);
            }
        }

        public async Task<bool> RemoveAppFromRoleAsync(string roleId, string appId)
        {
            var role = await roleDomain.GetRoleAsync(roleId);
            if (role == null) throw Oops.Oh("角色不存在，无法分配应用");

            var app = await GetAppInfoAsync(appId);
            if (app == null) throw Oops.Oh("应用不存在，无法分配应用");
            return await entityAssociationDomain.RemoveEntityAssignAsync(roleId, app.Id, AssociationTypeEnum.角色与应用, role.AppId);
        }
        #endregion
    }
}
