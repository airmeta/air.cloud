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
using air.cloud.system.model.Dtos.RoleDtos;
using air.cloud.system.model.Entitys.Roles;
using air.cloud.security.common.Base.Dtos;
using air.cloud.security.common.Model;

using Air.Cloud.Core.Standard.DataBase.Domains;
using Air.Cloud.Core.Standard.DynamicServer;

namespace air.cloud.system.model.Domains.RoleDomains
{
    /// <summary>
    /// 角色组领域信息
    /// </summary>
    public interface IRoleGroupDomain : IEntityDomain, ITransient
    {
        /// <summary>
        /// 创建角色组
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        public Task<bool> CreateRoleGroupAsync(RoleGroupSDto dto);

        /// <summary>
        /// 删除角色组 (删除所有有关于该角色组的权限) 该角色组下如果有角色则不允许删除 如果该角色组下有用户则不允许删除
        /// </summary>
        /// <param name="AppId"></param>
        /// <param name="roleGroupId"></param>
        /// <returns></returns>
        public Task<bool> DeleteRoleGroupAsync(string AppId, string roleGroupId);

        /// <summary>
        /// 更新角色组
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        public Task<bool> UpdateRoleGroupAsync(RoleGroupSDto dto);

        /// <summary>
        /// 查询角色组
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        public Task<PageList<RoleGroup>> QueryRoleGroupsAsync(BaseQDto dto);

        /// <summary>
        /// 获取单个角色组
        /// </summary>
        /// <param name="AppId"></param>
        /// <param name="roleGroupId"></param>
        /// <returns></returns>
        public Task<RoleGroup> GetRoleGroupAsync(string roleGroupId, string AppId = null);

        /// <summary>
        /// 获取角色下拉框
        /// </summary>
        /// <returns></returns>
        public Task<List<RoleGroupSelectRDto>> ListAllRoleGroupsAsync(string AppId);

        #region 角色组操作
        /// <summary>
        /// 合并角色组用户到目标角色组
        /// </summary>
        /// <param name="AppId"></param>
        /// <returns></returns>
        public Task<bool> MergeRoleGroupUsersToNewRoleGroupAsync(string AppId, string RoleGroupId, string TargetGroupId);
        /// <summary>
        /// 合并角色组角色到目标角色组
        /// </summary>
        /// <param name="AppId"></param>
        /// <param name="RoleGroupId"></param>
        /// <param name="TargetGroupId"></param>
        /// <returns></returns>
        public Task<bool> MergeRoleGroupRolesToNewRoleGroupAsync(string AppId, string RoleGroupId, string TargetGroupId);
        /// <summary>
        /// 清空角色组下的用户
        /// </summary>
        /// <remarks>
        ///   主动解绑该角色组下的所有用户与该角色组的关联关系
        /// </remarks>
        /// <param name="AppId"></param>
        /// <param name="roleGroupId"></param>
        /// <returns></returns>
        public Task<bool> ClearRoleGroupUsersByRoleGroupIdAsync(string AppId, string roleGroupId);

        /// <summary>
        /// 清空角色组下的角色
        /// </summary>
        /// <remarks>
        ///  主动解绑该角色组下的所有角色与该角色组的关联关系
        /// </remarks>
        /// <param name="AppId"></param>
        /// <param name="roleGroupId"></param>
        /// <returns></returns>
        public Task<bool> ClearRoleGroupRolesByRoleGroupIdAsync(string AppId, string roleGroupId);

        #endregion

    }
}



