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
using air.cloud.security.common.Base.Dtos;
using air.cloud.security.common.Dtos;
using air.cloud.security.common.Model;
using air.cloud.system.model.Domains.AppInfoDomains;
using air.cloud.system.model.Dtos.AppAuthDtos;
using air.cloud.system.model.Dtos.AppInfoDtos;
using air.cloud.system.model.Dtos.AppRouteDtos;
using air.cloud.system.model.Entitys.Apps;
using air.cloud.system.service.Services.AppServices;

using Air.Cloud.Core;
using Air.Cloud.Core.Extensions;
using Air.Cloud.Core.Standard.Taxin.Attributes;

using Mapster;

using Microsoft.AspNetCore.Mvc;

namespace air.cloud.system.service.Impl.AppService
{
    [Route("v1/security/system/approute")]
    public class AppRouteService : IAppRouteService
    {
        private readonly IAppRouteDomain appRouteDomain;
        public AppRouteService(IAppRouteDomain appRouteDomain) { 
        
            this.appRouteDomain= appRouteDomain;    
        }

        /// <summary>
        /// <para>zh-cn:保存App路由</para>
        /// <para>en-us:Save AppRoute</para>   
        /// </summary>
        /// <returns></returns>
        [HttpPost("save")]
        public async Task<bool> SaveAppRouteAsync(AppRouteSDto dto)
        {
            if (dto.Id.IsNullOrEmpty())
            {
                return await appRouteDomain.CreateAppRouteAsync(dto);
            }
            return await appRouteDomain.UpdateAppRouteAsync(dto);
        }


        /// <summary>
        /// <para>zh-cn:删除App路由</para>
        /// <para>en-us:Delete AppRoute</para>
        /// </summary>
        /// <param name="appId"></param>
        [HttpDelete("remove/{Id}")]
        public async Task<bool> DeleteAppRouteAsync(string Id)
        {
            return await appRouteDomain.DeleteAppRouteAsync(Id);
        }


        /// <summary>
        /// <para>zh-cn:获取App路由信息</para>
        /// <para>en-us:Get AppRoute Info</para>
        /// </summary>
        /// <param name="appId"></param>
        /// <returns></returns>
        [HttpGet("detail/{Id}")]
        public async Task<AppRouteSDto> GetAppRouteAsync(string Id)
        {
            var AppRoute= await appRouteDomain.GetAppRouteAsync(Id);

            var dto= AppRoute.Adapt<AppRouteSDto>();

            dto.SetAuthorizationMetasFromString(AppRoute.AuthorizationMeta);

            return dto;

        }


        /// <summary>
        /// <para>zh-cn:查询应用所有路由授权信息</para>
        /// <para>en-us:Query All AppRoute Auth Info</para>
        /// </summary>
        /// <param name="BindAppId"></param>
        /// <returns></returns>
        [HttpGet("query/all-auth/{BindAppId}")]
        public async Task<AppAllRouteAuthResultDto> QueryAllAppRouteAuthAsync(string BindAppId)
        {
            return await appRouteDomain.QueryAllAppRouteAuthAsync(BindAppId);
        }

        [HttpPost("query/by-ids")]
        [TaxinService("system_appAuth_query_by_ids")]
        public async Task<IList<AppRouteQueryByIdsResultDto>> QueryAppRouteDataAsync(AppRouteQueryByIdsDto dto)
        {
            AppRealization.Output.Print("请求数据", AppRealization.JSON.Serialize(dto));
            return await appRouteDomain.QueryAppRouteDataAsync(dto);
        }


        #region 查询应用路由列表或下拉框

        /// <summary>
        /// <para>zh-cn:查询App路由列表</para>
        /// <para>en-us:Query AppRoute List</para>
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [HttpPost("query/route")]
        public async Task<PageList<AppRouteSDto>> QueryAppRoutesAsync(BaseQDto dto)
        {
            return await appRouteDomain.QueryAppRoutesAsync(dto); 
        }

        #endregion



        #region  应用路由授权管理
        /// <summary>
        /// <para>zh-cn:查询应用路由授权信息</para>
        /// <para>en-us:Query AppRoute Auth Info</para>
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [HttpPost("query/auth")]
        public async Task<PageList<AppRouteAuthResultDto>> QueryAppRouteAuthAsync(BaseQDto dto)
        {
            return await appRouteDomain.QueryAppRouteAuthAsync(dto);
        }


        /// <summary>
        /// <para>zh-cn:绑定应用路由到具体应用</para>
        /// <para>en-us:Bind AppRoute to specific App</para>
        /// </summary>
        /// <param name="RouteId">
        ///  <para>zh-cn:路由信息编号</para>
        ///  <para>en-us:Route Info Id</para>
        /// </param>
        /// <param name="BindAppId">
        ///  <para>zh-cn:绑定的应用编号</para>
        ///  <para>en-us:Bind App Id</para>
        /// </param>
        /// <returns>
        ///  <para>zh-cn:绑定结果</para>
        ///  <para>en-us:Bind Result</para>
        /// </returns>
        [HttpGet("bind/{RouteId}/{BindAppId}")]
        public async Task<bool> BindAppRouteAsync(string RouteId, string BindAppId)
        {
            return await appRouteDomain.BindAppRouteAsync(RouteId, BindAppId);
        }


        /// <summary>
        /// <para>zh-cn:解绑应用路由到具体应用</para>
        /// <para>en-us:Remove AppRoute from specific App</para>
        /// </summary>
        /// <param name="RouteId">
        ///  <para>zh-cn:路由信息编号</para>
        ///  <para>en-us:Route Info Id</para>
        /// </param>
        /// <param name="BindAppId">
        ///  <para>zh-cn:绑定的应用编号</para>
        ///  <para>en-us:Bind App Id</para>
        /// </param>
        /// <returns>
        ///  <para>zh-cn:解绑结果</para>
        ///  <para>en-us:Remove Result</para>
        /// </returns>
        [HttpGet("remove/{RouteId}/{BindAppId}")]
        public async Task<bool> RemoveAppRouteAuthAsync(string RouteId, string BindAppId)
        {
            return await appRouteDomain.RemoveAppRouteAuthAsync(RouteId, BindAppId);
        }



        #endregion

    }
}
