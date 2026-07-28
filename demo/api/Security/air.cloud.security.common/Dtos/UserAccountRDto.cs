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
namespace air.cloud.security.common.Dtos
{
    public  class UserAccountRDto
    {
        /// <summary>
        /// 统一身份认证平台用户标识
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// 三方应用用户标识
        /// </summary>
        public string AppUserId { get; set; }
        /// <summary>
        /// 账户信息
        /// </summary>
        public string Account { get; set; }
        /// <summary>
        /// 用户名
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// 账户属性
        /// </summary>
        public List<UserAccountPropRDto> AccountProps { get; set; }
        /// <summary>
        /// 票据信息
        /// </summary>
        public string Ticket { get; set; }
        /// <summary>
        /// 超时时间(秒)
        /// </summary>
        public int ExpiredTime { get; set; }

    }

    public class UserAccountPropRDto
    {
        /// <summary>
        /// 键
        /// </summary>
        public string Key { get; set; }
        /// <summary>
        /// 值
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// 是否获得该属性的授权
        /// </summary>
        public string IsAuth { get; set; }
        /// <summary>
        /// 描述
        /// </summary>
        public string Description { get; set; }

    }

}
