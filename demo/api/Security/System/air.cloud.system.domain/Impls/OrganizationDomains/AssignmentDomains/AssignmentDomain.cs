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
using air.cloud.security.common.Enums;
using air.cloud.security.common.Extensions;
using air.cloud.security.common.Model;
using air.cloud.system.model.Domains.EntityAssociationDomains;
using air.cloud.system.model.Domains.OrganizationDomains;
using air.cloud.system.model.Dtos.OrganizationDtos.AssignmentDtos;
using air.cloud.system.model.Entitys.Organization;

using Air.Cloud.Core.App;
using Air.Cloud.Core.Extensions;
using Air.Cloud.Core.Extensions.Linqs;
using Air.Cloud.EntityFrameWork.Core.Repositories;
using Air.Cloud.WebApp.FriendlyException;

using Mapster;

using Microsoft.EntityFrameworkCore;

namespace air.cloud.system.domain.Impls.OrganizationDomains.AssignmentDomains
{
    public  class AssignmentDomain : IAssignmentDomain
    {
        private readonly IRepository<Assignment> repository;
        private readonly IUserAccountStore userAccount;
        private readonly IDepartmentDomain departmentDomain;
        private readonly IEntityAssociationDomain entityAssociationDomain;
        public AssignmentDomain(IRepository<Assignment> repository, IUserAccountStore userAccount,
            IEntityAssociationDomain entityAssociationDomain,
            IDepartmentDomain departmentDomain) { 
            this.repository = repository;   
            this.userAccount = userAccount;
            this.departmentDomain = departmentDomain;
            this.entityAssociationDomain = entityAssociationDomain;
        }

        public async Task<bool> CheckAssignmentExistsAsync(string asgId, string AppId)
        {
           return await repository.Where(x => x.Id == asgId && x.AppId == AppId && x.IsDelete== IsOrNotEnum.否).AnyAsync();
        }

        public async Task<string> CreateAssignmentAsync(AssignmentSDto dto, string AppId)
        {
            var departmentInfo=await departmentDomain.GetDepartmentAsync(dto.DepartmentId);
            if(departmentInfo==null) throw Oops.Oh("部门信息不存在");
            if (AppId.IsNullOrEmpty()) AppId= departmentInfo.AppId;
            if (departmentInfo.AppId != AppId) throw Oops.Oh("部门所属应用与职位所属应用不符");
            Assignment assignment = new Assignment()
            {
                Name = dto.Name,
                Description = dto.Description,
                DepartmentId = dto.DepartmentId,
                Id=AppCore.Guid(),
                AppId = AppId
           };
           await repository.InsertAsync(assignment);
           return assignment.Id;
        }

        public async Task<bool> DeleteAssignmentAsync(string AssignmentId,string AppId)
        {
            var assignment = await repository.Where(x => x.Id == AssignmentId&&x.AppId==AppId).FirstOrDefaultAsync();
            if (assignment == null) return true;
            assignment.IsDelete= IsOrNotEnum.是;

            assignment.DeleteTime = DateTime.Now;
            if (AppId.IsNullOrEmpty())
            {
                UserAccountFactory userAccountFactory = await userAccount.GetUserAccountAsync();
                assignment.DeleteUserId = userAccountFactory.Id;
                assignment.DeleteUserName = userAccountFactory.UserName;
            }
            else
            {
                assignment.DeleteUserId = AppId;
                assignment.DeleteUserName = AppId;
            }
            await repository.UpdateIncludeAsync(assignment, new string[]
            {
                nameof(assignment.IsDelete),
                nameof(assignment.DeleteUserId),
                nameof(assignment.DeleteTime),
                nameof(assignment.DeleteUserName)
            });

            #region 清理任职信息

            await entityAssociationDomain.TruncateEntityAssignAsync(assignment.Id, AppId, AssociationTypeEnum.用户与任职);

            #endregion

            return true;
        }

        public async Task<AssignmentSDto> GetAssignmentDetailAsync(string asgId, string AppId = null)
        {

            if (asgId.IsNullOrEmpty()) { return null; }
            if (AppId.IsNullOrEmpty())
            {
                var assignmentNoAppId = await repository.DetachedEntities.FirstOrDefaultAsync(s => s.Id == asgId);
                if (assignmentNoAppId == null) throw Oops.Oh("任职信息不存在");
                if (assignmentNoAppId.IsDelete == IsOrNotEnum.是)
                {
                    return null;
                }
                return assignmentNoAppId.Adapt<AssignmentSDto>();
            }

            var assignment = await repository.DetachedEntities.FirstOrDefaultAsync(s => s.Id == asgId && s.AppId == AppId);
            if (assignment == null) throw Oops.Oh("任职信息不存在");

            if (assignment.IsDelete==IsOrNotEnum.是)
            {
                return null;
            }

            return assignment.Adapt<AssignmentSDto>();

        }

        public async Task<IList<Assignment>> GetAssignmentsAsync(IList<string> assignmentIds)
        {
           return await repository.DetachedEntities.Where(s => assignmentIds.Contains(s.Id) && s.IsDelete == IsOrNotEnum.否).ToListAsync();
        }

        public async Task<IList<AssignmentSDto>> GetUserAssignmentsAsync(string userId, string AppId = null)
        {
            var associations = await entityAssociationDomain.GetEntityAssociationsQueryableAsync(
               userId,
               AppId,
               AssociationTypeEnum.用户与任职);
            var departments = await repository.DetachedEntities.Where(s => associations.Select(b => b.TargetEntityId).Contains(s.Id) && s.IsDelete == IsOrNotEnum.否)
                .Select(s => new AssignmentSDto
                {
                    DepartmentId = s.DepartmentId,
                    Description = s.Description,
                    Id = s.Id,
                    Name = s.Name,
                    AppId = s.AppId
                }).ToListAsync();
            return departments;

        }

        public async Task<PageList<AssignmentSDto>> QueryAssignmentAsync(AssignmentQDto dto)
        {
            var linq = LinqExpressionExtensions.And<Assignment>();
            linq = linq.And(s => s.IsDelete == IsOrNotEnum.否);
            if (!dto.Info.IsNullOrEmpty())
            {
                linq=linq.And(s => s.Name.Contains(dto.Info)|| s.Description.Contains(dto.Info));
            }

            if (!dto.DepartmentId.IsNullOrEmpty())
            {
                linq=linq.And(s => s.DepartmentId == dto.DepartmentId);
            }
            var assignments =repository.DetachedEntities.Where(linq).Select(s=>new AssignmentSDto
            {
                DepartmentId=s.DepartmentId,
                Description=s.Description,
                Id=s.Id,
                Name=s.Name
            }).AsQueryable();

            return await assignments.ToPageListAsync<AssignmentSDto>(dto.Page,dto.Limit);
        }

        public async Task<string> UpdateAssignmentAsync(AssignmentSDto dto, string AppId)
        {
            var assignment = await repository.Where(x => x.Id == dto.Id && x.AppId == AppId).FirstOrDefaultAsync();
            if (assignment == null) return string.Empty;
            assignment.Name = dto.Name;
            assignment.Description = dto.Description;
            assignment.DepartmentId = dto.DepartmentId;
            await repository.UpdateIncludeAsync(assignment, new string[]
            {
                nameof(assignment.Name),
                nameof(assignment.Description),
                nameof(assignment.DepartmentId)
            });
            return assignment.Id;
        }
    }
}
