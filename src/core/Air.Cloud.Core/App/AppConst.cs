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
using Air.Cloud.Core.Enums;
using Air.Cloud.Core.Standard.Container.Model;

namespace Air.Cloud.Core.App
{
    public static class AppConst
    {
        /// <summary>
        /// 启动时加载的主配置文件
        /// </summary>
        public const string DEFAULT_CONFIG_FILE = "appsettings.json";
        /// <summary>
        /// 实际加载的配置文件名
        /// </summary>
        public const string ENVIRONMENT_CONFIG_FILE = "appsettings.{0}.json";
        /// <summary>
        /// 环境标识
        /// </summary>
        public const string ENVIRONMENT = "Environment";
        /// <summary>
        /// 当前配置的环境(如果为空就应用程序自己启动时分析)
        /// </summary>
        public static string Current_Config_Environment = string.Empty;
        /// <summary>
        /// 当前应用程序启动配置文件全名称
        /// </summary>
        public static string SystemEnvironmentConfigFileFullName => string.Format(ENVIRONMENT_CONFIG_FILE, AppEnvironment.VirtualEnvironment);
        /// <summary>
        /// 当前应用程序启动配置文件全名称
        /// </summary>
        public static string CommonEnvironmentConfigFileFullName => string.Format(ENVIRONMENT_CONFIG_FILE, $"Common.{AppEnvironment.RealEnvironment}");
        /// <summary>
        /// 默认IP地址
        /// </summary>
        public const string DEFAULT_IP_HOST = "127.0.0.1";
        /// <summary>
        /// Docker环境标识
        /// </summary>

        public const string DOCKER_ENVIRONMENT_VARIABLE = "DOTNET_RUNNING_IN_CONTAINER";
        /// <summary>
        /// 测试环境判断标识
        /// </summary>
        public const string ENVIRONMENT_TEST_KEY = "test";
        /// <summary>
        /// 程序启动地址
        /// </summary>
        public static string ApplicationPath = AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
        /// <summary>
        /// 程序插件地址
        /// </summary>
        public static string AppPluginsPath = string.Format("{0}/plugins", ApplicationPath);
        /// <summary>
        /// 程序启动名称
        /// </summary>
        public static string ApplicationName = string.Empty;

        /// <summary>
        /// 当前程序运行信息
        /// </summary>
        public static IContainerRuntimeInformation RunningHostInfo = null;

        /// <summary>
        /// 当前程序运行环境
        /// </summary>
        public static EnvironmentContainersEnum? EnvironmentContainers = default;
        /// <summary>
        /// 当前应用程序启动模式 
        /// </summary>
        public static EnvironmentEnums? EnvironmentStatus = default;
        /// <summary>
        /// 是否Debugger模式
        /// </summary>
        public static bool IsDebugger = false;
        /// <summary>
        /// 当前应用程序配置文件加载方式
        /// </summary>
        /// <remarks>
        /// 文件/配置中心
        /// </remarks>
        public static LoadConfigurationTypeEnum LoadConfigurationTypeEnum = LoadConfigurationTypeEnum.File;
    }
}
