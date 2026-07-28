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
using air.cloud.system.service.Services.RoleServices;
using air.cloud.security.common.Base.Dtos;
using air.cloud.security.common.Model;
using air.cloud.system.model.Domains.RoleDomains;
using air.cloud.system.model.Dtos.RoleDtos;
using air.cloud.system.model.Entitys.Roles;

using Air.Cloud.Core.Extensions;
using Air.Cloud.WebApp.FriendlyException;

using Microsoft.AspNetCore.Mvc;

namespace air.cloud.system.service.Impl.RoleServices
{
    [Route("v1/security/system/role-group")]
    public  class RoleGroupService : IRoleGroupService
    {
        private readonly IRoleGroupDomain roleGroupDomain;
        public RoleGroupService(IRoleGroupDomain roleGroupDomain)
        {
            this.roleGroupDomain = roleGroupDomain;
        }

        [HttpGet("clear-roles/{roleGroupId}")]
        public async Task<bool> ClearRoleGroupRolesByRoleGroupIdAsync(string roleGroupId)
        {
            var group = await roleGroupDomain.GetRoleGroupAsync(roleGroupId);
            if (group == null) throw Oops.Oh("角色组不存在");
            return await roleGroupDomain.ClearRoleGroupRolesByRoleGroupIdAsync(group.AppId, roleGroupId);
        }

        [HttpGet("clear-users/{roleGroupId}")]
        public async Task<bool> ClearRoleGroupUsersByRoleGroupIdAsync(string roleGroupId)
        {
            var group = await roleGroupDomain.GetRoleGroupAsync(roleGroupId);
            if (group == null) throw Oops.Oh("角色组不存在");
            return await roleGroupDomain.ClearRoleGroupUsersByRoleGroupIdAsync(group.AppId, roleGroupId);
        }

        [HttpPost("save")]
        public Task<bool> SaveRoleGroupAsync(RoleGroupSDto dto)
        {
            if (dto.Id.IsNullOrEmpty())
            {
                return roleGroupDomain.CreateRoleGroupAsync(dto);

            }
            else
            {
                return roleGroupDomain.UpdateRoleGroupAsync(dto);
            }
        }
        [HttpDelete("remove/{roleGroupId}")]
        public async Task<bool> DeleteRoleGroupAsync(string roleGroupId)
        {
            var group=await roleGroupDomain.GetRoleGroupAsync(roleGroupId);
            if (group == null) throw Oops.Oh("角色组不存在");
            return await roleGroupDomain.DeleteRoleGroupAsync(group.AppId, group.Id);
        }
        [HttpGet("detail/{roleGroupId}")]
        public async Task<RoleGroup> GetRoleGroupAsync(string roleGroupId)
        {
           return await roleGroupDomain.GetRoleGroupAsync(roleGroupId);
        }
        [HttpGet("merge-roles/{roleGroupId}/{targetGroupId}")]
        public async Task<bool> MergeRoleGroupRolesToNewRoleGroupAsync(string RoleGroupId, string TargetGroupId)
        {
            var roleGroup = await roleGroupDomain.GetRoleGroupAsync(RoleGroupId);
            if (roleGroup == null) throw Oops.Oh("角色组不存在");
            var targetRoleGroup = await roleGroupDomain.GetRoleGroupAsync(TargetGroupId);
            if (targetRoleGroup == null) throw Oops.Oh("角色组不存在");
            if (roleGroup.AppId!=targetRoleGroup.AppId) throw Oops.Oh($"{roleGroup.RoleGroupName}与{targetRoleGroup.RoleGroupName}不在同一个应用下");
            return await roleGroupDomain.MergeRoleGroupRolesToNewRoleGroupAsync(roleGroup.AppId, RoleGroupId, TargetGroupId);
        }
        [HttpGet("merge-users/{roleGroupId}/{targetGroupId}")]
        public async Task<bool> MergeRoleGroupUsersToNewRoleGroupAsync(string RoleGroupId, string TargetGroupId)
        {
            var roleGroup = await roleGroupDomain.GetRoleGroupAsync(RoleGroupId);
            if (roleGroup == null) throw Oops.Oh("角色组不存在");
            var targetRoleGroup = await roleGroupDomain.GetRoleGroupAsync(TargetGroupId);
            if (targetRoleGroup == null) throw Oops.Oh("角色组不存在");
            if (roleGroup.AppId != targetRoleGroup.AppId) throw Oops.Oh($"{roleGroup.RoleGroupName}与{targetRoleGroup.RoleGroupName}不在同一个应用下");
            return await roleGroupDomain.MergeRoleGroupUsersToNewRoleGroupAsync(roleGroup.AppId, RoleGroupId, TargetGroupId);
        }

        [HttpPost("query")]
        public async Task<PageList<RoleGroup>> QueryRoleGroupsAsync(BaseQDto dto)
        {
          return await roleGroupDomain.QueryRoleGroupsAsync(dto);
        }

        [HttpGet("list/{appId}")]
        public async Task<List<RoleGroupSelectRDto>> ListAllRoleGroupsAsync(string AppId)
        {
            return await roleGroupDomain.ListAllRoleGroupsAsync(AppId);
        }
    }
}
