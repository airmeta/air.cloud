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
using air.cloud.system.model.Domains.ActionPermissionDomains;
using air.cloud.system.model.Domains.MenuDomains;
using air.cloud.system.model.Domains.RoleDomains;
using air.cloud.system.model.Dtos.MenuDtos;
using air.cloud.system.model.Dtos.RoleDtos;
using air.cloud.system.model.Entitys.Associations;
using air.cloud.system.model.Entitys.Roles;

using Air.Cloud.Core.Extensions;
using Air.Cloud.WebApp.FriendlyException;

using Microsoft.AspNetCore.Mvc;

namespace air.cloud.system.service.Impl.RoleServices
{
    [Route("v1/security/system/role")]
    public  class RoleService : IRoleService
    {
        private readonly IRoleDomain roleDomain;
        private readonly IActionPermissionDomain actionPermissionDomain;
        private readonly IMenuDomain menuDomain;
        private readonly IRoleGroupDomain roleGroupDomain;

        public RoleService(IRoleDomain roleDomain, IActionPermissionDomain actionPermissionDomain, IMenuDomain menuDomain, IRoleGroupDomain roleGroupDomain)
        {
            this.actionPermissionDomain= actionPermissionDomain;
            this.menuDomain= menuDomain;
            this.roleGroupDomain= roleGroupDomain;                                                                                  
            this.roleDomain= roleDomain;
        }

        [HttpPost("copy")]
        public async Task<bool> CopyRoleThenCreateNewRoleAsync(RoleSDto dto)
        {
            return await roleDomain.CopyRoleThenCreateNewRoleAsync(dto);
        }
        [HttpPost("save")]
        public async Task<bool> SaveRoleAsync(RoleSDto dto)
        {
            string RoleId = string.Empty;
            if (dto.Id.IsNullOrEmpty())
            {
                RoleId= await roleDomain.CreateRoleAsync(dto);
            }
            else
            {
                RoleId = await roleDomain.UpdateRoleAsync(dto);
            }
            return !RoleId.IsNullOrEmpty();
        }

        [HttpDelete("remove/{roleId}")]
        public async Task<bool> DeleteRoleAsync(string roleId)
        {
            var role= await roleDomain.GetRoleAsync(roleId);
            if (role == null) throw Oops.Oh("角色不存在");
            return await roleDomain.DeleteRoleAsync(role.AppId, roleId);
        }
        [HttpGet("detail/{roleId}")]
        public async Task<Role> GetRoleAsync(string roleId)
        {
           return await roleDomain.GetRoleAsync(roleId);
        }
        [HttpGet("join-action/{roleId}")]
        public async Task<bool> JoinActionPermissionToRoleAsync(string roleId, string permissionId)
        {
           var role=await roleDomain.GetRoleAsync(roleId);
           if (role == null) throw Oops.Oh("角色不存在");
           var actionPermission= await actionPermissionDomain.GetActionPermissionAsync(permissionId);
           if (actionPermission == null) throw Oops.Oh("动作权限不存在");
           if (actionPermission.AppId!=role.AppId) throw Oops.Oh("角色所在应用与权限所在应用不匹配,无法执行此操作!");
           return await roleDomain.JoinActionPermissionToRoleAsync(role.AppId, roleId, permissionId);
        }
        [HttpGet("join-menu/{roleId}/{menuId}")]
        public async Task<bool> JoinMenuToRoleAsync(string roleId, string menuId)
        {
            var role = await roleDomain.GetRoleAsync(roleId);
            if (role == null) throw Oops.Oh("角色不存在");
            var menu = await menuDomain.GetMenuAsync(menuId);
            if (menu == null) throw Oops.Oh("菜单不存在");
            if (menu.AppId != role.AppId) throw Oops.Oh("角色所在应用与菜单所在应用不匹配,无法执行此操作!");
            return await roleDomain.JoinMenuToRoleAsync(menu.AppId,roleId, menuId);
        }
        [HttpGet("join-group/{roleId}/{roleGroupId}")]
        public async Task<bool> JoinRoleToRoleGroupAsync(string roleId, string roleGroupId)
        {
            var role = await roleDomain.GetRoleAsync(roleId);
            if (role == null) throw Oops.Oh("角色不存在");
            var roleGroup = await roleGroupDomain.GetRoleGroupAsync(roleGroupId);
            if (roleGroup == null) throw Oops.Oh("角色组不存在");
            if (roleGroup.AppId != role.AppId) throw Oops.Oh("角色所在应用与角色组所在应用不匹配,无法执行此操作!");
            return await roleDomain.JoinRoleToRoleGroupAsync(roleGroup.AppId, roleId, roleGroupId);
        }
        [HttpGet("leave-group/{roleId}/{roleGroupId}")]
        public async Task<bool> LeaveRoleFromRoleGroupAsync(string roleId, string roleGroupId)
        {
            var role = await roleDomain.GetRoleAsync(roleId);
            if (role == null) throw Oops.Oh("角色不存在");
            var roleGroup = await roleGroupDomain.GetRoleGroupAsync(roleGroupId);
            if (roleGroup == null) throw Oops.Oh("角色组不存在");
            if (roleGroup.AppId != role.AppId) throw Oops.Oh("角色所在应用与角色组所在应用不匹配,无法执行此操作!");
            return await roleDomain.LeaveRoleFromRoleGroupAsync(roleGroup.AppId, roleId, roleGroupId);
        }

        [HttpPost("query")]
        public async Task<PageList<Role>> QueryRolesAsync(BaseQDto dto)
        {
           return await roleDomain.QueryRolesAsync(dto);
        }

        [HttpGet("remove-action/{roleId}/{permissionId}")]
        public async Task<bool> RemoveActionPermissionFromRoleAsync(string roleId, string permissionId)
        {
            var role = await roleDomain.GetRoleAsync(roleId);
            if (role == null) throw Oops.Oh("角色不存在");
            var actionPermission = await actionPermissionDomain.GetActionPermissionAsync(permissionId);
            if (actionPermission == null) throw Oops.Oh("动作权限不存在");
            if (actionPermission.AppId != role.AppId) throw Oops.Oh("角色所在应用与权限所在应用不匹配,无法执行此操作!");
            return await roleDomain.RemoveActionPermissionFromRoleAsync(actionPermission.AppId, roleId, permissionId);
        }

        [HttpGet("remove-menu/{roleId}/{menuId}")]
        public async Task<bool> RemoveMenuFromRoleAsync(string roleId, string menuId)
        {
            var role = await roleDomain.GetRoleAsync(roleId);
            if (role == null) throw Oops.Oh("角色不存在");
            var menu = await menuDomain.GetMenuAsync(menuId);
            if (menu == null) throw Oops.Oh("菜单不存在");
            if (menu.AppId != role.AppId) throw Oops.Oh("角色所在应用与菜单所在应用不匹配,无法执行此操作!");
            return await roleDomain.RemoveMenuFromRoleAsync(role.AppId, roleId, menuId);
        }

        [HttpGet("role-menu/{RoleId}")]
        public async Task<IList<RoleMenusRDto>> GetRoleMenuAsync(string RoleId)
        {
            return await roleDomain.GetRoleMenuAsync(RoleId);
        }

        [HttpPost("batch-change-menu")]
        public Task<bool> BatchChangeRoleMenuAsync(BatchChangeRoleMenuDto dto)
        {
            return roleDomain.BatchChangeRoleMenuAsync(dto);
        }
    }
}
