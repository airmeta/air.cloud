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
using air.cloud.system.model.Dtos.OrganizationDtos.DepartmentDtos;

using Air.Cloud.Core.Standard.DynamicServer;

namespace air.cloud.system.service.Services.OrganizationServices.DepartmentServices
{
    public interface IDepartmentService:IDynamicService,ITransient
    {
        /// <summary>
        /// 创建部门信息
        /// </summary>
        /// <returns></returns>
        public Task<bool> SaveDepartmentAsync(DepartmentSDto dto);
        /// <summary>
        /// 删除部门信息
        /// </summary>
        /// <param name="departmentId"></param>
        /// <returns></returns>
        public Task<bool> DeleteDepartmentAsync(string departmentId);

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
        public Task<DepartmentSDto> GetDepartmentAsync(string departmentId);

        /// <summary>
        /// 分配区域给部门
        /// </summary>
        /// <param name="departmentId"></param>
        /// <param name="regionId"></param>
        /// <returns></returns>
        public Task<bool> AssignRegionToDepartmentAsync(string departmentId, string regionId);


        /// <summary>
        /// 取消部门区域管理权限
        /// </summary>
        /// <param name="departmentId"></param>
        /// <returns></returns>
        public Task<bool> RemoveRegionFromDepartmentAsync(string departmentId, string regionId);


    }
}
