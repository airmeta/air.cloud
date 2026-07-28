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
using air.cloud.system.service.Services.UserServices;
using air.cloud.security.common.Model;
using air.cloud.system.model.Domains.OrganizationDomains;
using air.cloud.system.model.Domains.RoleDomains;
using air.cloud.system.model.Domains.UserDomains;
using air.cloud.system.model.Dtos.UserDtos;
using air.cloud.system.model.Entitys.Users;

using Air.Cloud.Core.Extensions;
using Air.Cloud.WebApp.FriendlyException;

using Mapster;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace air.cloud.system.service.Impl.UserServices
{
    [Route("v1/security/system/user")]
    public  class UserService : IUserService
    {
        private readonly IUserDomain userDomain;
        private readonly IRoleDomain roleDomain;
        private readonly IRoleGroupDomain roleGroupDomain;
        private readonly IDepartmentDomain departmentDomain;
        private readonly IAssignmentDomain assignmentDomain;
        private readonly IUserGroupDomain userGroupDomain;
        private readonly IHttpContextAccessor httpContextAccessor;
        public UserService(IUserDomain userDomain, IRoleDomain roleDomain, 
            IRoleGroupDomain roleGroupDomain, 
            IDepartmentDomain departmentDomain,
            IUserGroupDomain userGroupDomain,
            IAssignmentDomain assignmentDomain,
            IHttpContextAccessor httpContextAccessor)
        {
             this.userDomain = userDomain;
             this.roleDomain = roleDomain;
             this.roleGroupDomain = roleGroupDomain;
             this.departmentDomain = departmentDomain;
             this.userGroupDomain = userGroupDomain;
            this.httpContextAccessor = httpContextAccessor;
            this.assignmentDomain = assignmentDomain;
        }
     
        [HttpPost("remove/{userId}")]
       
        public async Task<bool> DeleteUserAsync(string userId)
        {
            var user = await userDomain.GetUserAsync(userId);
            if (user == null) throw Oops.Oh("用户信息为空");
            return await userDomain.DeleteUserAsync(userId);
        }
        [HttpGet("detail/{userId}")]
        public async Task<UserRDto> GetUserAsync(string userId)
        {
            var user = await userDomain.GetUserAsync(userId);
            if (user == null) throw Oops.Oh("用户信息为空");
            UserRDto dto=user.Adapt<UserRDto>();
            dto.RoleIds = string.Join(",",(await roleDomain.GetUserRoleAsync(userId)).Select(s => s.Id).ToList());
            dto.DepartmentIds = string.Join(",", (await departmentDomain.GetUserDepartmentsAsync(userId)).Select(s => s.Id).ToList());
            dto.AssignmentIds = string.Join(",", (await assignmentDomain.GetUserAssignmentsAsync(userId)).Select(s => s.Id).ToList());
            return dto; 
        }
        [HttpPost("save")]
        public async Task<bool> SaveUserAsync(UserSDto dto)
        {
            string AppId= httpContextAccessor.HttpContext.Request.Headers["AppId"];
            if (AppId.IsNullOrEmpty())
            {
                throw Oops.Oh("AppId不能为空");
            }
            string UserId = string.Empty;
            if (dto.Id.IsNullOrEmpty())
            {
                UserId= await userDomain.CreateUserAsync(dto, AppId);
            }
            else
            {
                UserId = await userDomain.UpdateUserAsync(dto);
            }
            if (!UserId.IsNullOrEmpty())
            {
                if (!dto.DepartmentIds.IsNullOrEmpty())
                {
                    await userDomain.GiveUserDepartmentsAsync(UserId, dto.DepartmentIds);
                }
                if (!dto.RoleIds.IsNullOrEmpty())
                {
                    await userDomain.GiveUserRolesAsync(UserId, dto.RoleIds);
                }
                if (!dto.AssignmentIds.IsNullOrEmpty()) {
                    await userDomain.GiveUserAssignmentsAsync(UserId, dto.AssignmentIds);
                }
            }

            return !string.IsNullOrEmpty(UserId);
        }
        [HttpPost("query")]
        public async Task<PageList<UserRDto>> QueryUsersAsync(UserQDto baseQDto)
        {
            return await userDomain.QueryUsersAsync(baseQDto);
        }

        [HttpGet("remove-role/{userId}/{roleId}")]
        public async Task<bool> RemoveRoleFromUserAsync(string userId, string roleId)
        {
            var user=await userDomain.GetUserAsync(userId);
            if (user == null) throw Oops.Oh("用户信息为空");
            var role=await roleDomain.GetRoleAsync(roleId);
            if (role == null) throw Oops.Oh("角色信息为空");
            return await userDomain.RemoveRoleFromUserAsync(role.AppId,userId, roleId);
        }

        [HttpGet("remove-role-group/{userId}/{roleGroupId}")]
        public async Task<bool> RemoveRoleGroupFromUserAsync(string userId, string roleGroupId)
        {
            var user = await userDomain.GetUserAsync(userId);
            if (user == null) throw Oops.Oh("用户信息为空");
            var roleGroup = await roleGroupDomain.GetRoleGroupAsync(roleGroupId);
            if (roleGroup == null) throw Oops.Oh("角色组信息为空");
            return await userDomain.RemoveRoleGroupFromUserAsync(roleGroup.AppId, userId, roleGroupId);
        }


        [HttpGet("join-department/{userId}/{departmentId}")]
        public async Task<bool> UserJoinToDepartmentAsync(string userId, string departmentId)
        {
            var user=await GetUserAsync(userId);
            var department= await departmentDomain.GetDepartmentAsync(departmentId);
            if (department == null) throw Oops.Oh("部门信息为空");
            return await userDomain.UserJoinToDepartmentAsync(department.AppId, userId, departmentId);
        }
        [HttpGet("join-user-group/{userId}/{groupId}")]
        public async Task<bool> UserJoinToUserGroupAsync(string userId, string groupId)
        {
           var user=await GetUserAsync(userId);

            var userGroup= await userGroupDomain.GetUserGroupAsync(groupId);

            return await userDomain.UserJoinToUserGroupAsync(userGroup.AppId, userId, groupId);
        }
        [HttpGet("leave-department/{userId}/{departmentId}")]
        public async Task<bool> UserLeaveFromDepartmentAsync(string userId, string departmentId)
        {
            var user = await GetUserAsync(userId);
            var department = await departmentDomain.GetDepartmentAsync(departmentId);
            if (department == null) throw Oops.Oh("部门信息为空");
            return await userDomain.UserLeaveFromDepartmentAsync(department.AppId, userId, departmentId);

        }
        [HttpGet("leave-user-group/{userId}/{groupId}")]
        public async Task<bool> UserLeaveFromUserGroupAsync(string userId, string groupId)
        {
            var user = await GetUserAsync(userId);
            var userGroup = await userGroupDomain.GetUserGroupAsync(groupId);
            if (userGroup == null) throw Oops.Oh("用户组信息为空");
            return await userDomain.UserLeaveFromUserGroupAsync(userGroup.AppId, userId, groupId);
        }
        [HttpGet("assign-role-group/{userId}/{roleGroupId}")]
        public async Task<bool> AssignRoleGroupToUserAsync(string userId, string roleGroupId)
        {
            var user = await GetUserAsync(userId);
            var roleGroup = await roleGroupDomain.GetRoleGroupAsync(roleGroupId);
            if (roleGroup == null) throw Oops.Oh("角色组信息为空");
            return await userDomain.AssignRoleGroupToUserAsync(roleGroup.AppId, userId, roleGroupId);
        }
        [HttpGet("assign-role/{userId}/{roleId}")]
        public async Task<bool> AssignRoleToUserAsync(string userId, string roleId)
        {
            var user = await GetUserAsync(userId);
            var role = await roleDomain.GetRoleAsync(roleId);
            if (role == null) throw Oops.Oh("角色信息为空");
            return await userDomain.AssignRoleToUserAsync(role.AppId, userId, roleId);
        }

    }
}
