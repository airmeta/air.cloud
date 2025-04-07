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
using Air.Cloud.DataBase.ElasticSearch.Connections;

namespace Air.Cloud.DataBase.ElasticSearch
{
    /// <summary>
    /// <para>zh-cn:ElasticSearch连接信息</para>
    /// <para>en-us:ElasticSearchConnection</para>
    /// </summary>
    public class ElasticSearchConnection
    {
        /// <summary>
        /// <para>zh-cn:ElasticSearch连接池信息</para>
        /// <para>en-us:ElasticSearchConnectionPool</para>
        /// </summary>
        public static ElasticSearchConnectionPool Pool = new ElasticSearchConnectionPool();

        /// <summary>
        /// <para>zh-cn:连接名称</para>
        /// <para>en-us:ConnectionName</para>
        /// </summary>
        public string ConnectionName { get; set; }
        /// <summary>
        /// <para>zh-cn:连接编号</para>
        /// <para>en-us:ConnectionUID</para>
        /// </summary>
        public string ConnectionUID => MD5Encryption.GetMd5By32(ConnectionName);
        /// <summary>
        /// <para>zh-cn:ElasticSearch客户端连接</para>
        /// <para>en-us:ElasticSearch Client</para>
        /// </summary>
        public IElasticClient? Connection => Pool.Get(ConnectionUID)?.Client;
    }
}
