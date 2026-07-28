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
using air.cloud.security.common.Auths;
using air.cloud.security.common.Enums;
using air.cloud.security.common.Extensions;
using air.cloud.security.common.Model;
using air.cloud.system.model.Domains.ActionPermissionDomains;
using air.cloud.system.model.Domains.AppInfoDomains;
using air.cloud.system.model.Domains.EntityAssociationDomains;
using air.cloud.system.model.Domains.MenuDomains;
using air.cloud.system.model.Domains.OrganizationDomains;
using air.cloud.system.model.Domains.RoleDomains;
using air.cloud.system.model.Domains.UserDomains;
using air.cloud.system.model.Dtos.MenuDtos;
using air.cloud.system.model.Dtos.OrganizationDtos.DepartmentDtos;
using air.cloud.system.model.Dtos.RoleDtos;
using air.cloud.system.model.Dtos.UserDtos;
using air.cloud.system.model.Entitys.Associations;
using air.cloud.system.model.Entitys.Users;

using Air.Cloud.Core;
using Air.Cloud.Core.App;
using Air.Cloud.Core.Extensions;
using Air.Cloud.Core.Extensions.Linqs;
using Air.Cloud.Core.Plugins.Security.MD5;
using Air.Cloud.EntityFrameWork.Core.Repositories;
using Air.Cloud.WebApp.FriendlyException;

using Mapster;

using Microsoft.EntityFrameworkCore;

using System.Data;

namespace air.cloud.system.domain.Impls.UserDomains
{
    public class UserDomain : IUserDomain
    {
        private readonly IRepository<User> repository;

        private readonly IEntityAssociationDomain entityAssociationDomain;
        
        private readonly IRoleDomain roleDomain;
        
        private readonly IRoleGroupDomain roleGroupDomain;

        private readonly IUserGroupDomain userGroupDomain;

        private readonly IMenuDomain menuDomain;

        private readonly IActionPermissionDomain actionPermissionDomain;

        private readonly IDepartmentDomain departmentDomain;

        private readonly IUserAccountStore userAccount;

        private readonly IAssignmentDomain assignmentDomain;

        public const string ASSET_STORE_PATH = "Assets";


        public UserDomain(IRepository<User> _repository,
            IEntityAssociationDomain entityAssociationDomain,
            IRoleDomain roleDomain,
            IRoleGroupDomain roleGroupDomain,
            IUserGroupDomain userGroupDomain,
            IAssignmentDomain assignmentDomain,
            IMenuDomain menuDomain,
            IActionPermissionDomain actionPermissionDomain,
            IUserAccountStore userAccountStore,
            IDepartmentDomain departmentDomain)
        {
            this.repository=_repository;
            this.entityAssociationDomain = entityAssociationDomain;
            this.roleDomain = roleDomain;
            this.roleGroupDomain = roleGroupDomain;
            this.userGroupDomain = userGroupDomain;
            this.menuDomain = menuDomain;
            this.actionPermissionDomain = actionPermissionDomain;
            this.departmentDomain = departmentDomain;
            this.userAccount = userAccountStore;
            this.assignmentDomain= assignmentDomain;
        }

        public async Task<bool> DeleteUserAsync(string userId,string AppId=null)
        {
            var User = await repository.DetachedEntities.FirstOrDefaultAsync(s => s.Id == userId);
            if (User == null) return false;
            User.IsDelete = IsOrNotEnum.是;
           
            if (AppId.IsNullOrEmpty())
            {
                UserAccountFactory userAccountFactory = await userAccount.GetUserAccountAsync();
                User.DeleteUserId = userId;
                User.DeleteUserName = userAccountFactory.UserName;
            }
            else
            {
                User.DeleteUserId = AppId;
                User.DeleteUserName = AppId;
            }
            await repository.UpdateIncludeAsync(User, new string[]
            {
                nameof(User.IsDelete)
            });
            return true;
        }
       
        public async Task<PageList<UserRDto>> QueryUsersAsync(UserQDto baseQDto)
        {
            var Linq = LinqExpressionExtensions.And<User>();
            Linq=Linq.And(s => s.IsDelete == IsOrNotEnum.否);
            if (!baseQDto.Info.IsNullOrEmpty())
            {
                Linq = Linq.And(s => s.UserName.Contains(baseQDto.Info) 
                || s.Account.Contains(baseQDto.Info)
                ||s.Email.Contains(baseQDto.Info)
                ||s.IdCardNo.Contains(baseQDto.Info)
                ||s.PhoneNumber.Contains(baseQDto.Info));
            }
            var list = repository.Entities.Where(Linq).AsQueryable();
            if (!baseQDto.DepartmentId.IsNullOrEmpty())
            {
                list = from a in list
                      .Where(s => s.IsDelete == IsOrNotEnum.否)
                      join b in repository.Change<EntityAssociation>().Where(s =>
                                        s.AssociationType==AssociationTypeEnum.用户与部门&&
                                        s.TargetEntityId == baseQDto.DepartmentId)
                      on a.Id equals b.SourceEntityId
                      select a;
            }
            if (!baseQDto.RoleId.IsNullOrEmpty())
            {
                list = from a in list
                      .Where(s => s.IsDelete == IsOrNotEnum.否)
                       join b in repository.Change<EntityAssociation>().Where(s =>
                                       s.AssociationType == AssociationTypeEnum.用户与角色 &&
                                       s.TargetEntityId == baseQDto.RoleId)
                      on a.Id equals b.SourceEntityId
                      select a;
            }
            var result=list.OrderByDescending(s => s.CreateTime).Select(s => new UserRDto(s)).AsQueryable();
            return await result.ToPageListAsync<UserRDto>(baseQDto.Page, baseQDto.Limit);
        }

        public async Task<string> UpdateUserAsync(UserSDto dto)
        {
            var User = await repository.DetachedEntities.FirstOrDefaultAsync(s => (s.Id == dto.Id||s.AppUserId==dto.AppUserId) &&s.IsDelete==IsOrNotEnum.否);

            if(User==null) return string.Empty;

            User = dto.Adapt<User>();
            User.UserName = dto.UserName;
            User.PhoneNumber= dto.PhoneNumber;
            User.IdCardNo = dto.IdCardNo;
            User.Email = dto.Email;
            await repository.UpdateIncludeAsync(User,new string[]
            {
                nameof(User.UserName),
                nameof(User.Email),
                nameof(User.PhoneNumber),
                nameof(User.IdCardNo)
            });
            return User.Id;
        }

        public async Task<bool> CreatDefaultAccountAsync(IAppInfoDomain appInfoDomain, string Account,string DefaultAppId)
        {
            /**
              *  初始化: 
              *      1. 创建安全认证平台应用程序(自动)
              *      2. 创建安全认证平台超管角色组(自动)
              *      3. 创建安全认证平台角色 并加入组(自动)
              *      4. 创建安全认证平台菜单、权限(自动)
              *      5. 配置安全认证平台菜单、权限到角色(自动)
              *      6. 创建安全认证平台的管理员账户(拥有安全认证平台全部功能)
              */
            string RoleId=await roleDomain.CreateRoleAsync(new model.Dtos.RoleDtos.RoleSDto()
            {
                AppId = DefaultAppId,
                Description = "系统初始化自动创建角色",
                RoleName = "统一身份认证平台管理员",
                TargetId = string.Empty
            });
            string GroupId= AppCore.Guid();
            await roleGroupDomain.CreateRoleGroupAsync(new RoleGroupSDto()
            {
                AppId = DefaultAppId,
                RoleGroupName = "统一身份认证平台默认角色组",
                Description = "系统初始化自动创建角色组",
                Id = GroupId
            });
            //绑定关联关系
            await roleDomain.JoinRoleToRoleGroupAsync(DefaultAppId, RoleId, GroupId);

            #region 增加一个默认部门到系统中
            string DepartmentId=await departmentDomain.CreateDepartmentAsync(new DepartmentSDto()
            {
                AppId = DefaultAppId,
                DepartmentName = "统一身份认证平台默认部门",
                Description= "系统初始化自动创建部门",
                DepartmentCode = "SFRZ_001"
            });
            #endregion
            await CreateDefaultMenus(DefaultAppId,RoleId);
            #region 创建用户
            string AccountCerdictKey = AppCore.Guid();
            string Password = MD5Encryption.GetMd5By8(AppCore.Guid());
            AppRealization.Output.Print("默认账户密码", $"账户名为:{Account} 密码为:{Password},请妥善保管,初次登录时将会要求修改密码!" );

            User user = new User()
            {
                Id = AppCore.Guid(),
                Account = Account,
                UserName = "统一身份认证平台默认账户",
                AccountCreateAppId = DefaultAppId,
                Email = "",
                IdCardNo = "",
                PhoneNumber = "",
                AppUserId = AccountCerdictKey,
                AccountCerdictKey = AccountCerdictKey,
                Password = MD5Encryption.GetMd5By32(Password + AccountCerdictKey)
            };
            await repository.InsertAsync(user);
            AccountCerdictKey =user.Id;
            #endregion

            #region 创建用户组 并关联用户到用户组、部门、角色组、角色

            string UserGroupId = AppCore.Guid();
            await userGroupDomain.CreateUserGroupAsync(new UserGroupSDto()
            {
                Id = UserGroupId,
                AppId = DefaultAppId,
                Description = "系统初始化自动创建用户组",
                GroupName = "统一身份认证平台默认用户组"
            });
            await AssignRoleGroupToUserAsync(DefaultAppId, AccountCerdictKey, UserGroupId);
            await UserJoinToDepartmentAsync(DefaultAppId, AccountCerdictKey, DepartmentId);
            await UserJoinToUserGroupAsync(DefaultAppId, AccountCerdictKey, UserGroupId);
            await AssignRoleToUserAsync(DefaultAppId, AccountCerdictKey, RoleId);
            await appInfoDomain.JoinAppToRoleAsync(RoleId, DefaultAppId, true);
            #endregion
            return true;
        }

        public async Task<string> CreateUserAsync(UserSDto dto,string AppId)
        {
            var Accounts= repository.DetachedEntities.Where(s => s.IsDelete==IsOrNotEnum.否).AsQueryable();

            if (!dto.Account.IsNullOrEmpty())
            {
                Accounts = Accounts.Where(s => s.Account == dto.Account);
            }

            var HasAccount = await Accounts.AnyAsync();
            
            if (HasAccount) throw Oops.Oh("当前账户已存在,无法创建");

            User user = dto.Adapt<User>();
            user.Id = AppCore.Guid();
            user.AppUserId = dto.AppUserId;
            user.AccountCreateAppId = AppId;
            user.AccountCerdictKey = user.AppUserId;
            user.Password =MD5Encryption.GetMd5By32(dto.Password+user.AccountCerdictKey);
            await repository.InsertAsync(user);
            return user.Id;
        }
        /// <summary>
        /// <para>zh-cn:创建默认菜单与权限</para>
        /// <para>en-us:Create Default Menus And Permissions</para>
        /// </summary>
        /// <param name="DefaultAppId"></param>
        /// <param name="RoleId"></param>
        /// <returns></returns>
        private async Task CreateDefaultMenus(string DefaultAppId,string RoleId)
        {
            #region 从文件读取菜单信息 并入库存储
            string FilePath = Path.Combine(AppConst.ApplicationPath, ASSET_STORE_PATH, "menu.json");
            string JsonContent = string.Empty;
            if (File.Exists(FilePath))
            {
                JsonContent = File.ReadAllText(FilePath);
                //菜单
                var menus = AppRealization.JSON.Deserialize<IList<AppCreateMenusDto>>(JsonContent);
                foreach (var item in menus)
                {
                    item.AppId = DefaultAppId;
                    item.ParentId = string.Empty;
                    string itemId = await menuDomain.CreateMenuAsync(item.Adapt<MenuSDto>());
                    await roleDomain.JoinMenuToRoleAsync(DefaultAppId, RoleId, itemId);
                    if (item.Children!=null)
                    {
                        await CreateDefaultMenusChild(item.Children, DefaultAppId, RoleId, itemId);
                    }
                   
                }
            }
            #endregion
        }
        private async Task CreateDefaultMenusChild(IList<AppCreateMenusDto> menus, string DefaultAppId,string RoleId, string ParentId)
        {
            foreach (var item in menus)
            {
                item.AppId = DefaultAppId;
                item.ParentId = ParentId;
                string itemId = await menuDomain.CreateMenuAsync(item.Adapt<MenuSDto>());
                await roleDomain.JoinMenuToRoleAsync(DefaultAppId, RoleId, itemId);
                if (item.Children != null)
                {
                    await CreateDefaultMenusChild(item.Children, DefaultAppId, RoleId, itemId);
                }
            }
        }
        #region 用户与角色关联操作


        public async Task<bool> AssignRoleToUserAsync(string AppId, string userId, string roleId)
        {
            return await entityAssociationDomain.AddEntityAssignAsync(userId,roleId, AssociationTypeEnum.用户与角色,AppId);
        }
        public async Task<bool> RemoveRoleFromUserAsync(string AppId, string userId, string roleId)
        {
            return await entityAssociationDomain.RemoveEntityAssignAsync(userId, roleId, AssociationTypeEnum.用户与角色, AppId);
        }

        #endregion

        #region 用户与角色组关联操作

        public async Task<bool> AssignRoleGroupToUserAsync(string AppId, string userId, string roleGroupId)
        {
            return await entityAssociationDomain.AddEntityAssignAsync(userId, roleGroupId, AssociationTypeEnum.用户与角色组, AppId);
        }
        public async Task<bool> RemoveRoleGroupFromUserAsync(string AppId, string userId, string roleGroupId)
        {
           return await entityAssociationDomain.RemoveEntityAssignAsync(userId, roleGroupId, AssociationTypeEnum.用户与角色组, AppId);
        }

        #endregion

        #region 用户与部门关联操作

        public async Task<bool> UserJoinToDepartmentAsync(string AppId, string userId, string departmentId)
        {
          return await entityAssociationDomain.AddEntityAssignAsync(userId, departmentId, AssociationTypeEnum.用户与部门, AppId);
        }
        public async Task<bool> UserLeaveFromDepartmentAsync(string AppId, string userId, string departmentId)
        {
            return await entityAssociationDomain.RemoveEntityAssignAsync(userId, departmentId, AssociationTypeEnum.用户与部门, AppId);
        }

        #endregion

        #region  用户与用户组关联操作
        public async Task<bool> UserJoinToUserGroupAsync(string AppId, string userId, string groupId)
        {
           return await entityAssociationDomain.AddEntityAssignAsync(userId, groupId, AssociationTypeEnum.用户与用户组, AppId);
        }

        public async Task<bool> UserLeaveFromUserGroupAsync(string AppId, string userId, string groupId)
        {
           return await entityAssociationDomain.RemoveEntityAssignAsync(userId, groupId, AssociationTypeEnum.用户与用户组, AppId);
        }

        public async Task<User?> GetUserAsync(string userId)
        {
            return await repository.DetachedEntities.FirstOrDefaultAsync(s => (s.Id == userId||s.Account==userId) && s.IsDelete == IsOrNotEnum.否);
        }


        #endregion

        #region  用户批量操作
        public async Task<bool> GiveUserDepartmentsAsync(string userId, string departmentIds)
        {
            var appIdList = departmentIds?.Split(',').ToList();

            var Departments = await departmentDomain.GetDepartmentAsync(appIdList);

            var UserDepartments = await departmentDomain.GetUserDepartmentsAsync(userId);

            //查出需要删除的关联
            var RemoveDepartments = UserDepartments.Select(s => s.Id).Except(appIdList).ToList();
            foreach (var departmentId in RemoveDepartments)
            {
                var department = UserDepartments.FirstOrDefault(s => s.Id == departmentId);
                if (department != null)
                {
                    await entityAssociationDomain.RemoveEntityAssignAsync(userId, departmentId, AssociationTypeEnum.用户与部门, department.AppId);
                }
            }
            //查出需要新增的关联
            var AddDepartments = appIdList.Except(UserDepartments.Select(s => s.Id)).ToList();
            foreach (var departmentId in AddDepartments)
            {
                var department = Departments.FirstOrDefault(s => s.Id == departmentId);
                if(department!=null)
                {
                    await entityAssociationDomain.AddEntityAssignAsync(userId, department.Id, AssociationTypeEnum.用户与部门, department.AppId);
                }
            }
            return true;
        }

        public async Task<bool> GiveUserRolesAsync(string userId, string roleIds)
        {
            var appIdList = roleIds?.Split(',').ToList();

            var Roles = await roleDomain.GetRoleAsync(appIdList);

            var UserRoles = await roleDomain.GetUserRoleAsync(userId);

            //查出需要删除的关联
            var RemoveRoles = UserRoles.Select(s => s.Id).Except(appIdList).ToList();
            foreach (var roleId in RemoveRoles)
            {
                var role = UserRoles.FirstOrDefault(s=>s.Id == roleId);
                if(role!=null)
                    await entityAssociationDomain.RemoveEntityAssignAsync(userId, roleId, AssociationTypeEnum.用户与角色, role.AppId);
            }
            //查出需要新增的关联
            var AddRoles = appIdList.Except(UserRoles.Select(s => s.Id)).ToList();
            foreach (var roleId in AddRoles)
            {
                var role = Roles.FirstOrDefault(s => s.Id == roleId);
                if (role != null)
                {
                    await entityAssociationDomain.AddEntityAssignAsync(userId, roleId, AssociationTypeEnum.用户与角色, role.AppId);
                }
            }
            return true;
        }


        public async Task<bool> GiveUserAssignmentsAsync(string userId, string assignmentIds)
        {
            var assignmentIdArray = assignmentIds?.Split(',').ToList();

            var Assignments = await assignmentDomain.GetAssignmentsAsync(assignmentIdArray);
            var UserAssignments = await assignmentDomain.GetUserAssignmentsAsync(userId);

            //查出需要删除的关联
            var RemoveAssignments = UserAssignments.Select(s => s.Id).Except(assignmentIdArray).ToList();
            foreach (var assignmentId in RemoveAssignments)
            {
                var assignment = UserAssignments.FirstOrDefault(s => s.Id == assignmentId);
                if (assignment != null)
                    await entityAssociationDomain.RemoveEntityAssignAsync(userId, assignmentId, AssociationTypeEnum.用户与任职, assignment.AppId);
            }
            //查出需要新增的关联
            var AddAssignments = assignmentIdArray.Except(UserAssignments.Select(s => s.Id)).ToList();
            foreach (var assignmentId in AddAssignments)
            {
                var assignment = Assignments.FirstOrDefault(s => s.Id == assignmentId);
                if (assignment != null)
                {
                    await entityAssociationDomain.AddEntityAssignAsync(userId, assignmentId, AssociationTypeEnum.用户与任职, assignment.AppId);
                }
            }
            return true;
        }

        public async Task<bool> ChangePasswordAsync(string UserId,string OldPassword, string NewPassword)
        {
            var user= repository.DetachedEntities.FirstOrDefault(s => s.Id == UserId && s.IsDelete == IsOrNotEnum.否);
            if (user == null) throw Oops.Oh("无法修改密码");

            var pwd= MD5Encryption.GetMd5By32(OldPassword + user.AccountCerdictKey);
            if (pwd != user.Password) throw Oops.Oh("旧密码输入错误，无法修改密码");
            user.AccountCerdictKey = AppCore.Guid();
            user.Password = MD5Encryption.GetMd5By32(NewPassword + user.AccountCerdictKey);
            await repository.UpdateIncludeAsync(user, new string[]
            {
                nameof(user.Password),
                nameof(user.AccountCerdictKey)
            });
            return true;

        }

        public async Task<bool> ResetPasswordAsync(string UserId, string NewPassword)
        {
            var user = repository.DetachedEntities.FirstOrDefault(s => s.Id == UserId && s.IsDelete == IsOrNotEnum.否);
            if (user == null) throw Oops.Oh("无法重置密码");
            user.AccountCerdictKey = AppCore.Guid();
            user.Password = MD5Encryption.GetMd5By32(NewPassword + user.AccountCerdictKey);
            await repository.UpdateIncludeAsync(user, new string[]
            {
                nameof(user.Password),
                nameof(user.AccountCerdictKey)
            });
            return true;
        }

        #endregion
    }
}
