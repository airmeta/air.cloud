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
using air.cloud.system.model.Domains.MenuDomains;
using air.cloud.system.model.Domains.RoleDomains;
using air.cloud.system.model.Dtos.MenuDtos;
using air.cloud.system.model.Dtos.RoleDtos;
using air.cloud.system.model.Entitys.Roles;

using Air.Cloud.Core.App;
using Air.Cloud.Core.Extensions;
using Air.Cloud.Core.Extensions.Linqs;
using Air.Cloud.EntityFrameWork.Core.Repositories;
using Air.Cloud.WebApp.FriendlyException;

using Mapster;

using Microsoft.EntityFrameworkCore;

namespace air.cloud.system.domain.Impls.RoleDomains
{
    public class RoleDomain : IRoleDomain
    {
        private readonly IRepository<Role> repository;

        private readonly IEntityAssociationDomain entityAssociationDomain;

        private readonly IMenuDomain menuDomain;

        public RoleDomain(IRepository<Role> repository, 
            IMenuDomain menuDomain,
            IEntityAssociationDomain entityAssociationDomain)
        {
            this.repository = repository;
            this.menuDomain= menuDomain;
            this.entityAssociationDomain = entityAssociationDomain;
        }

        #region 角色基本操作
        public async Task<string> CreateRoleAsync(RoleSDto dto)
        {
           var role =await repository.Change<Role>()
                .DetachedEntities.FirstOrDefaultAsync(s => s.AppId == dto.AppId && s.RoleName == dto.RoleName);
            if (role != null) throw Oops.Oh("当前角色名称在此应用中已经存在");
            role = new Role()
            {
                Id=AppCore.Guid(),
                AppId = dto.AppId,
                RoleName = dto.RoleName,
                Description = dto.Description
            };
            await repository.Change<Role>().InsertAsync(role);
            return role.Id;
        }
        public async Task<bool> CopyRoleThenCreateNewRoleAsync(RoleSDto dto)
        {
            var existingRole = await repository.Change<Role>().DetachedEntities.FirstOrDefaultAsync(s => s.AppId == dto.AppId && s.Id==dto.TargetId);
            if (existingRole == null) throw Oops.Oh("要复制的角色不存在，无法完成复制操作");
            var roleWithSameName = await repository.Change<Role>()
                 .DetachedEntities.FirstOrDefaultAsync(s => s.AppId == dto.AppId && s.RoleName == dto.RoleName);
            if (roleWithSameName != null) return true;
            Role newRole = new Role()
            {
                Id=AppCore.Guid(),
                AppId = existingRole.AppId,
                RoleName = dto.RoleName,
                Description = dto.Description
            };
            await repository.Change<Role>().InsertAsync(newRole);
            await entityAssociationDomain.CopyEntityAssignsAsync(existingRole.Id, newRole.Id,dto.AppId,new AssociationTypeEnum[]
            {
                    AssociationTypeEnum.角色与动作权限,
                    AssociationTypeEnum.角色与菜单权限
            });
            return true;
        }

        public async Task<bool> DeleteRoleAsync(string AppId, string roleId)
        {
            var role = await  repository.Change<Role>()
                 .DetachedEntities.FirstOrDefaultAsync(s => s.AppId == AppId && s.Id == roleId);
            if (role == null) return true;
            await repository.Change<Role>().DeleteAsync(role);
            return true;
        }

        public async Task<Role> GetRoleAsync(string roleId, string AppId=null)
        {
            if (AppId.IsNullOrEmpty())
            {
                var roleNoAppId = await repository.Change<Role>()
                     .DetachedEntities.FirstOrDefaultAsync(s => s.Id == roleId);
                return roleNoAppId;
            }
            else
            {
                var role = await repository.Change<Role>()
                     .DetachedEntities.FirstOrDefaultAsync(s => s.AppId == AppId && s.Id == roleId);
                return role;
            }
        }

        public async Task<PageList<Role>> QueryRolesAsync(BaseQDto dto)
        {
            var linq = LinqExpressionExtensions.And<Role>();

            if (!string.IsNullOrEmpty(dto.AppId))
            {
                linq = linq.And(s => s.AppId == dto.AppId);
            }
            if (!string.IsNullOrEmpty(dto.Info))
            {
                linq = linq.And(s => s.RoleName.Contains(dto.Info));
            }
            var query = repository.Change<Role>().DetachedEntities.Where(linq);
            return await query.OrderByDescending(s => s.CreateTime).ToPageListAsync<Role>(dto.Page,dto.Limit);
        }

        public async Task<string> UpdateRoleAsync(RoleSDto dto)
        {
           var role=await  repository.Change<Role>()
                .DetachedEntities.FirstOrDefaultAsync(s => s.AppId == dto.AppId && s.Id == dto.Id);
            if (role == null) return string.Empty;
            role.RoleName = dto.RoleName;
            role.Description = dto.Description;
            await repository.Change<Role>().UpdateIncludeAsync(role,new string[]
            {
                nameof(Role.RoleName),
                nameof(Role.Description),
            });
            return role.Id;  
        }

        #endregion


        #region 角色与角色组关联操作
        public async Task<bool> JoinRoleToRoleGroupAsync(string AppId, string roleId, string roleGroupId)
        {
            return await entityAssociationDomain.AddEntityAssignAsync(roleId, roleGroupId, AssociationTypeEnum.角色与角色组, AppId);
        }

        public async Task<bool> LeaveRoleFromRoleGroupAsync(string AppId, string roleId, string roleGroupId)
        {
            return await entityAssociationDomain.RemoveEntityAssignAsync(roleId, roleGroupId, AssociationTypeEnum.角色与角色组, AppId);
        }

        #endregion


        #region 角色与动作权限

        public async Task<bool> JoinActionPermissionToRoleAsync(string AppId, string roleId, string permissionId)
        {
            return await entityAssociationDomain.AddEntityAssignAsync(roleId, permissionId, AssociationTypeEnum.角色与动作权限, AppId);
        }

        public async Task<bool> RemoveActionPermissionFromRoleAsync(string AppId, string roleId, string permissionId)
        {
            return await entityAssociationDomain.RemoveEntityAssignAsync(roleId, permissionId, AssociationTypeEnum.角色与动作权限, AppId);
        }
        #endregion

        #region 角色与菜单权限

        public async Task<bool> JoinMenuToRoleAsync(string AppId, string roleId, string menuId)
        {
            return await entityAssociationDomain.AddEntityAssignAsync(roleId, menuId, AssociationTypeEnum.角色与菜单权限, AppId);
        }

        public async  Task<bool> RemoveMenuFromRoleAsync(string AppId, string roleId, string menuId)
        {
            return await entityAssociationDomain.RemoveEntityAssignAsync(roleId, menuId, AssociationTypeEnum.角色与菜单权限, AppId);
        }

        public async Task<bool> BatchChangeRoleMenuAsync(BatchChangeRoleMenuDto dto)
        {
            var role = await repository.DetachedEntities.FirstOrDefaultAsync(s=>s.Id==dto.RoleId);
            if (role == null) throw Oops.Oh("角色不存在，无法完成分配操作");

            var RoleMenus= await entityAssociationDomain.GetEntityAssociationsQueryableAsync(dto.RoleId, role.AppId, AssociationTypeEnum.角色与菜单权限);

            var CurrentHasMenus = RoleMenus.Select(s => s.TargetEntityId).ToList();

            //需要添加的菜单
            var needToAddMenus = dto.MenuIds.Except(CurrentHasMenus).ToList();

            if (needToAddMenus.Any())
            {
                foreach (var menuId in needToAddMenus)
                {
                    await entityAssociationDomain.AddEntityAssignAsync(dto.RoleId, menuId, AssociationTypeEnum.角色与菜单权限, role.AppId);
                }
            }
            //需要移除的菜单
            var needToRemoveMenus = CurrentHasMenus.Except(dto.MenuIds).ToList();
            if (needToRemoveMenus.Any())
            {
                foreach (var menuId in needToRemoveMenus)
                {
                    await entityAssociationDomain.RemoveEntityAssignAsync(dto.RoleId, menuId, AssociationTypeEnum.角色与菜单权限, role.AppId);
                }
            }
            return true;
        }

        public async Task<IList<RoleMenusRDto>> GetRoleMenuAsync(string RoleId)
        {
            var role = await repository.DetachedEntities.FirstOrDefaultAsync(s => s.Id == RoleId);
            if (role == null) throw Oops.Oh("角色不存在，无法完成分配操作");
            var Menus = await menuDomain.QueryMenusAsync(new BaseQDto()
            {
                AppId = role.AppId,
                Limit=0,
                Page=0
            });
            var roleMenuAuthoritys = await entityAssociationDomain.GetEntityAssociationsAsync(RoleId, role.AppId,AssociationTypeEnum.角色与菜单权限);
            var roleMenuIds = roleMenuAuthoritys.Select(s => s.TargetEntityId).ToList();
            return Menus.List.Select(s =>
            {
                var dto = s.Adapt<RoleMenusRDto>();
                dto.Checked = roleMenuIds.Contains(s.Id);
                return dto;
            }).ToList(); 
        }

        public async Task<IEnumerable<Role>> GetUserRoleAsync(string userId)
        {
            var roleIds = entityAssociationDomain
                .GetEntityAssociationsQueryableAsync(userId, null, AssociationTypeEnum.用户与角色)
                .Result
                .Select(s => s.TargetEntityId)
                .ToList();
            return await GetRoleAsync(roleIds.ToList());
        }

        public async Task<IEnumerable<Role>> GetRoleAsync(List<string>? roleIds)
        {
           return await repository.DetachedEntities.Where(s => roleIds!.Contains(s.Id)).ToListAsync();
        }
        #endregion
    }
}