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
using Microsoft.Extensions.Options;

namespace Air.Cloud.DataBase.Options
{
    /// <summary>
    /// <para>zh-cn:数据源配置选项</para>
    /// <para>en-us:Data Source Configuration Options</para>
    /// </summary>
    public class DataSourceOptions
    {
        /// <summary>
        /// <para>zh-cn:连接验证SQL语句</para>
        /// <para>en-us:Connection Validation SQL Statement</para>
        /// </summary>
        public string ConnectionValidationSQL { get;set; }=string.Empty;

        /// <summary>
        /// <para>zh-cn:连接验证间隔时间（毫秒）</para>
        /// <para>en-us:Connection Validation Interval (Milliseconds)</para>
        /// </summary>
        /// <remarks>
        ///  <para>默认值:1分钟</para>
        ///  <para>Default Value: 1 minute</para>
        /// </remarks>
        public int ConnectionValidationIntervalMillis { get;set; } = 60000;

        /// <summary>
        /// <para>zh-cn:数据库连接字符串集合</para>
        /// <para>en-us:Database Connection String Collection</para>
        /// </summary>
        public IDictionary<string,string> ConnectionStrings { get;set; }


        /// <summary>
        /// <para>zh-cn:获取指定名称的连接字符串</para>
        /// <para>en-us:Get the connection string for the specified name</para>
        /// </summary>
        /// <param name="name">
        ///   <para>zh-cn:连接字符串名称</para>
        ///   <para>en-us:Connection string name</para>
        /// </param>
        /// <returns>
        ///  <para>zh-cn:连接字符串，如果不存在则返回空字符串</para>
        ///  <para>en-us:The connection string, or an empty string if it does not exist</para>
        /// </returns>
        public string  GetConnectionString(string name)
        {
            if (ConnectionStrings!=null&& ConnectionStrings.ContainsKey(name))
            {
                return ConnectionStrings[name];
            }
            return string.Empty;
        }

        /// <summary>
        /// <para>zh-cn:设置指定名称的连接字符串</para>
        /// <para>en-us:Set the connection string for the specified name</para>
        /// </summary>
        /// <param name="name">
        ///  <para>zh-cn:连接字符串名称</para>
        ///  <para>en-us:Connection string name</para>
        /// </param>
        /// <param name="connectionString">
        ///  <para>zh-cn:连接字符串</para>
        ///  <para>en-us:Connection string</para>
        /// </param>
        /// <returns>
        ///  <para>zh-cn:设置后的连接字符串</para>
        ///  <para>en-us:The set connection string</para>
        /// </returns>
        public string SetConnectionString(string name,string connectionString)
        {
            if (ConnectionStrings==null)
            {
                ConnectionStrings=new Dictionary<string, string>();
            }
            ConnectionStrings[name]=connectionString;
            return connectionString;
        }


        /// <summary>
        /// <para>zh-cn:启用数据库状态检查</para>
        /// <para>en-us:Enable Database Status Check</para>
        /// </summary>
        /// <returns>
        ///  <para>zh-cn:如果启用则返回true，否则返回false</para>
        ///  <para>en-us:Returns true if enabled, otherwise false</para>
        /// </returns>
        public bool EnableDatabaseStatusCheck()
        {
            if (ConnectionValidationSQL.IsNullOrEmpty())
            {
                return false;
            }
            return true;
        }
    }
}
