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
using Air.Cloud.Core;
using Air.Cloud.Core.App;
using Air.Cloud.Core.Extensions;
using Air.Cloud.Core.Extensions.Aspects;
using Air.Cloud.Core.Modules.AppAspect.Attributes;
using Air.Cloud.Core.Standard.Print;
using Air.Cloud.Modules.RedisCache.Options;

using StackExchange.Redis;

using System;
using System.ComponentModel;
using System.Reflection;

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
        [Aspect(typeof(IfNullReferenceException))]
        [Description("获取Redis链接")]
        private static ConnectionMultiplexer Connect()
        {
            string? ConnectionString = AppCore.GetOptions<RedisSettingsOptions>()?.ConnectionString;
            return ConnectionMultiplexer.Connect(ConnectionString);
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
    }
}
