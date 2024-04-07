/*
 * Copyright (c) 2024 星曳数据
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 *
 * This file is provided under the Mozilla Public License Version 2.0,
 * and the "NO WARRANTY" clause of the MPL is hereby expressly
 * acknowledged.
 */
using Air.Cloud.Core.App.Options;
using Air.Cloud.Core.Attributes;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Reflection;

namespace Air.Cloud.Core.App
{
    /// <summary>
    /// 应用程序配置信息
    /// </summary>
    /// <remarks>
    /// 包含:应用程序配置信息、应用程序启动地址信息、应用程序PID信息、应用程序启动端口信息
    /// </remarks>
    public static class AppConfiguration
    {
        internal class AppConfigurationDomain
        {
            /// <summary>
            /// 获取系统所在机器的IP地址
            /// </summary>
            /// <returns></returns>
            public static IPAddress GetLocalIPAddress()
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork) return ip;
                }
                return IPAddress.Parse(AppConst.DEFAULT_IP_HOST);
            }

        }

        /// <summary>
        /// 应用程序配置信息
        /// </summary>
        public static IConfiguration Configuration => AppCore.Configuration;
        /// <summary>
        /// 应用程序启动地址信息
        /// </summary>
        public static IPAddress IPAddress => AppConfigurationDomain.GetLocalIPAddress();
        /// <summary>
        /// 应用程序PID信息
        /// </summary>
        public static PIDOptions PID => PIDOptions.Instance;

        /// <summary>
        /// 应用程序启动端口信息
        /// </summary>
        /// <remarks>
        ///  -1表示未知端口
        /// </remarks>
        public static int Port => -1;

        /// <summary>
        /// 获取配置
        /// </summary>
        /// <typeparam name="TOptions">强类型选项类</typeparam>
        /// <param name="path">配置中对应的Key</param>
        /// <param name="loadPostConfigure"></param>
        /// <returns>TOptions</returns>
        public static TOptions GetConfig<TOptions>(string path, bool loadPostConfigure = false)
        {
            try
            {
                var options = Configuration.GetSection(path).Get<TOptions>();

                // 加载默认选项配置
                if (loadPostConfigure && typeof(IConfigurableOptions).IsAssignableFrom(typeof(TOptions)))
                {
                    var postConfigure = typeof(TOptions).GetMethod("PostConfigure");
                    if (postConfigure != null)
                    {
                        options ??= Activator.CreateInstance<TOptions>();
                        postConfigure.Invoke(options, new object[] { options, Configuration });
                    }
                }

                return options;
            }
            catch (NullReferenceException ex)
            {

                throw new Exception("配置文件配置节缺失", ex);
            }

        }


        /// <summary>
        /// 获取配置文件 单个配置
        /// </summary>
        /// <param name="configName"></param>
        /// <returns></returns>
        public static string GetConfig(this IConfiguration configuration, string configName = null)
        {
            return configuration.GetSection(configName).Get<string>();
        }
        /// <summary>
        /// 获取配置文件 单个配置
        /// </summary>
        /// <param name="configName"></param>
        /// <returns></returns>
        public static string GetConfig<T>(this IConfiguration configuration, string configName = null)
        {
            ConfigurationInfoAttribute s = typeof(T).GetCustomAttribute<ConfigurationInfoAttribute>(false);
            if (string.IsNullOrEmpty(configName))
            {
                configName = s?.ConfigurationName;
            }
            return configuration.GetSection(configName).Get<string>();
        }
        /// <summary>
        /// 获取配置文件 多个配置
        /// </summary>
        /// <param name="configName"></param>
        /// <returns></returns>
        public static T[] GetConfigs<T>(this IConfiguration configuration, string configName = null)
        {
            ConfigurationInfoAttribute s = typeof(T).GetCustomAttribute<ConfigurationInfoAttribute>(false);
            if (string.IsNullOrEmpty(configName))
            {
                configName = s?.ConfigurationName;
            }
            return configuration.GetSection(configName).Get<T[]>();
        }
        /// <summary>
        /// 获取配置文件 单个配置
        /// </summary>
        /// <param name="configName">配置名称</param>
        /// <returns></returns>
        public static T GetObjectConfig<T>(this IConfiguration configuration, string configName = null) where T : class, new()
        {
            ConfigurationInfoAttribute s = typeof(T).GetCustomAttribute<ConfigurationInfoAttribute>(false);
            if (string.IsNullOrEmpty(configName))
            {
                configName = s?.ConfigurationName;
            }
            return configuration.GetSection(configName).Get<T>();
        }


        internal static class AppConfigurationObServer
        {
            internal static ConcurrentDictionary<string, Action> Actions = new ConcurrentDictionary<string, Action>();

            public static void TryAdd(KeyValuePair<string, Action> ac)
            {
                Actions.TryAdd(ac.Key, ac.Value);
            }
            static AppConfigurationObServer(){
                ChangeToken.OnChange(Configuration.GetReloadToken, () =>
                {
                    //轮番调用
                    foreach (var item in Actions)
                    {
                        item.Value.Invoke();
                    }
                });
            }
        }
        public  static void AddChangeReloadFunction(string Key,Action Action)
        {
            AppConfigurationObServer.TryAdd(new KeyValuePair<string, Action>(Key,Action));
        }
    }
}
