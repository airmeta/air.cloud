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
using air.cloud.system.model.Dtos.MenuDtos;
using air.cloud.system.model.Dtos.RoleDtos;
using air.cloud.system.model.Entitys.Roles;

using Air.Cloud.Core.Standard.DynamicServer;

namespace air.cloud.system.service.Services.RoleServices
{
    /// <summary>
    /// <para>zh-cn:角色服务接口</para>
    /// <para>en-us:Role Service Interface</para>
    /// </summary>
    public interface IRoleService : IDynamicService
    {
        /// <summary>
        /// 创建角色
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        public Task<bool> SaveRoleAsync(RoleSDto dto);
        /// <summary>
        /// 拷贝角色并创建新角色
        /// </summary>
        /// <param name="AppId"></param>
        /// <param name="roleId"></param>
        /// <returns></returns>
        public Task<bool> CopyRoleThenCreateNewRoleAsync(RoleSDto dto);

        /// <summary>
        /// 删除角色 (删除所有有关于该角色的权限)
        /// </summary>
        /// <param name="roleId"></param>
        /// <returns></returns>
        public Task<bool> DeleteRoleAsync(string roleId);

        /// <summary>
        /// 查询角色
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        public Task<PageList<Role>> QueryRolesAsync(BaseQDto dto);

        /// <summary>
        /// 获取单个角色
        /// </summary>
        /// <param name="roleId"></param>
        /// <returns></returns>
        public Task<Role> GetRoleAsync(string roleId);


        #region  角色与角色组关联操作
        /// <summary>
        /// 角色加入角色组   
        /// </summary>
        /// <param name="roleId"></param>
        /// <param name="roleGroupId"></param>
        /// <returns></returns>
        public Task<bool> JoinRoleToRoleGroupAsync(string roleId, string roleGroupId);

        /// <summary>
        /// 角色离开角色组   
        /// </summary>
        /// <param name="roleId"></param>
        /// <param name="roleGroupId"></param>
        /// <returns></returns>
        public Task<bool> LeaveRoleFromRoleGroupAsync(string roleId, string roleGroupId);

        #endregion


        #region 角色与动作权限关联操作
        /// <summary>
        /// 分配动作权限给角色
        /// </summary>
        /// <param name="roleId"></param>
        /// <param name="permissionId"></param>
        /// <returns></returns>
        public Task<bool> JoinActionPermissionToRoleAsync(string roleId, string permissionId);
        /// <summary>
        /// 移除角色的动作权限
        /// </summary>
        /// <param name="roleId"></param>
        /// <param name="permissionId"></param>
        /// <returns></returns>
        public Task<bool> RemoveActionPermissionFromRoleAsync(string roleId, string permissionId);
        #endregion


        #region 角色与菜单关联操作

        /// <summary>
        /// 分配菜单给角色
        /// </summary>
        /// <param name="roleId"></param>
        /// <param name="menuId"></param>
        /// <returns></returns>
        public Task<bool> JoinMenuToRoleAsync(string roleId, string menuId);
        /// <summary>
        /// 移除角色的菜单
        /// </summary>
        /// <param name="roleId"></param>
        /// <param name="menuId"></param>
        /// <returns></returns>
        public Task<bool> RemoveMenuFromRoleAsync(string roleId, string menuId);

        /// <summary>
        /// <para>zh-cn:批量修改角色菜单权限</para>
        /// <para>en-us:Batch change role menu permissions</para>
        /// </summary>
        /// <param name="dto">
        ///  <para>zh-cn:包含角色ID和菜单ID列表的DTO对象</para>
        ///  <para>en-us:DTO object containing role ID and list of menu IDs</para>
        /// </param>
        /// <returns></returns>
        public Task<bool> BatchChangeRoleMenuAsync(BatchChangeRoleMenuDto dto);
        /// <summary>
        /// <para>zh-cn:获取角色的菜单权限</para>
        /// <para>en-us:Get the menu permissions of the role</para>
        /// </summary>
        /// <param name="RoleId"></param>
        /// <returns></returns>
        Task<IList<RoleMenusRDto>> GetRoleMenuAsync(string RoleId);
        #endregion
    }
}
