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
using air.cloud.security.common.Extensions;
using air.cloud.security.common.Model;
using air.cloud.security.common.Base.Dtos;
using air.cloud.system.model.Domains.EntityAssociationDomains;
using air.cloud.system.model.Domains.UserDomains;
using air.cloud.system.model.Dtos.UserDtos;
using air.cloud.system.model.Entitys.Users;

using Air.Cloud.Core.Extensions;
using Air.Cloud.Core.Extensions.Linqs;
using Air.Cloud.EntityFrameWork.Core.Repositories;
using Air.Cloud.WebApp.FriendlyException;
using Microsoft.EntityFrameworkCore;
using air.cloud.security.common.Enums;

namespace air.cloud.system.domain.Impls.UserDomains
{
    public  class UserGroupDomain : IUserGroupDomain
    {

        private readonly IRepository<UserGroup> repository;

        private readonly IEntityAssociationDomain entityAssociationDomain;

        public UserGroupDomain(IRepository<UserGroup> repository,IEntityAssociationDomain entityAssociationDomain)
        {
            this.repository= repository;
            this.entityAssociationDomain= entityAssociationDomain;
        }

        /// <summary>
        /// 创建用户组
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        public async Task<bool> CreateUserGroupAsync(UserGroupSDto dto)
        {
            var userGroup =await repository.FirstOrDefaultAsync(u => u.AppId == dto.AppId && u.GroupName == dto.GroupName);
            if (userGroup != null) return true;
            userGroup = new UserGroup
            {
                AppId = dto.AppId,
                GroupName = dto.GroupName,
                Description = dto.Description
            };
            await repository.InsertAsync(userGroup);
            return true;
        }

        /// <summary>
        /// 删除用户组 (删除所有有关于该用户组的权限) 该用户组下如果有用户则不允许删除 如果该用户组下有角色则不允许删除    
        /// </summary>
        /// <param name="AppId"></param>
        /// <param name="userGroupId"></param>
        /// <returns></returns>
        public async Task<bool> DeleteUserGroupAsync(string AppId, string userGroupId)
        {
            var UserGroup = await repository.FirstOrDefaultAsync(u => u.Id == userGroupId && u.AppId == AppId);
            if (UserGroup == null) throw Oops.Oh("用户组不存在，无法删除");

            var HasUsers = await entityAssociationDomain.ExitsEntityAssignAsync(userGroupId, AppId, AssociationTypeEnum.用户与用户组);

            if (HasUsers) throw Oops.Oh("用户组下存在用户，无法删除");

            //删除用户组与角色的关联信息
            await entityAssociationDomain.TruncateEntityAssignAsync(userGroupId,AppId, AssociationTypeEnum.用户组与角色);

            //删除用户组与角色组的关联信息

            await entityAssociationDomain.TruncateEntityAssignAsync(userGroupId, AppId, AssociationTypeEnum.用户组与角色组);

            //删除用户组
            await repository.DeleteAsync(UserGroup);

            return true;
        }
        /// <summary>
        /// 获取用户组详情
        /// </summary>
        /// <param name="AppId"></param>
        /// <param name="userGroupId"></param>
        /// <returns></returns>
        public Task<UserGroup> GetUserGroupAsync(string userGroupId)
        {
            var userGroup = repository.FirstOrDefaultAsync(u => u.Id == userGroupId);
            return userGroup;
        }

        /// <summary>
        /// 查询用户组列表
        /// </summary>
        /// <param name="baseQDto"></param>
        /// <returns></returns>

        public Task<PageList<UserGroup>> QueryUserGroupsAsync(BaseQDto baseQDto)
        {
            var linq=LinqExpressionExtensions.And<UserGroup>();

            if (!baseQDto.Info.IsNullOrEmpty())
            {
                linq = linq.And(u => u.GroupName.Contains(baseQDto.Info)||u.Description.Contains(baseQDto.Info));
            }

            linq = linq.And(u => u.AppId == baseQDto.AppId);

            var query= repository.DetachedEntities.Where(linq);
            var pageList= query.OrderByDescending(s => s.CreateTime).ToPageListAsync(baseQDto.Page,baseQDto.Limit);
            return pageList;
        }


        public async Task<IList<UserGroup>> QueryUserGroupsAsync(List<string> GroupIds)
        {
            var linq = LinqExpressionExtensions.And<UserGroup>();
            linq = linq.And(u => GroupIds.Contains(u.Id));
            var query = repository.DetachedEntities.Where(linq);
            return await query.ToListAsync();
        }


        /// <summary>
        /// 更新用户组信息
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        public async Task<bool> UpdateUserGroupAsync(UserGroupSDto dto)
        {
            var userGroup = await repository.FirstOrDefaultAsync(u => u.Id == dto.Id && u.AppId == dto.AppId);
            if (userGroup == null) return true;
            userGroup.GroupName = dto.GroupName;
            userGroup.Description = dto.Description;
            await repository.UpdateIncludeAsync(userGroup,new string[]
            {
                nameof(userGroup.GroupName),
                nameof(userGroup.Description)
            });
            return true;
        }


        #region 用户组与角色组关联操作
        /// <summary>
        /// 分配角色组到用户组
        /// </summary>
        /// <param name="AppId"></param>
        /// <param name="userGroupId"></param>
        /// <param name="roleGroupId"></param>
        /// <returns></returns>
        public async Task<bool> AssignRoleGroupToUserGroupAsync(string AppId, string userGroupId, string roleGroupId)
        {

            return await entityAssociationDomain.AddEntityAssignAsync(userGroupId, roleGroupId, AssociationTypeEnum.用户组与角色组, AppId);

        }


        /// <summary>
        /// 移除用户组的角色组
        /// </summary>
        /// <param name="AppId"></param>
        /// <param name="userGroupId"></param>
        /// <param name="roleGroupId"></param>
        /// <returns></returns>
        public async Task<bool> RemoveRoleGroupFromUserGroupAsync(string AppId, string userGroupId, string roleGroupId)
        {
            return await entityAssociationDomain.RemoveEntityAssignAsync(userGroupId, roleGroupId, AssociationTypeEnum.用户组与角色组, AppId);
        }


        #endregion


        #region  用户组与角色关联操作
        /// <summary>
        /// 分配角色到用户组
        /// </summary>
        /// <param name="AppId"></param>
        /// <param name="userGroupId"></param>
        /// <param name="roleId"></param>
        /// <returns></returns>
        public async Task<bool> AssignRoleToUserGroupAsync(string AppId, string userGroupId, string roleId)
        {
            return await entityAssociationDomain.AddEntityAssignAsync(userGroupId, roleId, AssociationTypeEnum.用户组与角色, AppId);
        }

        /// <summary>
        /// 移除用户组的角色
        /// </summary>
        /// <param name="AppId"></param>
        /// <param name="userGroupId"></param>
        /// <param name="roleId"></param>
        /// <returns></returns>
        public async Task<bool> RemoveRoleFromUserGroupAsync(string AppId, string userGroupId, string roleId)
        {
            return await entityAssociationDomain.RemoveEntityAssignAsync(userGroupId, roleId, AssociationTypeEnum.用户组与角色, AppId);
        }


        #endregion



        #region 用户组操作
        /// <summary>
        /// 迁移用户组用户到目标用户组
        /// </summary>
        /// <param name="AppId"></param>
        /// <returns></returns>
        public async Task<bool> MergeUserGroupUsersToNewUserGroupAsync(string AppId, string BeforeMergeUserGroupId, string AfterMergeUserGroupId)
        {
            return await entityAssociationDomain.MergeEntityAssignsAsync(BeforeMergeUserGroupId, AfterMergeUserGroupId, AssociationTypeEnum.用户与用户组, AppId);
        }
        /// <summary>
        /// 清空用户组下的用户
        /// </summary>
        /// <remarks>
        ///   主动解绑该用户组下的所有用户与该用户组的关联关系
        /// </remarks>
        /// <param name="AppId"></param>
        /// <param name="userGroupId"></param>
        /// <returns></returns>
        public async Task<bool> ClearUserGroupUsersByUserGroupIdAsync(string AppId, string userGroupId)
        {
            return await entityAssociationDomain.TruncateEntityAssignAsync(userGroupId, AppId, AssociationTypeEnum.用户与用户组);
        }

        /// <summary>
        /// 迁移用户组角色到目标用户组
        /// </summary>
        /// <param name="AppId"></param>
        /// <param name="RoleGroupId"></param>
        /// <param name="TargetGroupId"></param>
        /// <returns></returns>
        public async Task<bool> MergeUserGroupRolesToNewUserGroupAsync(string AppId, string BeforeMergeUserGroupId, string AfterMergeUserGroupId)
        {
            return await entityAssociationDomain.MergeEntityAssignsAsync(BeforeMergeUserGroupId, AfterMergeUserGroupId, AssociationTypeEnum.用户组与角色, AppId);
        }

        /// <summary>
        /// 迁移用户组的角色组到新的用户组
        /// </summary>
        /// <param name="AppId"></param>
        /// <param name="BeforeMergeUserGroupId"></param>
        /// <param name="AfterMergeUserGroupId"></param>
        /// <returns></returns>
        public async Task<bool> MergeUserGroupRoleGroupsToNewUserGroupAsync(string AppId, string BeforeMergeUserGroupId, string AfterMergeUserGroupId)
        {
            return await entityAssociationDomain.MergeEntityAssignsAsync(BeforeMergeUserGroupId, AfterMergeUserGroupId, AssociationTypeEnum.用户组与角色组, AppId);
        }


        /// <summary>
        /// 清空用户组下的角色
        /// </summary>
        /// <remarks>
        ///  主动解绑该用户组下的所有角色与该用户组的关联关系
        /// </remarks>
        /// <param name="AppId"></param>
        /// <param name="userGroupId"></param>
        /// <returns></returns>
        public async Task<bool> ClearUserGroupRolesByUserGroupIdAsync(string AppId, string userGroupId)
        {
            return await entityAssociationDomain.TruncateEntityAssignAsync(userGroupId, AppId, AssociationTypeEnum.用户组与角色);
        }

        /// <summary>
        /// 清空用户组下的角色组
        /// </summary>
        /// <param name="AppId"></param>
        /// <param name="userGroupId"></param>
        /// <returns></returns>
        public async Task<bool> ClearUserGroupRoleGroupsByUserGroupIdAsync(string AppId, string userGroupId)
        {
            return await entityAssociationDomain.TruncateEntityAssignAsync(userGroupId, AppId, AssociationTypeEnum.用户组与角色组);
        }



        #endregion


    }
}
