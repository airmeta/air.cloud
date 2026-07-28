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
using air.cloud.system.model.Dtos.OrganizationDtos.DepartmentDtos;
using air.cloud.system.model.Entitys.Organization;
using air.cloud.security.common.Base.Dtos;

using Air.Cloud.Core.Standard.DataBase.Domains;
using Air.Cloud.Core.Standard.DynamicServer;

namespace air.cloud.system.model.Domains.OrganizationDomains
{
    /// <summary>
    /// 部门领域信息
    /// </summary>
    public interface IDepartmentDomain:IEntityDomain,ITransient
    {
        /// <summary>
        /// 创建部门信息
        /// </summary>
        /// <returns></returns>
        public Task<string> CreateDepartmentAsync(DepartmentSDto dto);
        /// <summary>
        /// 删除部门信息
        /// </summary>
        /// <param name="departmentId"></param>
        /// <returns></returns>
        public Task<bool> DeleteDepartmentAsync(string departmentId, string AppId = null);
        /// <summary>
        /// 更新部门信息
        /// </summary>
        /// <param name="departmentId"></param>
        /// <param name="departmentName"></param>
        /// <param name="departmentCode"></param>
        /// <param name="parentDepartmentId"></param>
        /// <returns></returns>
        public Task<string> UpdateDepartmentAsync(DepartmentSDto dto);
        
        /// <summary>
        /// 查询部门信息
        /// </summary>
        /// <param name="Info"></param>
        /// <returns></returns>
        public Task<List<DepartmentTreeDto>> QueryDepartmentsAsync(BaseQDto dto);
        /// <summary>
        /// 查询部门详情
        /// </summary>
        /// <param name="departmentId"></param>
        /// <returns></returns>
        public Task<Department> GetDepartmentAsync(string departmentId, string AppId = null);

        /// <summary>
        /// 查询部门详情
        /// </summary>
        /// <param name="departmentIds"></param>
        /// <returns></returns>
        public Task<IList<Department>> GetDepartmentAsync(IList<string> departmentIds, string AppId = null);


        /// <summary>
        /// 查询部门详情
        /// </summary>
        /// <returns></returns>
        public Task<IList<Department>> GetUserDepartmentsAsync(string UserId,string AppId=null);



        /// <summary>
        /// 查询部门详情
        /// </summary>
        /// <param name="departmentIds">部门编码</param>
        /// <returns></returns>
        public Task<IList<Department>> GetDepartmentAsync(IList<string> departmentIds);


        /// <summary>
        /// 分配区域给部门
        /// </summary>
        /// <param name="departmentId"></param>
        /// <param name="regionId"></param>
        /// <returns></returns>
        public Task<bool> AssignRegionToDepartmentAsync(string departmentId, string regionId, string AppId);


        /// <summary>
        /// 取消部门区域管理权限
        /// </summary>
        /// <param name="departmentId"></param>
        /// <returns></returns>
        public Task<bool> RemoveRegionFromDepartmentAsync(string departmentId, string regionId, string AppId);

        /// <summary>
        /// 分配部门应用权限
        /// </summary>
        /// <param name="departmentId"></param>
        /// <param name="AppId"></param>
        /// <returns></returns>
        public Task<bool> AssignAppToDepartmentAsync(string departmentId, string AppId);


        /// <summary>
        /// 取消部门应用权限
        /// </summary>
        /// <param name="departmentId"></param>
        /// <param name="AppId"></param>
        /// <returns></returns>
        public Task<bool> RemoveAppFromDepartmentAsync(string departmentId,string AppId);


        /// <summary>
        /// 合并部门应用权限
        /// </summary>
        /// <param name="departmentId"></param>
        /// <param name="AppIds"></param>
        /// <returns></returns>
        public Task<bool> MergeAppsFromDepartmentAsync(string departmentId, string departmentAppId, string AppIds);

        /// <summary>
        /// 合并部门区域管理权限
        /// </summary>
        /// <param name="departmentId"></param>
        /// <returns></returns>
        public Task<bool> MergeRegionsFromDepartmentAsync(string departmentId, string departmentAppId, string regionIds);





    }
}
