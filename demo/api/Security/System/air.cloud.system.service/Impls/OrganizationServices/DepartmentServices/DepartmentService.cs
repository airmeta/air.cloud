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
using air.cloud.system.service.Services.OrganizationServices.DepartmentServices;
using air.cloud.security.common.Base.Dtos;
using air.cloud.security.common.Enums;
using air.cloud.system.model.Domains.AppInfoDomains;
using air.cloud.system.model.Domains.EntityAssociationDomains;
using air.cloud.system.model.Domains.OrganizationDomains;
using air.cloud.system.model.Domains.RegionDomains;
using air.cloud.system.model.Dtos.OrganizationDtos.DepartmentDtos;

using Air.Cloud.Core.Extensions;
using Air.Cloud.WebApp.FriendlyException;

using Mapster;

using Microsoft.AspNetCore.Mvc;

using System.ComponentModel;

using RouteAttribute = Microsoft.AspNetCore.Mvc.RouteAttribute;

namespace air.cloud.system.service.Impl.OrganizationServices.DepartmentServices
{
    [Route("v1/security/system/department")]
    [Description("部门管理")]
    public  class DepartmentService : IDepartmentService
    {
        private readonly IDepartmentDomain _departmentDomain;
        private readonly IRegionDomain regionDomain;
        private readonly IAppInfoDomain appInfoDomain;
        private readonly IEntityAssociationDomain entityAssociationDomain;
        public DepartmentService(IDepartmentDomain departmentDomain,
            IEntityAssociationDomain entityAssociationDomain,
            IRegionDomain regionDomain,IAppInfoDomain appInfoDomain)
        {
            this._departmentDomain = departmentDomain;
            this.regionDomain= regionDomain;
            this.appInfoDomain = appInfoDomain;
            this.entityAssociationDomain = entityAssociationDomain;
        }
       
        [HttpDelete("remove/{departmentId}")]
        public async Task<bool> DeleteDepartmentAsync(string departmentId)
        {
            return await _departmentDomain.DeleteDepartmentAsync(departmentId);
        }
        [HttpGet("detail/{departmentId}")]
        public async Task<DepartmentSDto> GetDepartmentAsync(string departmentId)
        {
            var detail= await _departmentDomain.GetDepartmentAsync(departmentId);
            detail.ParentDepartmentId = detail.ParentDepartmentId == DepartmentSDto.TOP_DEPARTMENT_ID ? string.Empty : detail.ParentDepartmentId;

            var apps = await entityAssociationDomain.GetEntityAssociationsAsync(departmentId,detail.AppId,AssociationTypeEnum.部门与区域,AssociationTypeEnum.部门与应用);

            var sdetail = detail.Adapt<DepartmentSDto>();
            sdetail.ManagedAppIds = string.Join(",", apps.Where(s => s.AssociationType == AssociationTypeEnum.部门与应用).Select(s=>s.TargetEntityId).ToList());
            sdetail.ManagedRegions = string.Join(",", apps.Where(s => s.AssociationType == AssociationTypeEnum.部门与区域).Select(s => s.TargetEntityId).ToList());
            return sdetail;
        }
        [HttpPost("query")]
        public async  Task<List<DepartmentTreeDto>> QueryDepartmentsAsync(BaseQDto dto)
        {
            return await _departmentDomain.QueryDepartmentsAsync(dto);
        }
        [HttpDelete("remove/region/{departmentId}/{regionId}")]
        public async Task<bool> RemoveRegionFromDepartmentAsync(string departmentId, string regionId)
        {
            var Department = await _departmentDomain.GetDepartmentAsync(departmentId);
            if (Department == null) return false;
            var Region = await regionDomain.GetRegionAsync(regionId);
            if (Region == null) throw Oops.Oh("区域信息不存在");
            return await _departmentDomain.RemoveRegionFromDepartmentAsync(Department.Id,regionId, Region.AppId);
        }
        [HttpGet("assign/region/{departmentId}/{regionId}")]
        public async Task<bool> AssignRegionToDepartmentAsync(string departmentId, string regionId)
        {
            var Department = await _departmentDomain.GetDepartmentAsync(departmentId);
            if (Department == null) throw Oops.Oh("部门信息不存在");
            var Region = await regionDomain.GetRegionAsync(regionId);
            if (Region == null) throw Oops.Oh("区域信息不存在");
            return await _departmentDomain.AssignRegionToDepartmentAsync(departmentId, regionId, Region.AppId);
        }

        [HttpDelete("remove/app/{departmentId}/{appId}")]
        public async Task<bool> RemoveAppFromDepartmentAsync(string departmentId, string appId)
        {
            var Department = await _departmentDomain.GetDepartmentAsync(departmentId);
            if (Department == null) throw Oops.Oh("部门信息不存在");
            var App = await appInfoDomain.GetAppInfoAsync(appId);
            if (App == null) throw Oops.Oh("应用信息不存在");
            return await _departmentDomain.RemoveAppFromDepartmentAsync(Department.Id, appId);
        }
        [HttpGet("assign/app/{departmentId}/{appId}")]
        public async Task<bool> AssignAppToDepartmentAsync(string departmentId, string appId)
        {
            var Department = await _departmentDomain.GetDepartmentAsync(departmentId);
            if (Department == null) throw Oops.Oh("部门信息不存在");
            var App = await appInfoDomain.GetAppInfoAsync(appId);
            if (App == null) throw Oops.Oh("应用信息不存在");
            return await _departmentDomain.AssignAppToDepartmentAsync(departmentId, appId);
        }

        [HttpPost("save")]
        public async Task<bool> SaveDepartmentAsync(DepartmentSDto dto)
        {
            string DepartmentId = string.Empty;
            dto.ParentDepartmentId = dto.ParentDepartmentId.IsNullOrEmpty() ? DepartmentSDto.TOP_DEPARTMENT_ID : dto.ParentDepartmentId;
            if (dto.Id.IsNullOrEmpty())
            {
                DepartmentId= await _departmentDomain.CreateDepartmentAsync(dto);
            }
            else
            {
                DepartmentId = await _departmentDomain.UpdateDepartmentAsync(dto);
            }
            if (!DepartmentId.IsNullOrEmpty())
            {
                
                if (!dto.ManagedAppIds.IsNullOrEmpty())
                {
                    await _departmentDomain.MergeAppsFromDepartmentAsync(DepartmentId, dto.AppId, dto.ManagedAppIds);
                }
                if (!dto.ManagedRegions.IsNullOrEmpty())
                {
                    await _departmentDomain.MergeRegionsFromDepartmentAsync(DepartmentId, dto.AppId, dto.ManagedRegions);
                }
            }
            return true;
        }
    }
}
