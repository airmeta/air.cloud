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
using air.cloud.security.common.Model;
using air.cloud.security.common.Base.Dtos;

using Air.Cloud.Core.Standard.DataBase.Domains;
using Air.Cloud.Core.Standard.DynamicServer;
using air.cloud.system.model.Dtos.UserDtos;
using air.cloud.system.model.Entitys.Users;

namespace air.cloud.system.model.Domains.UserDomains
{
    public interface IUserGroupDomain : IEntityDomain, ITransient
    {

        /// <summary>
        /// 创建用户组
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        public Task<bool> CreateUserGroupAsync(UserGroupSDto dto);
        /// <summary>
        /// 删除用户组 (删除所有有关于该用户组的权限) 该用户组下如果有用户则不允许删除 如果该用户组下有角色则不允许删除    
        /// </summary>
        /// <param name="AppId"></param>
        /// <param name="userGroupId"></param>
        /// <returns></returns>
        public Task<bool> DeleteUserGroupAsync(string AppId, string userGroupId);
        /// <summary>
        /// 获取用户组详情
        /// </summary>
        /// <param name="userGroupId"></param>
        /// <returns></returns>
        public Task<UserGroup> GetUserGroupAsync(string userGroupId);

        /// <summary>
        /// 查询用户组列表
        /// </summary>
        /// <param name="baseQDto"></param>
        /// <returns></returns>

        public Task<PageList<UserGroup>> QueryUserGroupsAsync(BaseQDto baseQDto);
        /// <summary>
        /// 查询用户组列表
        /// </summary>
        /// <param name="baseQDto"></param>
        /// <returns></returns>

        public Task<IList<UserGroup>> QueryUserGroupsAsync(List<string> GroupIds);


        /// <summary>
        /// 更新用户组信息
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        public Task<bool> UpdateUserGroupAsync(UserGroupSDto dto);


        #region 用户组与角色组关联操作
        /// <summary>
        /// 分配角色组到用户组
        /// </summary>
        /// <param name="AppId"></param>
        /// <param name="userGroupId"></param>
        /// <param name="roleGroupId"></param>
        /// <returns></returns>
        public Task<bool> AssignRoleGroupToUserGroupAsync(string AppId, string userGroupId, string roleGroupId);


        /// <summary>
        /// 移除用户组的角色组
        /// </summary>
        /// <param name="AppId"></param>
        /// <param name="userGroupId"></param>
        /// <param name="roleGroupId"></param>
        /// <returns></returns>
        public Task<bool> RemoveRoleGroupFromUserGroupAsync(string AppId, string userGroupId, string roleGroupId);


        #endregion


        #region  用户组与角色关联操作
        /// <summary>
        /// 分配角色到用户组
        /// </summary>
        /// <param name="AppId"></param>
        /// <param name="userGroupId"></param>
        /// <param name="roleId"></param>
        /// <returns></returns>
        public Task<bool> AssignRoleToUserGroupAsync(string AppId, string userGroupId, string roleId);

        /// <summary>
        /// 移除用户组的角色
        /// </summary>
        /// <param name="AppId"></param>
        /// <param name="userGroupId"></param>
        /// <param name="roleId"></param>
        /// <returns></returns>
        public Task<bool> RemoveRoleFromUserGroupAsync(string AppId, string userGroupId, string roleId);


        #endregion



        #region 用户组操作
        /// <summary>
        /// 迁移用户组用户到目标用户组
        /// </summary>
        /// <param name="AppId"></param>
        /// <returns></returns>
        public Task<bool> MergeUserGroupUsersToNewUserGroupAsync(string AppId, string BeforeMergeUserGroupId, string AfterMergeUserGroupId);
        /// <summary>
        /// 迁移用户组角色到目标用户组
        /// </summary>
        /// <param name="AppId"></param>
        /// <param name="RoleGroupId"></param>
        /// <param name="TargetGroupId"></param>
        /// <returns></returns>
        public Task<bool> MergeUserGroupRolesToNewUserGroupAsync(string AppId, string BeforeMergeUserGroupId, string AfterMergeUserGroupId);

        /// <summary>
        /// 迁移用户组角色组到新的用户组
        /// </summary>
        /// <param name="AppId"></param>
        /// <param name="BeforeMergeUserGroupId"></param>
        /// <param name="AfterMergeUserGroupId"></param>
        /// <returns></returns>
        public Task<bool> MergeUserGroupRoleGroupsToNewUserGroupAsync(string AppId, string BeforeMergeUserGroupId, string AfterMergeUserGroupId);

        /// <summary>
        /// 清空用户组下的用户
        /// </summary>
        /// <remarks>
        ///   主动解绑该用户组下的所有用户与该用户组的关联关系
        /// </remarks>
        /// <param name="AppId"></param>
        /// <param name="userGroupId"></param>
        /// <returns></returns>
        public Task<bool> ClearUserGroupUsersByUserGroupIdAsync(string AppId, string userGroupId);
        /// <summary>
        /// 清空用户组下的角色
        /// </summary>
        /// <remarks>
        ///  主动解绑该用户组下的所有角色与该用户组的关联关系
        /// </remarks>
        /// <param name="AppId"></param>
        /// <param name="userGroupId"></param>
        /// <returns></returns>
        public Task<bool> ClearUserGroupRolesByUserGroupIdAsync(string AppId, string userGroupId);

        /// <summary>
        /// 清空用户组下的角色组
        /// </summary>
        /// <param name="AppId"></param>
        /// <param name="userGroupId"></param>
        /// <returns></returns>
        public Task<bool> ClearUserGroupRoleGroupsByUserGroupIdAsync(string AppId, string userGroupId);


        #endregion


    }
}
