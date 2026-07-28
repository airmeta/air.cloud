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
using air.cloud.system.service.Services.AppServices;
using air.cloud.security.common.Base.Dtos;
using air.cloud.security.common.Model;
using air.cloud.system.model.Domains.AppInfoDomains;
using air.cloud.system.model.Dtos.AppInfoDtos;
using air.cloud.system.model.Entitys.Apps;

using Air.Cloud.Core;
using Air.Cloud.Core.Extensions;
using Air.Cloud.WebApp.FriendlyException;

using Mapster;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using RouteAttribute = Microsoft.AspNetCore.Mvc.RouteAttribute;
using Air.Cloud.Core.Standard.Taxin.Attributes;

namespace air.cloud.system.service.Impl.AppService
{
    [Route("v1/security/system/app")]
    public class AppService : IAppService
    {
        private readonly IAppInfoDomain _appInfoDomain;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AppService(IAppInfoDomain appInfoDomain,IHttpContextAccessor httpContextAccessor) { 
            this._appInfoDomain = appInfoDomain;
            this._httpContextAccessor = httpContextAccessor;
        }
        [HttpPost("save")]
        public async Task<bool> SaveAppAsync(AppInfoCreateDto dto)
        {
            if (dto.Id.IsNullOrEmpty())
            {
                return await _appInfoDomain.CreateAppAsync(dto);
            }
            else
            {
                return await _appInfoDomain.UpdateAppAsync(dto);
            }
        }
        [HttpPost("inject")]
        public async Task<bool> CreateFirstAppAsync(AppInfoFirstCreateDto dto)
        {
            string AppSecrect= _httpContextAccessor.HttpContext.Request.Headers["Ticket"];
            //检查Secrect是否为空 是否合法
            string StoreSecrect= AppRealization.RedisCache.String.Get("InitAppSecret");

            if (string.IsNullOrEmpty(StoreSecrect) || StoreSecrect != AppSecrect)
            {
                throw Oops.Oh("非法请求");
            }
            await AppRealization.RedisCache.Key.DeleteAsync("InitAppSecret");
            return await _appInfoDomain.CreateFirstAppAsync(dto);
        }
        [HttpDelete("remove/{appId}")]
        public async Task<bool> DeleteAppAsync(string appId)
        {
            return await _appInfoDomain.DeleteAppAsync(appId);
        }
        [HttpGet("detail/{appId}")]
        [TaxinService("system_appInfo_query_by_appId")]
        public async Task<AppInfoResultDto> GetAppInfoAsync(string appId)
        {
            var App= await _appInfoDomain.GetAppInfoAsync(appId);
            return App.Adapt<AppInfoResultDto>();
        }
        [HttpGet("list")]
        public async Task<List<AppSelectRDto>> ListAllAppsAsync()
        {
           return await _appInfoDomain.ListAllAppsAsync();
        }
        [HttpPost("query")]
        public async Task<PageList<AppInfoResultDto>> QueryAppsAsync(BaseQDto dto)
        {
            return await _appInfoDomain.QueryAppsAsync(dto);
        }
        [HttpGet("assign/role/{roleId}/{appId}")]
        public async Task<bool> JoinAppToRoleAsync(string roleId, string appId)
        {
            return await _appInfoDomain.JoinAppToRoleAsync(roleId, appId);
        }
        [HttpGet("remove/role/{roleId}/{appId}")]
        public async Task<bool> RemoveAppFromRoleAsync(string roleId, string appId)
        {
            return await _appInfoDomain.RemoveAppFromRoleAsync(roleId, appId);
        }
    }
}
