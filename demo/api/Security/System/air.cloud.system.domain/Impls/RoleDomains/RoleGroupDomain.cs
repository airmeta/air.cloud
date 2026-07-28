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
using air.cloud.security.common.Enums;
using air.cloud.security.common.Extensions;
using air.cloud.security.common.Model;
using air.cloud.system.model.Domains.EntityAssociationDomains;
using air.cloud.system.model.Domains.RoleDomains;
using air.cloud.system.model.Dtos.RoleDtos;
using air.cloud.system.model.Entitys.Roles;

using Air.Cloud.Core.Extensions;
using Air.Cloud.Core.Extensions.Linqs;
using Air.Cloud.EntityFrameWork.Core.Repositories;

using Microsoft.EntityFrameworkCore;

namespace air.cloud.system.domain.Impls.RoleDomains
{
    public  class RoleGroupDomain : IRoleGroupDomain
    {
        private readonly IEntityAssociationDomain entityAssociationDomain;  

        private readonly IRepository<RoleGroup> repository;

        public RoleGroupDomain(IRepository<RoleGroup> repository,IEntityAssociationDomain entityAssociationDomain)
        {
            this.repository= repository;
            this.entityAssociationDomain= entityAssociationDomain;
        }

        #region 角色组的基本操作
        public async Task<bool> CreateRoleGroupAsync(RoleGroupSDto dto)
        {
            var roleGroup = await repository.Change<RoleGroup>()
                 .DetachedEntities.FirstOrDefaultAsync(s => s.AppId == dto.AppId && s.RoleGroupName == dto.RoleGroupName);
            if (roleGroup != null) return true;

            roleGroup = new RoleGroup
            {
                AppId = dto.AppId,
                RoleGroupName = dto.RoleGroupName
            };
            await repository.Change<RoleGroup>().InsertAsync(roleGroup);
            return true;    
        }

        public async Task<bool> DeleteRoleGroupAsync(string AppId, string roleGroupId)
        {
            var roleGroup = await repository.Change<RoleGroup>()
                 .DetachedEntities.FirstOrDefaultAsync(s => s.AppId == AppId && s.Id == roleGroupId);
            if (roleGroup == null) return true;
            await repository.Change<RoleGroup>().DeleteAsync(roleGroup);
            return true;
        }

        public async Task<bool> UpdateRoleGroupAsync(RoleGroupSDto dto)
        {
            var roleGroup = await repository.Change<RoleGroup>()
                .DetachedEntities.FirstOrDefaultAsync(s => s.AppId == dto.AppId && s.RoleGroupName == dto.RoleGroupName);
            if (roleGroup == null) return true;

            roleGroup.RoleGroupName= dto.RoleGroupName;
            roleGroup.Description= dto.Description;
            await repository.UpdateIncludeAsync(roleGroup,new string[]
            {
                nameof(RoleGroup.RoleGroupName),
                nameof(RoleGroup.Description)
            });
            return true;
        }

        public Task<PageList<RoleGroup>> QueryRoleGroupsAsync(BaseQDto dto)
        {
            var linq = LinqExpressionExtensions.And<RoleGroup>();
            linq = linq.And(s => s.AppId == dto.AppId);
            if (!string.IsNullOrEmpty(dto.Info))
            {
                linq = linq.And(s => s.RoleGroupName.Contains(dto.Info));
            }
            return repository.Change<RoleGroup>()
                .DetachedEntities
                .Where(linq)
                .OrderByDescending(s => s.CreateTime).ToPageListAsync(dto.Page, dto.Limit);
        }

        public async Task<RoleGroup> GetRoleGroupAsync(string roleGroupId, string AppId=null)
        {
            if (AppId.IsNullOrEmpty())
            {
                var roleGroupNoAppId = await repository.Change<RoleGroup>()
                     .DetachedEntities.FirstOrDefaultAsync(s =>s.Id == roleGroupId);
                return roleGroupNoAppId;
            }
            var roleGroup = await repository.Change<RoleGroup>()
                 .DetachedEntities.FirstOrDefaultAsync(s => s.AppId == AppId && s.Id == roleGroupId);
            return roleGroup;   
        }
        public async Task<List<RoleGroupSelectRDto>> ListAllRoleGroupsAsync(string AppId)
        {
            var roleGroup = await repository.Change<RoleGroup>()
                .DetachedEntities.Where(s => s.AppId == AppId ).OrderByDescending(s=>s.CreateTime).Select(s=>new RoleGroupSelectRDto()
                {
                    Description=s.Description,
                    Name=s.RoleGroupName,
                    Id=s.Id 
                }).ToListAsync();
            return roleGroup;

        }
        #endregion



        #region 角色组的关联操作



        public async Task<bool> MergeRoleGroupUsersToNewRoleGroupAsync(string AppId, string RoleGroupId, string TargetGroupId)
        {
            return await entityAssociationDomain.MergeEntityAssignsAsync(RoleGroupId, TargetGroupId, AssociationTypeEnum.用户与用户组, AppId);

        }
        public async Task<bool> ClearRoleGroupUsersByRoleGroupIdAsync(string AppId, string roleGroupId)
        {
            return await entityAssociationDomain.TruncateEntityAssignAsync(roleGroupId, AppId, AssociationTypeEnum.用户与角色组);
        }


        public async Task<bool> MergeRoleGroupRolesToNewRoleGroupAsync(string AppId, string RoleGroupId, string TargetGroupId)
        {
            return await entityAssociationDomain.MergeEntityAssignsAsync(RoleGroupId, TargetGroupId, AssociationTypeEnum.角色与角色组, AppId);
        }


        public async Task<bool> ClearRoleGroupRolesByRoleGroupIdAsync(string AppId, string roleGroupId)
        {
            return await entityAssociationDomain.TruncateEntityAssignAsync(roleGroupId, AppId, AssociationTypeEnum.角色与角色组);
        }


        #endregion



    }
}
