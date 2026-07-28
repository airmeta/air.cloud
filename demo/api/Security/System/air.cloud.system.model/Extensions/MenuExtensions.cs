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
using air.cloud.system.model.Dtos.AccountDtos;
using air.cloud.system.model.Entitys.Roles;

using Air.Cloud.Core.Extensions;

namespace air.cloud.system.model.Extensions
{
    public static class MenuExtensions
    {
        public static IList<AuthorityItem> GetAuthorities(this IList<Menu> menus, string MenuId = null)
        {
            IList<AuthorityItem> items = new List<AuthorityItem>();
            if (MenuId.IsNullOrEmpty())
            {
                List<Menu> ChildrenMenus = menus.Where(s => s.ParentId.IsNullOrEmpty()).ToList();
                foreach (var item in ChildrenMenus)
                {
                    items.Add(new AuthorityItem()
                    {
                        SortNumber = item.SortNumber,
                        Children = GetAuthorities(menus, item.Id),
                        Component = item.Component,
                        Icon = item.Icon,
                        Path = item.Path,
                        Title = item.Title,
                        Type = item.Type,
                        Id=item.Id,
                        Hide=item.Hide,
                        Authority=item.Authority,
                        Meta=string.Empty
                    });
                }
                return items;
            }
            else
            {
                List<Menu> ChildrenMenus = menus.Where(s => s.ParentId == MenuId).ToList();
                foreach (var item in ChildrenMenus)
                {
                    items.Add(new AuthorityItem()
                    {
                        SortNumber = item.SortNumber,
                        Children = GetAuthorities(menus, item.Id),
                        Component = item.Component,
                        Icon = item.Icon,
                        Path = item.Path,
                        Title = item.Title,
                        Type = item.Type,
                        Id = item.Id,
                        Hide = item.Hide,
                        Authority = item.Authority,
                        Meta = string.Empty
                    });
                }
                return items;
            }
        }

    }
}
