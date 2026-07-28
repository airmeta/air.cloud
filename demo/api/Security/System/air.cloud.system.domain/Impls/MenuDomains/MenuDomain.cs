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
using air.cloud.system.model.Dtos.MenuDtos;
using air.cloud.system.model.Entitys.Roles;

using Air.Cloud.Core.App;
using Air.Cloud.Core.Extensions;
using Air.Cloud.Core.Extensions.Linqs;
using Air.Cloud.EntityFrameWork.Core.Repositories;
using Air.Cloud.WebApp.FriendlyException;

using Mapster;

using Microsoft.EntityFrameworkCore;

using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace air.cloud.system.domain.Impls.MenuDomains
{
    public  class MenuDomain : IMenuDomain
    {
        private readonly IRepository<Menu> repository;
        private readonly IEntityAssociationDomain entityAssociationDomain;
        public MenuDomain(IRepository<Menu> repository,IEntityAssociationDomain entityAssociationDomain)
        {
            this.repository = repository;
            this.entityAssociationDomain = entityAssociationDomain;
        }

        public async Task<string> CreateMenuAsync(MenuSDto dto)
        {
            if (!dto.ParentId.IsNullOrEmpty())
            {
                var exitsParentMenu = await repository.DetachedEntities.FirstOrDefaultAsync(x => x.AppId == dto.AppId && x.Id == dto.ParentId);
                if (exitsParentMenu == null) throw Oops.Oh("上级菜单不存在");
                dto.AppId= exitsParentMenu.AppId;
            }
            var Menu = dto.Adapt<Menu>();
            Menu.Id = AppCore.Guid();
            await repository.InsertAsync(Menu);
            return Menu.Id;
        }

        public async Task<bool> DeleteMenuAsync(string AppId, string menuId)
        {
            var childMenus = await repository.DetachedEntities.Where(x => x.AppId == AppId && x.ParentId == menuId).ToListAsync();
            if (childMenus.Count>0) throw new Exception("请先删除子菜单或按钮");

            var Menu = await repository.DetachedEntities.FirstOrDefaultAsync(x => x.AppId == AppId && x.Id == menuId);  

            if (Menu == null) return true;

            await repository.DeleteAsync(Menu);
            return true;
        }

        public Task<Menu> GetMenuAsync(string MenuId)
        {
            var Menu = repository.DetachedEntities.FirstOrDefaultAsync(x =>x.Id == MenuId);    

            return Menu;
        }

        public async Task<List<Menu>> GetMenusByRoleIdsAsync(List<string> RoleIds,string AppId)
        {
            //角色所有关联的菜单信息
            var MenuAssociations = await entityAssociationDomain.GetEntityAssociationsQueryableAsync(RoleIds, AppId, AssociationTypeEnum.角色与菜单权限);

            var Menus= from menu in repository.DetachedEntities.Where(s=>s.IsDelete== IsOrNotEnum.否&&s.AppId==AppId&&s.Hide== IsOrNotEnum.否)
                       join ma in MenuAssociations
                       on menu.Id equals ma.TargetEntityId
                       select menu;

            return await Menus.ToListAsync();

        }

        public async Task<PageList<Menu>> QueryMenusAsync(BaseQDto dto)
        {
            var linq = LinqExpressionExtensions.And<Menu>();
            if (!dto.AppId.IsNullOrEmpty())
            {
                linq = linq.And(s => s.AppId == dto.AppId);
            }
            if (!dto.Info.IsNullOrEmpty())
            {
                linq = linq.And(x => x.Title.Contains(dto.Info) || x.Path.Contains(dto.Info)||x.Component.Contains(dto.Info));
            }
            var Menus = repository.DetachedEntities.AsNoTracking().Where(linq).AsQueryable();
            return await Menus.OrderByDescending(s=>s.CreateTime).ToPageListAsync<Menu>(dto.Page, dto.Limit);
        }
        public async Task<string> UpdateMenuAsync(MenuSDto dto)
        {
           var Menu = await repository.DetachedEntities.FirstOrDefaultAsync(x => x.AppId == dto.AppId && x.Id == dto.Id);
            if (Menu == null) return string.Empty;
            if(dto.Id==dto.ParentId) throw Oops.Oh("上级菜单不能选择自己");
            if (!dto.ParentId.IsNullOrEmpty())
            {
                var exitsParentMenu = await repository.DetachedEntities.FirstOrDefaultAsync(x => x.AppId == dto.AppId && x.Id == dto.ParentId);
                if (exitsParentMenu == null) throw Oops.Oh("上级菜单不存在");
                dto.AppId= exitsParentMenu.AppId;
            }
            Menu = dto.Adapt<MenuSDto, Menu>(Menu);
            await repository.UpdateExcludeAsync(Menu,new string[]
            {
                nameof(Menu.CreateTime),
                nameof(Menu.CreateUserId),
                nameof(Menu.CreateUserName),
                nameof(Menu.Id),
                nameof(Menu.AppId)
            });
            return Menu.Id;

        }



    }
}
