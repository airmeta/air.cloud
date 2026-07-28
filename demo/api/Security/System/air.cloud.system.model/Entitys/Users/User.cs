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
using air.cloud.security.common.Base;
using air.cloud.security.common.Enums;

namespace air.cloud.system.model.Entitys.Users
{
    /// <summary>
    /// <para>用户实体</para>
    /// </summary>
    [Table("SYS_USER")]
    public class User:AllEntityBase
    {
        /// <summary>
        /// <para>用户名</para>
        /// </summary>
        [Column("USER_NAME")]
        public string? UserName { get; set; }
        /// <summary>
        /// <para>所属应用用户标识</para>
        /// </summary>
        [Column("APP_USER_ID")]
        public string? AppUserId { get; set; }

        /// <summary>
        /// <para>账号</para>
        /// </summary>
        [Column("ACCOUNT")]
        public string? Account { get; set; }

        /// <summary>
        /// <para>密码</para>
        /// </summary>
        [Column("PASSWORD")]
        public string? Password { get; set; }

        /// <summary>
        /// <para>账号加密密钥</para>
        /// </summary>
        [Column("ACCOUNT_CERDICT")]
        public string? AccountCerdictKey { get; set; }

        /// <summary>
        /// <para>证件号码</para>
        /// </summary>
        [Column("ID_CARDNO")]
        public string? IdCardNo { get; set; }

        /// <summary>
        /// <para>邮件</para>
        /// </summary>
        [Column("Email")]
        public string? Email { get; set; }

        /// <summary>
        /// <para>电话号码</para>
        /// </summary>
        [Column("PHONE_NUMBER")]
        public string? PhoneNumber { get; set; }

        /// <summary>
        /// 创建账户的应用
        /// </summary>
        [Column("ACCOUNT_CREATE_APPID")]
        public string AccountCreateAppId { get; set; }


        /// <summary>
        /// 是否三方平台用户(0否 1是)
        /// </summary>
        /// <remarks>
        /// 从统一身份认证平台建立的账户都是 否 外部进来的都是 是
        /// </remarks>
        [Column("IS_THIRD_PLATFORM_USER")]
        public IsOrNotEnum IsThirdPlatformUser { get; set; } = IsOrNotEnum.否;

    }
}
