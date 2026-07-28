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
using air.cloud.system.model.Dtos.AppInfoDtos;
using air.cloud.system.model.Entitys.Apps;
using air.cloud.security.common.Base.Dtos;
using air.cloud.security.common.Dtos;
using air.cloud.security.common.Model;

using Air.Cloud.Core.Standard.DataBase.Domains;
using Air.Cloud.Core.Standard.DynamicServer;
using air.cloud.system.model.Dtos.AppAuthDtos;
using air.cloud.system.model.Dtos.AppRouteDtos;

namespace air.cloud.system.model.Domains.AppInfoDomains
{
    public interface IAppRouteDomain:IEntityDomain,ITransient
    {
        /// <summary>
        /// <para>zh-cn:创建App路由</para>
        /// <para>en-us:Create AppRoute</para>   
        /// </summary>
        /// <returns></returns>
        public Task<bool> CreateAppRouteAsync(AppRouteSDto dto);
        /// <summary>
        /// <para>zh-cn:更新App路由</para>
        /// <para>en-us:Update AppRoute</para>
        /// </summary>
        public Task<bool> UpdateAppRouteAsync(AppRouteSDto dto);

        /// <summary>
        /// <para>zh-cn:删除App路由</para>
        /// <para>en-us:Delete AppRoute</para>
        /// </summary>
        /// <param name="appId"></param>
        public Task<bool> DeleteAppRouteAsync(string Id);

        /// <summary>
        /// <para>zh-cn:获取App路由信息</para>
        /// <para>en-us:Get AppRoute Info</para>
        /// </summary>
        /// <param name="appId"></param>
        /// <returns></returns>
        public Task<AppRoute> GetAppRouteAsync(string Id);
        /// <summary>
        /// <para>zh-cn:查询App路由列表</para>
        /// <para>en-us:Query AppRoute List</para>
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        public Task<PageList<AppRouteSDto>> QueryAppRoutesAsync(BaseQDto dto);
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
        public Task<bool> BindAppRouteAsync(string RouteId,string BindAppId);

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
        public Task<bool> RemoveAppRouteAuthAsync(string RouteId, string BindAppId);

        /// <summary>
        /// <para>zh-cn:查询应用路由授权信息</para>
        /// <para>en-us:Query AppRoute Auth Info</para>
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        Task<PageList<AppRouteAuthResultDto>> QueryAppRouteAuthAsync(BaseQDto dto);
        /// <summary>
        /// <para>zh-cn:查询应用所有路由授权信息</para>
        /// <para>en-us:Query All AppRoute Auth Info</para>
        /// </summary>
        /// <param name="BindAppId"></param>
        /// <returns></returns>
        Task<AppAllRouteAuthResultDto> QueryAllAppRouteAuthAsync(string BindAppId);


        /// <summary>
        /// <para>zh-cn:绑定当前所有服务路由到应用</para>
        /// <para>en-us:Bind Current Service All Routes to App</para>
        /// </summary>
        /// <returns></returns>
        Task<bool> BindCurrentServiceAllRouteToAppAsync(IList<AppRouteSDto> routes,string AppId);


        /// <summary>
        /// <para>zh-cn:通过多个Ids查询App路由</para>
        /// <para>en-us:Query AppRoute by multiple Ids</para>
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        Task<IList<AppRouteQueryByIdsResultDto>> QueryAppRouteDataAsync(AppRouteQueryByIdsDto dto);

    }
}
