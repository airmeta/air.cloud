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
using Air.Cloud.Core.App;
using Air.Cloud.Core.Aspects;
using Air.Cloud.Core.Modules.AppAspect.Attributes;
using Air.Cloud.Modules.RedisCache.Options;

using StackExchange.Redis;

using System.ComponentModel;

namespace Air.Cloud.Modules.RedisCache.Provider
{
    /// <summary>
    /// Redis基本信息初始化辅助类
    /// </summary>
    internal static class RedisConnectorProvider
    {
        private static readonly object Locker = new object();
        private static ConnectionMultiplexer _instance;
        /// <summary>
        /// 单例获取
        /// </summary>
        public static ConnectionMultiplexer Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (Locker)
                    {
                        if (_instance == null || !_instance.IsConnected)
                        {
                            _instance = Connect();
                        }
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// <para>zh-cn:获取Redis链接</para>
        /// <para>en-us:Get Redis Connection</para>
        /// </summary>
        /// <returns></returns>
        [Aspect(typeof(IfNullReferenceExceptionHandler))]
        [Description("获取Redis链接")]
        private static ConnectionMultiplexer Connect()
        {
            RedisSettingsOptions Connection = AppCore.GetOptions<RedisSettingsOptions>();
            return ConnectionMultiplexer.Connect(Connection.ConnectionString, (x) =>
            {
                x.User = Connection?.UserName??string.Empty;
                x.Password= Connection?.Password??string.Empty;
            });
        }
        public static ConnectionMultiplexer SetEventHandler(
            this ConnectionMultiplexer connection,
            Action<object, EndPointEventArgs> MuxerConfigurationChanged = null,
            Action<object, RedisErrorEventArgs> MuxerErrorMessage = null,
            Action<object, ConnectionFailedEventArgs> MuxerConnectionRestored = null,
            Action<object, ConnectionFailedEventArgs> MuxerConnectionFailed = null,
            Action<object, HashSlotMovedEventArgs> MuxerHashSlotMoved = null,
            Action<object, InternalErrorEventArgs> MuxerInternalError = null
         )
        {
            //注册如下事件
            connection.ConnectionFailed += (s, e) =>
            {
                MuxerConnectionFailed(s, e);
            };
            connection.ConnectionRestored += (s, e) =>
            {
                MuxerConnectionRestored(s, e);
            };
            connection.ErrorMessage += (s, e) =>
            {
                MuxerErrorMessage(s, e);
            };
            connection.ConfigurationChanged += (s, e) =>
            {
                MuxerConfigurationChanged(s, e);
            };
            connection.HashSlotMoved += (s, e) =>
            {
                MuxerHashSlotMoved(s, e);
            };
            connection.InternalError += (s, e) =>
            {
                MuxerInternalError(s, e);
            };
            return connection;
        }

        /// <summary>
        /// <para>zh-cn:切换Redis数据库地址</para>
        /// <para>en-us:Switch Redis database address</para>
        /// </summary>
        /// <param name="Connection">
        ///  <para>zh-cn:Redis数据库连接信息</para>
        ///  <para>en-us:Redis database connection information</para>
        /// </param>
        /// <returns>
        ///  <para>zh-cn:新的连接信息</para>
        ///  <para>en-us:New connection information</para>
        /// </returns>
        /// <remarks>
        ///  <para>zh-cn:如果需要切换Redis数据库地址，请使用此方法</para>
        ///  <para>en-us:If you need to switch the Redis database address, please use this method</para>
        /// </remarks>
        public static ConnectionMultiplexer Change(RedisSettingsOptions Connection,bool UseToGlobal=false)
        {
            var instance= ConnectionMultiplexer.Connect(Connection.ConnectionString, (x) =>
            {
                x.User = Connection?.UserName ?? string.Empty;
                x.Password = Connection?.Password ?? string.Empty;
            });
            if (UseToGlobal) _instance = instance;
            return instance;
        }

    }
}
