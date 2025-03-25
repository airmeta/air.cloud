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
using Air.Cloud.Core.App.Options;
using Air.Cloud.Core.Enums;
using Air.Cloud.Core.Modules.AppPrint;
using Air.Cloud.Core.Standard.AppInject;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Reflection;

namespace Air.Cloud.Core.App
{
    /// <summary>
    /// <para>zh-cn:应用程序配置信息</para>
    /// <para>en-us:Application configuration information</para>
    /// </summary>
    /// <remarks>
    /// <para>zh-cn:包含:应用程序配置信息、应用程序启动地址信息、应用程序PID信息、应用程序启动端口信息</para>
    /// <para>en-us:Contains: application configuration information, application startup address information, application PID information, application startup port information</para>
    /// </remarks>
    public static class AppConfiguration
    {
        /// <summary>
        /// <para>zh-cn:应用程序主机信息获取</para>
        /// <para>en-us:Application host information acquisition</para>
        /// </summary>
        internal class AppHostInformation
        {
            /// <summary>
            /// <para>zh-cn:获取本地IP地址</para>
            /// <para>en-us:GetAsync local IP address</para>
            /// </summary>
            /// <returns></returns>
            public static IPAddress GetLocalIPAddress()
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork) return ip;
                }
                return IPAddress.Parse(AppConst.DEFAULT_IP_ADDRESS);
            }

        }

        /// <summary>
        /// <para>zh-cn:应用程序配置信息</para>
        /// <para>en-us:Application configuration information</para>
        /// </summary>
        public static IConfiguration Configuration => AppCore.Configuration;
        /// <summary>
        /// <para>zh-cn:应用程序启动IP地址信息</para>
        /// <para>en-us:Application startup Ip address information</para>
        /// </summary>
        public static IPAddress IPAddress => AppHostInformation.GetLocalIPAddress();

        /// <summary>
        /// <para>zh-cn:应用程序PID信息</para>
        /// <para>en-us:Application PID information</para>
        /// </summary>
        public static string PID => AppRealization.PID.Get();

        /// <summary>
        /// <para>zh-cn:应用程序启动端口信息</para>
        /// <para>en-us:Application startup port information</para>
        /// </summary>
        /// <remarks>
        ///  <para>zh-cn: -1表示未知端口,一般该参数由模块去宿主机中获取并赋值</para>
        ///  <para>en-us: -1 indicates an unknown port, generally this parameter is obtained and assigned by the module to the host</para>
        /// </remarks>
        public static int Port => -1;

        /// <summary>
        /// <para>zh-cn:获取配置信息</para>
        /// <para>en-us:GetAsync configuration information</para>
        /// </summary>
        /// <typeparam name="TOptions">
        /// <para>zh-cn:强类型选项类</para>
        /// <para>en-us:Strongly typed option class</para>
        /// </typeparam>
        /// <param name="path">
        /// <para>zh-cn:配置路径</para>
        /// <para>en-us:Configuration path</para>
        /// </param>
        /// <param name="loadPostConfigure">
        ///  <para>zh-cn:是否加载后期配置</para>
        ///  <para>en-us:Whether to load post configuration</para>
        /// </param>
        /// <returns>TOptions</returns>
        public static TOptions GetConfig<TOptions>(string path=null, bool loadPostConfigure = false)
        {
            try
            {
                if (path.IsNullOrEmpty())
                {
                    ConfigurationInfoAttribute s = typeof(TOptions).GetCustomAttribute<ConfigurationInfoAttribute>(false);
                    if(s==null) throw new ArgumentNullException("未能查询到"+nameof(TOptions)+"得配置");
                    path = s.ConfigurationName;
                }

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
                throw new Exception("未能加载到"+ path + "配置信息",ex);
            }catch(Exception ex)
            {
                AppRealization.Output.Print(new AppPrintInformation
                {
                    State = true,
                    AdditionalParams = new Dictionary<string, object>()
                    {
                        { ex.Message,ex}
                    },
                    Content = "加载应用程序选项时出现异常",
                    Level = AppPrintLevel.Error,
                    Title = ex.Message,
                });
                throw;
            }

        }

        /// <summary>
        /// <para>zh-cn:获取多个配置信息</para>
        /// <para>en-us:GetAsync multiple configuration information</para>
        /// </summary>
        /// <param name="configName">
        /// <para>zh-cn:配置名称</para>
        /// <para>en-us:Configuration name</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:多个配置信息</para>
        /// <para>en-us:multiple Configuration information</para>
        /// </returns>
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
        /// <para>zh-cn:获取单个配置信息</para>
        /// <para>en-us:GetAsync single configuration information</para>
        /// </summary>
        /// <param name="configName">
        /// <para>zh-cn:配置名称</para>
        /// <para>en-us:Configuration name</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:配置信息</para>
        /// <para>en-us:Configuration information</para>
        /// </returns>
        public static T GetConfig<T>(this IConfiguration configuration, string configName = null) where T : class, new()
        {
            ConfigurationInfoAttribute s = typeof(T).GetCustomAttribute<ConfigurationInfoAttribute>(false);
            if (string.IsNullOrEmpty(configName))
            {
                configName = s?.ConfigurationName;
            }
            return configuration.GetSection(configName).Get<T>();
        }

        /// <summary>
        /// <para>zh-cn:应用程序配置监听器</para>
        /// <para>en-us:Application configuration listener</para>
        /// </summary>
        internal static class AppConfigurationObServer
        {
            /// <summary>
            /// <para>zh-cn:需要监听的配置</para>
            /// <para>en-us:Configuration to listen to</para>
            /// </summary>
            internal static ConcurrentDictionary<string, Action> Actions = new ConcurrentDictionary<string, Action>();

            /// <summary>
            /// <para>zh-cn:尝试添加配置信息监听动作</para>
            /// <para>en-us:Try to add configuration information listening action</para>
            /// </summary>
            /// <param name="ac">
            /// <para>zh-cn:配置信息,监听到配置变化的动作 </para>
            /// <para>en-us:Configuration information, action when configuration changes</para>
            /// </param>
            public static void TryAddListen(KeyValuePair<string, Action> ac)
            {
                Actions.TryAdd(ac.Key, ac.Value);
            }
            /// <summary>
            /// <para>zh-cn:开始监听</para>
            /// <para>en-us:Start listening</para>
            /// </summary>
            public static void StartListen()
            {
                ChangeToken.OnChange(Configuration.GetReloadToken, () =>
                {
                    foreach (var item in Actions)
                    {
                        item.Value.Invoke();
                    }
                });
            }
        }

        /// <summary>
        /// <para>zh-cn:添加配置变化重新加载函数</para>
        /// <para>en-us:Add configuration change reload function</para>
        /// </summary>
        /// <param name="Key"></param>
        /// <param name="Action"></param>
        public  static void AddChangeReloadFunction(string Key,Action Action)
        {
            AppConfigurationObServer.TryAddListen(new KeyValuePair<string, Action>(Key,Action));
        }
        /// <summary>
        /// <para>zh-cn:开始监听配置变化</para>
        /// <para>en-us:Start listening for configuration changes</para>
        /// </summary>
        public static void StartListenChangeReloadFunction()
        {
            AppConfigurationObServer.StartListen();
        }

        /// <summary>
        /// <para>zh-cn:默认的加载配置文件的方法</para>
        /// <para>en-us:Default method for loading configuration files</para>
        /// </summary>
        /// <typeparam name="TAppInjectImplementation">
        ///  <para>zh-cn:应用注入标准实现</para>
        ///  <para>en-us:Application injection standard implementation</para>
        /// </typeparam>
        /// <param name="AppStartupTypeEnum">
        /// <para>zh-cn:应用程序启动类型</para>
        /// <para>en-us:Application startup type</para>
        /// </param>
        /// <param name="loadConfigurationTypeEnum">
        /// <para>zh-cn:加载配置文件的方式</para>
        /// <para>en-us:Method of loading configuration files</para>
        /// <returns></returns>
        public static ConfigurationManager AppDefaultInjectConfiguration<TAppInjectImplementation>(
            AppStartupTypeEnum AppStartupTypeEnum,
            LoadConfigurationTypeEnum loadConfigurationTypeEnum)
        where TAppInjectImplementation : IAppInjectStandard, new()
        {
            AppRealization.Configuration.LoadConfiguration(AppConst.SystemEnvironmentConfigFileFullName, false);
            AppRealization.Configuration.LoadConfiguration(AppConst.CommonEnvironmentConfigFileFullName, true);
            AppConst.LoadConfigurationTypeEnum = loadConfigurationTypeEnum;
            AppConst.ApplicationName = Assembly.GetCallingAssembly().GetName().Name;
            AppConst.ApplicationInstanceName = $"{AppConst.ApplicationName}_{AppRealization.PID.Get()}";
            AppCore.AppStartType = AppStartupTypeEnum;
            AppRealization.SetDependency<IAppInjectStandard>(new TAppInjectImplementation());
            return AppConfigurationLoader.Configurations;
        }

    }
}
