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
namespace Air.Cloud.Modules.RedisCache.Options
{
    /// <summary>
    /// <para>zh-cn:Redis配置选项</para>
    /// <para>en-us:Redis configuration options</para>
    /// </summary>
    public class RedisSettingsOptions
    {
        /// <summary>
        /// <para>zh-cn:Redis连接字符串</para>
        /// .<para>en-us:Redis connection string</para>
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// <para>zh-cn:用户名</para>
        /// <para>en-us:User name</para>
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// <para>zh-cn:密码</para>
        /// <para>en-us:Password</para>
        /// </summary>
        public string Password { get; set; }




    }
}
