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
using air.cloud.system.model.Dtos.AccountDtos;
using air.cloud.system.model.Dtos.AppInfoDtos;
using air.cloud.system.model.Entitys.Apps;
using air.cloud.security.common.Base.Dtos;
using air.cloud.security.common.Enums;
using air.cloud.security.common.Model;

using Air.Cloud.Core.Standard.DataBase.Domains;
using Air.Cloud.Core.Standard.DynamicServer;

namespace air.cloud.system.model.Domains.AppInfoDomains
{
    public interface IAppInfoDomain:ITransient,IEntityDomain
    {

        /// <summary>
        /// <para>zh-cn:创建第一个App-身份认证服务本身</para>
        /// <para>en-us:Create App</para>   
        /// </summary>
        /// <returns></returns>
        public Task<bool> CreateFirstAppAsync(AppInfoFirstCreateDto dto);

        /// <summary>
        /// <para>zh-cn:创建App</para>
        /// <para>en-us:Create App</para>   
        /// </summary>
        /// <returns></returns>
        public Task<bool> CreateAppAsync(AppInfoCreateDto dto, IsOrNotEnum IsDefault = IsOrNotEnum.否, IsOrNotEnum CanDelete = IsOrNotEnum.是);
        /// <summary>
        /// <para>zh-cn:更新App</para>
        /// <para>en-us:Update App</para>
        /// </summary>
        public Task<bool> UpdateAppAsync(AppInfoCreateDto dto);

        /// <summary>
        /// <para>zh-cn:删除App</para>
        /// <para>en-us:Delete App</para>
        /// </summary>
        /// <param name="appId"></param>
        public Task<bool> DeleteAppAsync(string appId);

        /// <summary>
        /// <para>zh-cn:获取App信息</para>
        /// <para>en-us:Get App Info</para>
        /// </summary>
        /// <param name="appId"></param>
        /// <returns></returns>
        public Task<AppInformation> GetAppInfoAsync(string appId);
        /// <summary>
        /// <para>zh-cn:查询App列表</para>
        /// <para>en-us:Query App List</para>
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        public Task<PageList<AppInfoResultDto>> QueryAppsAsync(BaseQDto dto);
        /// <summary>
        /// <para>zh-cn:列出所有App</para>
        /// <para>en-us:List All Apps</para>
        /// </summary>
        /// <returns></returns>
        public Task<List<AppSelectRDto>> ListAllAppsAsync();


        /// <summary>
        ///  <para>zh-cn:检查是否存在App</para>
        ///  <para>en-us:Check if App exists</para>
        /// </summary>
        /// <returns></returns>
        public Task<bool> HasAppAsync(string AppId=null);
        /// <summary>
        /// 获取默认应用信息
        /// </summary>
        /// <returns></returns>
        Task<AppInformation> GetFirstAppAsync();
        /// <summary>
        /// 获取用户关联的应用列表
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<AccountAppIdsRDto[]> GetUserAccountAppIdsAsync(string id);

        #region 角色与App关联
        /// <summary>
        /// 分配应用给角色
        /// </summary>
        /// <param name="roleId"></param>
        /// <param name="appId"></param>
        /// <returns></returns>
        public Task<bool> JoinAppToRoleAsync(string roleId, string appId,bool IsFirstApp = false);
        /// <summary>
        /// 移除角色的应用
        /// </summary>
        /// <param name="roleId"></param>
        /// <param name="appId"></param>
        /// <returns></returns>
        public Task<bool> RemoveAppFromRoleAsync(string roleId, string appId);



        #endregion
    }
}
