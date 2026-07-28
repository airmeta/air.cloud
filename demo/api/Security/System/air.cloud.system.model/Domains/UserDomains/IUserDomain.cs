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
using air.cloud.system.model.Domains.AppInfoDomains;
using air.cloud.system.model.Dtos.UserDtos;
using air.cloud.system.model.Entitys.Users;
using air.cloud.security.common.Base.Dtos;
using air.cloud.security.common.Model;

using Air.Cloud.Core.Standard.DataBase.Domains;
using Air.Cloud.Core.Standard.DynamicServer;

using System.Text;

namespace air.cloud.system.model.Domains.UserDomains
{
    public interface IUserDomain:IEntityDomain,ITransient
    {
        /// <summary>
        /// 创建用户
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        public Task<string> CreateUserAsync(UserSDto dto, string AppId);

        /// <summary>
        /// 删除用户
        /// </summary>
        /// <param name="AppId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public Task<bool> DeleteUserAsync(string userId, string AppId = null);

        /// <summary>
        /// 更新用户
        /// </summary>
        /// <param name="AppId"></param>
        /// <param name="userId"></param>
        /// <param name="userName"></param>
        /// <param name="email"></param>
        /// <param name="phone"></param>
        /// <returns></returns>
        public Task<string> UpdateUserAsync(UserSDto dto);

        /// <summary>
        /// 用户列表
        /// </summary>
        /// <returns></returns>
        public Task<PageList<UserRDto>> QueryUsersAsync(UserQDto baseQDto);
        /// <summary>
        /// 用户详情
        /// </summary>
        /// <param name="AppId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public Task<User?> GetUserAsync(string userId);


        /// <summary>
        /// 创建默认用户账号(仅在初始化的时候触发)
        /// </summary>
        /// <param name="Account"></param>
        /// <param name="Password"></param>
        /// <returns></returns>
        public Task<bool> CreatDefaultAccountAsync(IAppInfoDomain appInfoDomain, string Account,string AppId);

        /// <summary>
        /// <para>zh-cn:修改用户密码</para>
        /// <para>en-us:Change user password</para>
        /// </summary>
        /// <param name="UserId"></param>
        /// <param name="NewPassword"></param>
        /// <returns></returns>
        public Task<bool> ChangePasswordAsync(string UserId, string OldPassword, string NewPassword);

        /// <summary>
        /// <para>zh-cn:重置用户密码</para>
        /// <para>en-us:Reset user password</para>
        /// </summary>
        /// <param name="UserId">
        ///  <para>zh-cn:用户ID</para>    
        ///  <para>en-us:User Id</para>
        /// </param>
        /// <param name="NewPassword">
        ///  <para>zh-cn:新密码</para>
        ///  <para>en-us:New Password</para>
        /// </param>
        /// <returns></returns>
        public Task<bool> ResetPasswordAsync(string UserId, string NewPassword);

        #region 用户与角色关联操作

        /// <summary>
        /// 分配角色给用户
        /// </summary>
        /// <param name="AppId"></param>
        /// <param name="userId"></param>
        /// <param name="roleId"></param>
        /// <returns></returns>

        public Task<bool> AssignRoleToUserAsync(string AppId, string userId, string roleId);
        /// <summary>
        /// 删除用户角色
        /// </summary>
        /// <param name="AppId"></param>
        /// <param name="userId"></param>
        /// <param name="roleId"></param>
        /// <returns></returns>

        public Task<bool> RemoveRoleFromUserAsync(string AppId, string userId, string roleId);


        #endregion

        #region 用户与角色组关联操作

        /// <summary>
        /// 分配角色组给用户
        /// </summary>
        /// <param name="AppId"></param>
        /// <param name="userId"></param>
        /// <param name="roleGroupId"></param>
        /// <returns></returns>
        public Task<bool> AssignRoleGroupToUserAsync(string AppId, string userId, string roleGroupId);
        /// <summary>
        /// 移除用户角色组
        /// </summary>
        /// <param name="AppId"></param>
        /// <param name="userId"></param>
        /// <param name="roleGroupId"></param>
        /// <returns></returns>
        public Task<bool> RemoveRoleGroupFromUserAsync(string AppId, string userId, string roleGroupId);

        #endregion


        #region 用户与部门关联操作

        /// <summary>
        /// 加入部门
        /// </summary>
        /// <param name="AppId"></param>
        /// <param name="userId"></param>
        /// <param name="departmentId"></param>
        /// <returns></returns>
        public Task<bool> UserJoinToDepartmentAsync(string AppId, string userId, string departmentId);





        /// <summary>
        /// 离开部门
        /// </summary>
        /// <param name="AppId"></param>
        /// <param name="userId"></param>
        /// <param name="departmentId"></param>
        /// <returns></returns>
        public Task<bool> UserLeaveFromDepartmentAsync(string AppId, string userId, string departmentId);

        #endregion

        #region 用户与用户组关联操作
        /// <summary>
        /// 加入用户组
        /// </summary>
        /// <param name="AppId"></param>
        /// <param name="userId"></param>
        /// <param name="groupId"></param>
        /// <returns></returns>
        public Task<bool> UserJoinToUserGroupAsync(string AppId, string userId, string groupId);
        /// <summary>
        /// 离开用户组
        /// </summary>
        /// <param name="AppId"></param>
        /// <param name="userId"></param>
        /// <param name="groupId"></param>
        /// <returns></returns>
        public Task<bool> UserLeaveFromUserGroupAsync(string AppId, string userId, string groupId);
        /// <summary>
        /// <para>zh-cn:给予用户部门</para>
        /// <para>en-us:Give user departments</para>
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="departmentIds"></param>
        /// <returns></returns>
        Task<bool> GiveUserDepartmentsAsync(string userId, string departmentIds);
        /// <summary>
        /// <para>zh-cn:给予用户角色</para>
        /// <para>en-us:Give user roles</para>
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="roleIds"></param>
        /// <returns></returns>
        Task<bool> GiveUserRolesAsync(string userId, string roleIds);

        /// <summary>
        /// <para>zh-cn:给予用户岗位</para>
        /// <para>en-us:Give user assignments</para>
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="assignmentIds"></param>
        /// <returns></returns>
        Task<bool> GiveUserAssignmentsAsync(string userId, string assignmentIds);

        #endregion


        /// <summary>
        /// <para>zh-cn:生成随机密码</para>
        /// <para>en-us:Generate random password</para>
        /// </summary>
        /// <returns></returns>
        public static string GeneratePassword()
        {
            string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!@#$%^&*()";
            StringBuilder sb = new StringBuilder();
            Random random = new Random();

            // 随机选择一个字符作为密码的第一个字符
            sb.Append(chars[random.Next(chars.Length)]);

            // 随机选择一个数字作为密码的第二个字符
            sb.Append(random.Next(10));

            // 随机选择一个大写字母作为密码的第三个字符
            sb.Append(chars[random.Next(26, 52)]);

            // 随机选择一个特殊符号作为密码的第四个字符
            sb.Append(chars[random.Next(52, chars.Length)]);

            // 生成剩余的字符
            for (int i = 4; i < 8; i++)
            {
                sb.Append(chars[random.Next(chars.Length)]);
            }

            return sb.ToString();
        }
    }


}
