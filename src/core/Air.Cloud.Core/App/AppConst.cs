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
using Air.Cloud.Core.Enums;
using Air.Cloud.Core.Standard.Container.Model;

namespace Air.Cloud.Core.App
{
    /// <summary>
    /// <para>zh-cn: 应用程序常量</para>
    /// <para>en-us: Application constants</para>
    /// </summary>
    public static class AppConst
    {
        /// <summary>
        /// <para>zh-cn: 默认配置文件</para>
        /// <para>en-us: Default configuration file</para>
        /// </summary>
        public const string DEFAULT_CONFIG_FILE = "appsettings.json";
        /// <summary>
        /// <para>zh-cn: 环境配置文件</para>
        /// <para>en-us: Environment configuration file</para>
        /// </summary>
        /// <remarks>
        /// <para>zh-cn: 根据当前环境变量动态加载配置文件</para>
        /// <para>en-us: Dynamically load configuration files based on current environment variables</para>
        /// </remarks>
        public const string ENVIRONMENT_CONFIG_FILE = "appsettings.{0}.json";
        /// <summary>
        /// <para>zh-cn: 手动指定环境配置Key</para>
        /// <para>en-us: Environment configuration key  </para>
        /// </summary>
        public const string ENVIRONMENT = "Environment";
        /// <summary>
        /// <para>zh-cn: 系统环境配置文件全名称</para>
        /// <para>en-us: System environment configuration file full name</para>
        /// </summary>
        public static string SystemEnvironmentConfigFileFullName => string.Format(ENVIRONMENT_CONFIG_FILE, AppEnvironment.VirtualEnvironmentKey);
        /// <summary>
        /// <para>zh-cn:全局公共配置文件全名称</para>
        /// <para>en-us: Global common configuration file full name</para>
        /// </summary>
        public static string CommonEnvironmentConfigFileFullName => string.Format(ENVIRONMENT_CONFIG_FILE, $"Common.{AppEnvironment.VirtualEnvironmentKey}");
        /// <summary>
        /// <para>zh-cn: 默认IP地址</para>
        /// <para>en-us: Default IP address</para>
        /// </summary>
        public const string DEFAULT_IP_ADDRESS = "127.0.0.1";
        /// <summary>
        /// <para>zh-cn:Docker环境变量</para>
        /// <para>en-us:Docker environment variables</para>
        /// </summary>
        public const string DOCKER_ENVIRONMENT_VARIABLE = "DOTNET_RUNNING_IN_CONTAINER";
        /// <summary>
        /// <para>zh-cn:测试环境关键字</para>
        /// <para>en-us:Test environment keyword</para>
        /// </summary>
        public const string ENVIRONMENT_TEST_KEY = "test";
        /// <summary>
        /// <para>zh-cn:程序启动地址</para>
        /// <para>en-us:Program startup address</para>
        /// </summary>
        public static string ApplicationPath = AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
        /// <summary>
        /// <para>zh-cn:程序插件地址</para>
        /// <para>en-us:Program plugin address</para>
        /// </summary>
        public static string AppPluginsPath = string.Format("{0}/plugins", ApplicationPath);
        /// <summary>
        /// <para>zh-cn:程序名称</para>
        /// <para>en-us:Program name</para>
        /// </summary>
        public static string ApplicationName = string.Empty;

        /// <summary>
        /// <para>zh-cn:程序实例名称</para>
        /// <para>en-us:Application instance name</para>
        /// </summary>
        /// <remarks>
        ///  <para>zh-cn: 由ApplicationName和PID拼接而成</para>
        ///  <para>en-us: Concatenated by ApplicationName and PID</para>
        /// </remarks>
        public static string ApplicationInstanceName = string.Empty;
        /// <summary>
        /// <para>zh-cn:当前程序运行信息</para>
        /// <para>en-us:Current program running information</para>
        /// </summary>
        public static IContainerInstance RunningHostInfo = null;
        /// <summary>
        /// <para>zh-cn:当前程序运行环境</para>
        /// <para>en-us:Current program running environment</para>
        /// </summary>
        public static EnvironmentContainersEnum? EnvironmentContainers = default;
        /// <summary>
        /// <para>zh-cn:当前应用程序启动模式</para> 
        /// <para>en-us:Current application startup mode</para>
        /// </summary>
        public static EnvironmentEnums? EnvironmentStatus = default;
        /// <summary>
        /// <para>zh-cn:当前应用程序是否为调试模式</para>
        /// <para>en-us:Whether the current application is in debug mode</para>
        /// </summary>
        public static bool IsDebugger = false;
        /// <summary>
        /// <para>zh-cn:当前应用程序配置文件加载方式</para>
        /// <para>en-us:Current application configuration file loading method</para>
        /// </summary>
        public static LoadConfigurationTypeEnum LoadConfigurationTypeEnum = LoadConfigurationTypeEnum.File;



        /// <summary>
        ///  <para>zh-cn:应用程序启动绑定路径信息</para>
        ///  <para>en-us:Application startup binding path information</para>
        /// </summary>
        /// <returns>
        ///  <para>zh-cn:路径信息,多个路径之间用</para>
        /// </returns>
        public static string GetApplicationUrls()
        {
            string[] Commands = AppEnvironment.CommandLines;

            string ASPNETCORE_URLS = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");

            string Urls = ASPNETCORE_URLS ?? Commands.FirstOrDefault(m => m.StartsWith("--urls"))?.Split('=')[1] ?? string.Empty;

            return Urls;
        }


        #region  V1.2.6 新增功能

        /// <summary>
        /// <para>zh-cn:是否启用集中式身份认证</para>
        /// <para>en-us:Whether to enable centralized identity authentication</para>
        /// </summary>
        public const string ENABLE_Concentration_Authentication_SERVICE = "EnableConcentrationAuthentication";






        #endregion
    }
}
