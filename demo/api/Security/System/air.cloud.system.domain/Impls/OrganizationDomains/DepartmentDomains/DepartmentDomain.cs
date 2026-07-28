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
using air.cloud.security.common.Model;
using air.cloud.system.model.Consts;
using air.cloud.system.model.Domains.EntityAssociationDomains;
using air.cloud.system.model.Domains.OrganizationDomains;
using air.cloud.system.model.Dtos.OrganizationDtos.DepartmentDtos;
using air.cloud.system.model.Entitys.Organization;

using Air.Cloud.Core.App;
using Air.Cloud.Core.Extensions;
using Air.Cloud.Core.Extensions.Linqs;
using Air.Cloud.EntityFrameWork.Core.Repositories;
using Air.Cloud.WebApp.FriendlyException;

using Microsoft.EntityFrameworkCore;

namespace air.cloud.system.domain.Impls.OrganizationDomains.DepartmentDomains
{
    public class DepartmentDomain : IDepartmentDomain
    {
        private readonly IRepository<Department> repository;
        private readonly IEntityAssociationDomain entityAssociationDomain;
        private readonly IUserAccountStore userAccount;
        public DepartmentDomain(IRepository<Department> repository,
            IEntityAssociationDomain entityAssociationDomain,
            IUserAccountStore userAccount
            )
        {
            this.repository = repository;
            this.entityAssociationDomain = entityAssociationDomain;
            this.userAccount= userAccount;
        }

        public async Task<string> CreateDepartmentAsync(DepartmentSDto dto)
        {
            var department = await repository.DetachedEntities.FirstOrDefaultAsync(s => s.DepartmentCode == dto.DepartmentCode);
            if (department != null) return string.Empty;

            if (dto.ParentDepartmentId != DepartmentSDto.TOP_DEPARTMENT_ID)
            {
                var parentDepartment = await repository.DetachedEntities.SingleOrDefaultAsync(s => s.Id == dto.ParentDepartmentId);
                if (parentDepartment == null) throw Oops.Oh("上级部门不存在");
                dto.AppId = parentDepartment.AppId;
            }

            var HasSameNameDepartment = await repository.DetachedEntities.FirstOrDefaultAsync(s => s.DepartmentName == dto.DepartmentName && s.AppId == dto.AppId);
            if (HasSameNameDepartment != null) throw Oops.Oh("同一应用下部门名称不能重复");

            Department department1 = new Department()
            {
                Id = AppCore.Guid(),
                DepartmentCode = dto.DepartmentCode,
                DepartmentName = dto.DepartmentName,
                ParentDepartmentId = dto.ParentDepartmentId,
                Description = dto.Description,
                AppId = dto.AppId
            };
            await repository.InsertAsync(department1);

            return department1.Id;
        }

        public async Task<bool> DeleteDepartmentAsync(string departmentId, string AppId = null)
        {
            var department = await GetDepartmentAsync(departmentId, AppId);
            if (department == null) return false;
            department.IsDelete = IsOrNotEnum.是;
            department.DeleteTime = DateTime.Now;
            if (AppId.IsNullOrEmpty())
            {
                UserAccountFactory userAccountFactory = await userAccount.GetUserAccountAsync();
                department.DeleteUserId = userAccountFactory.Id;
                department.DeleteUserName = userAccountFactory.UserName;
            }
            else
            {
                department.DeleteUserId =  AppId;
                department.DeleteUserName = AppId;
            }
            await repository.UpdateIncludeAsync(department, new string[]
            {
                nameof(department.IsDelete),
                nameof(department.DeleteTime),
                nameof(department.DeleteUserId),
                nameof(department.DeleteUserName)
            });
            return true;
        }

        public async Task<Department> GetDepartmentAsync(string departmentId, string AppId = null)
        {
            if (departmentId.IsNullOrEmpty()) { return null; }
            if (AppId.IsNullOrEmpty())
            {
                var departmentNoAppId = await repository.DetachedEntities.FirstOrDefaultAsync(s => s.Id == departmentId);
                return departmentNoAppId;
            }

            var department = await repository.DetachedEntities.FirstOrDefaultAsync(s => s.Id == departmentId&&s.AppId==AppId);
            return department;
        }

        public async Task<List<DepartmentTreeDto>> QueryDepartmentsAsync(BaseQDto dto)
        {
            var linq = LinqExpressionExtensions.And<Department>();
            linq = linq.And(s => s.IsDelete == IsOrNotEnum.否);

            var AllDepartments = await repository.DetachedEntities.Where(linq).ToListAsync();

            var TreeDepartments = Department.CreatTree(AllDepartments);

            return TreeDepartments;

        }
        public async Task<string> UpdateDepartmentAsync(DepartmentSDto dto)
        {
            var department = await repository.DetachedEntities.FirstOrDefaultAsync(s => s.Id == dto.Id);
            if (department == null) return string.Empty;

            if (dto.ParentDepartmentId != DepartmentSDto.TOP_DEPARTMENT_ID)
            {
                var parentDepartment = await repository.DetachedEntities.SingleOrDefaultAsync(s => s.Id == dto.ParentDepartmentId);
                if (parentDepartment == null) throw Oops.Oh("上级部门不存在");
                if (department.AppId!=parentDepartment.AppId) throw Oops.Oh("不允许将部门迁移到其他应用中");
            }
            var HasSameNameDepartment = await repository.DetachedEntities.FirstOrDefaultAsync(s => s.DepartmentName == dto.DepartmentName&&s.Id!=dto.Id && s.AppId == dto.AppId);
            if (HasSameNameDepartment != null) throw Oops.Oh("同一应用下部门名称不能重复");

            if(dto.Id==dto.ParentDepartmentId) throw Oops.Oh("上级部门不能选择自己");

            department.DepartmentName = dto.DepartmentName;
            department.Description = dto.Description;
            department.DepartmentCode = dto.DepartmentCode;
            department.ParentDepartmentId = dto.ParentDepartmentId;
            if (department.Id==dto.ParentDepartmentId)
            {
                return string.Empty;
            }
            await repository.UpdateIncludeAsync(department, new string[]
            {
                nameof(department.DepartmentCode),
                nameof(department.ParentDepartmentId),
                nameof(department.DepartmentName),
                nameof(department.Description)

            });
            return department.Id;
        }
        public async Task<IList<Department>> GetDepartmentAsync(IList<string> departmentIds)
        {
            if (!departmentIds.Any()) { return null; }
            var departments = await repository.DetachedEntities.Where(s => departmentIds.Contains(s.Id)).ToListAsync();
            return departments;
        }
        #region 部门与区域关联操作
        public async Task<bool> RemoveRegionFromDepartmentAsync(string departmentId, string regionId, string AppId)
        {
            var result = await entityAssociationDomain.RemoveEntityAssignAsync(departmentId, regionId, AssociationTypeEnum.部门与区域, AppId);
            return result;
        }
        public async Task<bool> AssignRegionToDepartmentAsync(string departmentId, string regionId, string AppId)
        {
            return await entityAssociationDomain.AddEntityAssignAsync(departmentId, regionId, AssociationTypeEnum.部门与区域, AppId);
        }

        public async Task<bool> AssignAppToDepartmentAsync(string departmentId, string AppId)
        {
            var result = await entityAssociationDomain.RemoveEntityAssignAsync(departmentId, AppId, AssociationTypeEnum.部门与应用, AppId);
            return result;
        }

        public async Task<bool> RemoveAppFromDepartmentAsync(string departmentId, string AppId)
        {
            return await entityAssociationDomain.AddEntityAssignAsync(departmentId, AppId, AssociationTypeEnum.部门与应用, AppId);
        }

        public async Task<IList<Department>> GetDepartmentAsync(IList<string> departmentIds, string AppId = null)
        {
            return await repository.DetachedEntities.Where(s => departmentIds.Contains(s.Id)).ToListAsync();
        }

        public async Task<IList<Department>> GetUserDepartmentsAsync(string UserId, string AppId = null)
        {

            var associations = await entityAssociationDomain.GetEntityAssociationsQueryableAsync(
                UserId,
                AppId,
                AssociationTypeEnum.用户与部门);
            var departments = await repository.DetachedEntities.Where(s => associations.Select(b => b.TargetEntityId).Contains(s.Id)&&s.IsDelete==IsOrNotEnum.否).ToListAsync();

            return departments;
        }

        public async Task<bool> MergeAppsFromDepartmentAsync(string departmentId, string departmentAppId, string AppIds)
        {
            var appIdList = AppIds?.Split(',').ToList();
            var HasApps = (await entityAssociationDomain.GetEntityAssociationsQueryableAsync(departmentId, departmentAppId, AssociationTypeEnum.部门与应用)).Select(s => s.TargetEntityId).ToList();
            //查出需要删除的关联
            var RemoveApps = HasApps.Except(appIdList).ToList();
            foreach (var appId in RemoveApps)
            {
                await entityAssociationDomain.RemoveEntityAssignAsync(departmentId, appId, AssociationTypeEnum.部门与应用, departmentAppId);
            }
            //查出需要新增的关联
            var AddApps = appIdList.Except(HasApps).ToList();
            foreach (var appId in AddApps)
            {
                await entityAssociationDomain.AddEntityAssignAsync(departmentId, appId, AssociationTypeEnum.部门与应用, departmentAppId);
            }
            return true;
        }

        public async Task<bool> MergeRegionsFromDepartmentAsync(string departmentId, string departmentAppId, string regionIds)
        {
            //当前选择的
            var regionIdList = regionIds.Split(',').ToList();
            //已经有的
            var HasRegions = (await entityAssociationDomain.GetEntityAssociationsQueryableAsync(departmentId, departmentAppId, AssociationTypeEnum.部门与区域)).Select(s => s.TargetEntityId).ToList();
            //删除未选择的
            var RemoveRegions = HasRegions.Except(regionIdList).ToList();
            foreach (var region in RemoveRegions) {
                await entityAssociationDomain.RemoveEntityAssignAsync(departmentId, region, AssociationTypeEnum.部门与区域, departmentAppId);
            }
            //查出需要新增的关联 RegionIdList元素不在HasRegions中的
            var AddRegions = regionIdList.Except(HasRegions).ToList();
            foreach (var region in AddRegions) {
                await entityAssociationDomain.AddEntityAssignAsync(departmentId, region, AssociationTypeEnum.部门与区域, departmentAppId);
            }
            return true;
        }
        #endregion

    }
}
