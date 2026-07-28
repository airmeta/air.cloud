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
using air.cloud.system.model.Dtos.UserDtos;
using air.cloud.security.common.Enums;
using air.cloud.system.model.Entitys.Roles;

using Microsoft.EntityFrameworkCore;

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace air.cloud.system.model.Dtos.AccountDtos
{
    public  class UserAccountInfo
    {
        /// <summary>
        /// 用户编号
        /// </summary>
        public string Id { get; set; }  
        /// <summary>
        /// 账号
        /// </summary>
        public string Account { get; set; }
        /// <summary>
        ///  账户创建时的应用编号
        /// </summary>
        public string AccountCreateAppId { get; set; }

        /// <summary>
        /// 用户信息
        /// </summary>
        public UserRDto User { get; set; }
        /// <summary>
        /// 菜单权限信息
        /// </summary>
        public IList<AuthorityItem> Authoritys { get; set; }

        /// <summary>
        /// 用户组
        /// </summary>
        public List<string> UserGroups { get; set; }


        /// <summary>
        /// 当前应用名称
        /// </summary>
        public string CurrentAppName { get; set; }

        /// <summary>
        /// 当前应用编号
        /// </summary>
        public string CurrentAppId { get; set; }    
    }

    public class AuthorityItem
    {
        #region 字段
        /// <summary>
        /// 菜单分类
        /// </summary>
        public MenuTypeEnum Type { get; set; }

        /// <summary>
        /// 菜单名
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// 图标
        /// </summary>
        public string Icon { get; set; }

        /// <summary>
        /// 前端路由地址
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// 前端组件地址
        /// </summary>
        public string Component { get; set; }

        /// <summary>
        /// 排序号
        /// </summary>
        public int? SortNumber { get; set; }

        /// <summary>
        /// 菜单编号
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// 上级菜单编号
        /// </summary>
        public string ParentId { get; set; }

        /// <summary>
        /// 权限
        /// </summary>
        public string Authority { get; set; }

        /// <summary>
        /// 是否隐藏
        /// </summary>
        public IsOrNotEnum Hide { get; set; }

        /// <summary>
        /// 菜单编号(兼容其他前端框架)
        /// </summary>
        public string MenuId { get
            {
                return Id;
            }
        }

        /// <summary>
        /// 元数据
        /// </summary>
        public string Meta { get; set; }


        #endregion
        /// <summary>
        /// 子项
        /// </summary>
        public IList<AuthorityItem> Children { get; set; }

    }
}
