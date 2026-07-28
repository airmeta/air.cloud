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
using air.cloud.security.common.Model;
using air.cloud.system.model.Dtos.UserDtos;
using air.cloud.system.model.Entitys.Users;

using Air.Cloud.Core.Standard.DynamicServer;

namespace air.cloud.system.service.Services.UserServices
{
    /// <summary>
    /// <para>zh-cn:用户服务接口</para>
    /// <para>en-us:User Service Interface</para>
    /// </summary>
    public interface IUserService : IDynamicService
    {
        /// <summary>
        /// <para>zh-cn:保存或更新用户信息</para>
        /// <para>en-us:Save or update user information</para>
        /// </summary>
        /// <param name="dto">
        ///  <para>zh-cn:用户信息传输对象</para>
        ///  <para>en-us:User Information Transfer Object</para>
        /// </param>
        /// <returns>
        ///  <para>zh-cn:返回保存结果，true表示成功，false表示失败</para>
        ///  <para>en-us:Returns the save result, true indicates success, false indicates failure</para>
        /// </returns>
        public Task<bool> SaveUserAsync(UserSDto dto);

        /// <summary>
        /// <para>zh-cn:删除用户</para>
        /// <para>en-us:Delete user</para>
        /// </summary>
        /// <param name="userId">
        ///  <para>zh-cn:用户ID</para>
        ///  <para>en-us:User Id</para>
        /// </param>
        /// <returns>
        ///  <para>zh-cn:返回删除结果，true表示成功，false表示失败</para>
        ///  <para>en-us:Returns the deletion result, true indicates success, false indicates failure</para>
        /// </returns>
        public Task<bool> DeleteUserAsync(string userId);

        /// <summary>
        /// <para>zh-cn:查询用户列表</para>
        /// <para>en-us:Query user list</para>
        /// </summary>
        /// <param name="baseQDto">
        ///  <para>zh-cn:用户查询参数</para>
        ///  <para>en-us:User query parameters</para>
        /// </param>
        /// <returns>
        ///  <para>zh-cn:分页的用户列表</para>
        ///  <para>en-us:Paged list of users</para>
        /// </returns>
        public Task<PageList<UserRDto>> QueryUsersAsync(UserQDto baseQDto);

        /// <summary>
        /// <para>zh-cn:获取用户详情</para>
        /// <para>en-us:Get user details</para>
        /// </summary>
        /// <param name="userId">
        ///  <para>zh-cn:用户ID</para>
        ///  <para>en-us:User Id</para>
        /// </param>
        /// <returns>
        ///  <para>zh-cn:返回用户详情传输对象</para>
        ///  <para>en-us:Returns user detail transfer object</para>
        /// </returns>
        public Task<UserRDto> GetUserAsync(string userId);

        #region 用户与角色关联操作

        /// <summary>
        /// <para>zh-cn:分配角色给用户</para>
        /// <para>en-us:Assign role to user</para>
        /// </summary>
        /// <param name="userId">
        ///  <para>zh-cn:用户ID</para>
        ///  <para>en-us:User Id</para>
        /// </param>
        /// <param name="roleId">
        ///  <para>zh-cn:角色ID</para>
        ///  <para>en-us:Role Id</para>
        /// </param>
        /// <returns>
        ///  <para>zh-cn:返回分配结果，true表示成功，false表示失败</para>
        ///  <para>en-us:Returns the assignment result, true indicates success, false indicates failure</para>
        /// </returns>
        public Task<bool> AssignRoleToUserAsync(string userId, string roleId);

        /// <summary>
        /// <para>zh-cn:移除用户的角色</para>
        /// <para>en-us:Remove role from user</para>
        /// </summary>
        /// <param name="userId">
        ///  <para>zh-cn:用户ID</para>
        ///  <para>en-us:User Id</para>
        /// </param>
        /// <param name="roleId">
        ///  <para>zh-cn:角色ID</para>
        ///  <para>en-us:Role Id</para>
        /// </param>
        /// <returns>
        ///  <para>zh-cn:返回移除结果，true表示成功，false表示失败</para>
        ///  <para>en-us:Returns the removal result, true indicates success, false indicates failure</para>
        /// </returns>
        public Task<bool> RemoveRoleFromUserAsync(string userId, string roleId);

        #endregion

        #region 用户与角色组关联操作

        /// <summary>
        /// <para>zh-cn:分配角色组给用户</para>
        /// <para>en-us:Assign role group to user</para>
        /// </summary>
        /// <param name="userId">
        ///  <para>zh-cn:用户ID</para>
        ///  <para>en-us:User Id</para>
        /// </param>
        /// <param name="roleGroupId">
        ///  <para>zh-cn:角色组ID</para>
        ///  <para>en-us:Role Group Id</para>
        /// </param>
        /// <returns>
        ///  <para>zh-cn:返回分配结果，true表示成功，false表示失败</para>
        ///  <para>en-us:Returns the assignment result, true indicates success, false indicates failure</para>
        /// </returns>
        public Task<bool> AssignRoleGroupToUserAsync(string userId, string roleGroupId);

        /// <summary>
        /// <para>zh-cn:移除用户的角色组</para>
        /// <para>en-us:Remove role group from user</para>
        /// </summary>
        /// <param name="userId">
        ///  <para>zh-cn:用户ID</para>
        ///  <para>en-us:User Id</para>
        /// </param>
        /// <param name="roleGroupId">
        ///  <para>zh-cn:角色组ID</para>
        ///  <para>en-us:Role Group Id</para>
        /// </param>
        /// <returns>
        ///  <para>zh-cn:返回移除结果，true表示成功，false表示失败</para>
        ///  <para>en-us:Returns the removal result, true indicates success, false indicates failure</para>
        /// </returns>
        public Task<bool> RemoveRoleGroupFromUserAsync(string userId, string roleGroupId);

        #endregion

        #region 用户与部门关联操作

        /// <summary>
        /// <para>zh-cn:让用户加入部门</para>
        /// <para>en-us:Make user join a department</para>
        /// </summary>
        /// <param name="userId">
        ///  <para>zh-cn:用户ID</para>
        ///  <para>en-us:User Id</para>
        /// </param>
        /// <param name="departmentId">
        ///  <para>zh-cn:部门ID</para>
        ///  <para>en-us:Department Id</para>
        /// </param>
        /// <returns>
        ///  <para>zh-cn:返回加入结果，true表示成功，false表示失败</para>
        ///  <para>en-us:Returns the join result, true indicates success, false indicates failure</para>
        /// </returns>
        public Task<bool> UserJoinToDepartmentAsync(string userId, string departmentId);

        /// <summary>
        /// <para>zh-cn:让用户离开部门</para>
        /// <para>en-us:Make user leave a department</para>
        /// </summary>
        /// <param name="userId">
        ///  <para>zh-cn:用户ID</para>
        ///  <para>en-us:User Id</para>
        /// </param>
        /// <param name="departmentId">
        ///  <para>zh-cn:部门ID</para>
        ///  <para>en-us:Department Id</para>
        /// </param>
        /// <returns>
        ///  <para>zh-cn:返回离开结果，true表示成功，false表示失败</para>
        ///  <para>en-us:Returns the leave result, true indicates success, false indicates failure</para>
        /// </returns>
        public Task<bool> UserLeaveFromDepartmentAsync(string userId, string departmentId);

        #endregion

        #region 用户与用户组关联操作

        /// <summary>
        /// <para>zh-cn:让用户加入用户组</para>
        /// <para>en-us:Make user join a user group</para>
        /// </summary>
        /// <param name="userId">
        ///  <para>zh-cn:用户ID</para>
        ///  <para>en-us:User Id</para>
        /// </param>
        /// <param name="groupId">
        ///  <para>zh-cn:用户组ID</para>
        ///  <para>en-us:User Group Id</para>
        /// </param>
        /// <returns>
        ///  <para>zh-cn:返回加入结果，true表示成功，false表示失败</para>
        ///  <para>en-us:Returns the join result, true indicates success, false indicates failure</para>
        /// </returns>
        public Task<bool> UserJoinToUserGroupAsync(string userId, string groupId);

        /// <summary>
        /// <para>zh-cn:让用户离开用户组</para>
        /// <para>en-us:Make user leave a user group</para>
        /// </summary>
        /// <param name="userId">
        ///  <para>zh-cn:用户ID</para>
        ///  <para>en-us:User Id</para>
        /// </param>
        /// <param name="groupId">
        ///  <para>zh-cn:用户组ID</para>
        ///  <para>en-us:User Group Id</para>
        /// </param>
        /// <returns>
        ///  <para>zh-cn:返回离开结果，true表示成功，false表示失败</para>
        ///  <para>en-us:Returns the leave result, true indicates success, false indicates failure</para>
        /// </returns>
        public Task<bool> UserLeaveFromUserGroupAsync(string userId, string groupId);

        #endregion
    }
}