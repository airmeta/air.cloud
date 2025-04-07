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
namespace Air.Cloud.DataBase.ElasticSearch.Attributes
{
    /// <summary>
    /// <para>zh-cn:NoSQL数据表特性</para>
    /// <para>en-us:NoSql table Attribute</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ElasticSearchIndexAttribute : Attribute
    {
        /// <summary>
        /// <para>zh-cn:表名</para>
        /// <para>en-us:table name</para>
        /// </summary>
        public string TableName { get; set; }
        /// <summary>
        /// <para>zh-cn:Schema</para>
        /// <para>en-us:Schema</para>
        /// </summary>
        public string Schema { get; set; }

        /// <summary>
        /// <para>zh-cn:库标识</para>
        /// <para>en-us:DataBase key</para>
        /// </summary>
        public string DbKey { get; set; }
    }
}
