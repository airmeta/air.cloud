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
using Air.Cloud.Core.Plugins.Security.MD5;

namespace Air.Cloud.Core.Standard.DataBase.Options
{
    /// <summary>
    /// <para>zh-cn:数据库配置信息</para>
    /// <para>en-us:DataBase options</para>
    /// </summary>
    [ConfigurationInfo("DataBaseSettings")]
    public class DataBaseOptions
    {
        /// <summary>
        /// 非关系型数据库
        /// </summary>
        /// <remarks>
        /// ES,MongoDB,Redis
        /// </remarks>
        public const string 非关系型 = "NOSQL";
        /// <summary>
        /// 关系型数据库
        /// </summary>
        /// <remarks>
        /// MySQL,Oracle,SQLServer,PostgreSQL,SQLite
        /// </remarks>
        public const string 关系型 = "SQL";


        public IList<DataBaseOption> Options { get; set; }
    }

    public class DataBaseOption
    {
       /// <summary>
       /// 数据库名称
       /// </summary>
        public string Key { get; set; }
        /// <summary>
        /// 数据库配置唯一标识
        /// </summary>
        public string UID => MD5Encryption.GetMd5By32(Key);
        /// <summary>
        /// 连接字符串
        /// </summary>
        public string ConnectionString { get; set; }
        /// <summary>
        /// IP地址
        /// </summary>
        public string IpAddress { get; set; }
        /// <summary>
        /// 端口号
        /// </summary>
        public string Port { get; set; }
        /// <summary>
        /// 账号
        /// </summary>
        public string Account { get; set; }
        /// <summary>
        /// 密码
        /// </summary>
        public string Password { get; set; }
        /// <summary>
        /// 类型
        /// </summary>
        /// <remarks>
        ///  目前只有两种类型: 非关系型数据库和关系型数据库 分别使用 NoSQL 和 SQL 表示
        /// </remarks>
        public string Type { get; set; } = DataBaseOptions.关系型;
    }
}
