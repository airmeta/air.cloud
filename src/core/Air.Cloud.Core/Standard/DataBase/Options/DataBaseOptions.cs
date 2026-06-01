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
    /// <para>zh-cn:表示数据库配置根节点，承载多个数据库连接配置。</para>
    /// <para>en-us:Represents the root database configuration node, carrying multiple database connection options.</para>
    /// </summary>
    [ConfigurationInfo("DataBaseSettings")]
    public class DataBaseOptions
    {
        /// <summary>
        /// <para>zh-cn:表示非关系型数据库类型。</para>
        /// <para>en-us:Represents a non-relational database type.</para>
        /// </summary>
        /// <remarks>
        /// <para>zh-cn:示例包括 Elasticsearch、MongoDB 和 Redis。</para>
        /// <para>en-us:Examples include Elasticsearch, MongoDB, and Redis.</para>
        /// </remarks>
        public const string 非关系型 = "NOSQL";
        /// <summary>
        /// <para>zh-cn:表示关系型数据库类型。</para>
        /// <para>en-us:Represents a relational database type.</para>
        /// </summary>
        /// <remarks>
        /// <para>zh-cn:示例包括 MySQL、Oracle、SQL Server、PostgreSQL 和 SQLite。</para>
        /// <para>en-us:Examples include MySQL, Oracle, SQL Server, PostgreSQL, and SQLite.</para>
        /// </remarks>
        public const string 关系型 = "SQL";

        /// <summary>
        /// <para>zh-cn:获取或设置数据库配置项集合。</para>
        /// <para>en-us:Gets or sets the database option collection.</para>
        /// </summary>
        public IList<DataBaseOption> Options { get; set; }
    }

    /// <summary>
    /// <para>zh-cn:表示单个数据库连接配置项。</para>
    /// <para>en-us:Represents a single database connection option.</para>
    /// </summary>
    public class DataBaseOption
    {
       /// <summary>
       /// <para>zh-cn:获取或设置数据库配置名称。</para>
       /// <para>en-us:Gets or sets the database option name.</para>
       /// </summary>
        public string Key { get; set; }
        /// <summary>
        /// <para>zh-cn:获取数据库配置唯一标识，由配置名称计算得到。</para>
        /// <para>en-us:Gets the unique database option identifier calculated from the option name.</para>
        /// </summary>
        public string UID => MD5Encryption.GetMd5By32(Key);
        /// <summary>
        /// <para>zh-cn:获取或设置数据库连接字符串。</para>
        /// <para>en-us:Gets or sets the database connection string.</para>
        /// </summary>
        public string ConnectionString { get; set; }
        /// <summary>
        /// <para>zh-cn:获取或设置数据库服务 IP 地址。</para>
        /// <para>en-us:Gets or sets the database service IP address.</para>
        /// </summary>
        public string IpAddress { get; set; }
        /// <summary>
        /// <para>zh-cn:获取或设置数据库服务端口号。</para>
        /// <para>en-us:Gets or sets the database service port.</para>
        /// </summary>
        public string Port { get; set; }
        /// <summary>
        /// <para>zh-cn:获取或设置数据库账号。</para>
        /// <para>en-us:Gets or sets the database account.</para>
        /// </summary>
        public string Account { get; set; }
        /// <summary>
        /// <para>zh-cn:获取或设置数据库密码。</para>
        /// <para>en-us:Gets or sets the database password.</para>
        /// </summary>
        public string Password { get; set; }
        /// <summary>
        /// <para>zh-cn:获取或设置数据库类型。</para>
        /// <para>en-us:Gets or sets the database type.</para>
        /// </summary>
        /// <remarks>
        /// <para>zh-cn:当前约定包含非关系型数据库和关系型数据库，分别使用 `NOSQL` 和 `SQL` 表示。</para>
        /// <para>en-us:The current convention includes non-relational and relational databases, represented by `NOSQL` and `SQL` respectively.</para>
        /// </remarks>
        public string Type { get; set; } = DataBaseOptions.关系型;
    }
}
