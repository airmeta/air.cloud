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
using Air.Cloud.Core.Modules.DynamicApp.Enums;
using Air.Cloud.Core.Modules.DynamicApp.Model;
using Air.Cloud.Core.Standard;

using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.Configuration;

using System.Reflection;
using System.Runtime.Loader;

namespace Air.Cloud.Core.Modules.DynamicApp
{
    /// <summary>
    /// <para>zh-cn:动态应用存储静态类</para>
    /// <para>en-us: Dynamic Application Storage Static Class</para>
    /// </summary>
    public interface IDynamicAppStoreStandard:IStandard
    { 
        /// <summary>
        /// <para>zh-cn:模组的dll所在目录</para>
        /// <para>en-us: Directory where the dll of the module or plugin is located</para>
        /// </summary>
        public const string LIB_DIRECTORY = "lib";

        /// <summary>
        /// <para>zh-cn:模组配置文件名称</para>
        /// <para>en-us: Module configuration file name</para>
        /// </summary>
        public const string MOD_CONFIG_FILE_NAME = "ModInfo.xml";

        /// <summary>
        /// <para>zh-cn:程序集文件扩展名</para>
        /// <para>en-us: Assembly file extension</para>
        /// </summary>
        public const string ASSEMBLY_EXTENSIONS = "dll";
        /// <summary>
        /// <para>zh-cn:已加载的动态应用信息集合</para>
        /// <para>en-us: Collection of loaded dynamic application information</para>
        /// </summary>
        public static IList<DynamicAppInformation> DynamicAppScanningResult = new List<DynamicAppInformation>();

        /// <summary>
        /// <para>zh-cn:已加载的模组程序集集合</para>
        /// <para>en-us: Collection of loaded module assemblies</para>
        /// </summary>
        public static IList<AssemblyName> DynamicModules = null;

        /// <summary>
        /// <para>zh-cn:已加载的插件程序集集合</para>
        /// <para>en-us: Collection of loaded plugin assemblies</para>
        /// </summary>
        public static IList<AssemblyName> DynamicPlugins = null;

        /// <summary>
        /// <para>zh-cn:尝试加载动态应用程序的配置文件</para>
        /// <para>en-us: Try to load the configuration file of the dynamic application</para>
        /// </summary>'
        /// <remarks>
        ///  <para>zh-cn:加载到的配置文件 将会装载到configuration中</para>
        ///  <para>en-us: The loaded configuration file will be loaded into the configuration</para>
        /// </remarks>
        /// <returns></returns>
        public IConfiguration TryLoadConfiguration(DynamicAppType dynamicAppType);


        /// <summary>
        /// <para>zh-cn:尝试加载动态应用</para>
        /// <para>en-us: Try to load dynamic applications</para>
        /// </summary>
        /// <param name="dynamicAppType">
        ///  <para>zh-cn:动态应用类型枚举</para>
        ///  <para>en-us: Dynamic Application Type Enumeration</para>
        /// </param>
        /// <returns></returns>
        public IList<AssemblyName> TryLoadApplication(DynamicAppType dynamicAppType);


        /// <summary>
        ///  <para>zh-cn:尝试加载动态应用</para>
        ///  <para>en-us: Try to load dynamic applications</para>
        /// </summary>
        /// <param name="pluginUsageEnum">
        ///  <para>zh-cn:动态应用使用场景枚举</para>
        ///  <para>en-us: Dynamic Application Usage Scenario Enumeration</para>
        /// </param>
        /// <returns></returns>
        public IList<DynamicAppInformation> TryLoadDynamicApps(DynamicAppUsage pluginUsageEnum);

        /// <summary>
        /// <para>zh-cn:尝试获取已加载的模组或插件程序集上下文</para>
        /// <para>en-us: Try to get the loaded module or plugin assembly</para>
        /// </summary>
        /// <param name="Name">
        ///  <para>zh-cn:程序集名称</para>
        ///  <para>en-us: Assembly name</para>
        /// </param>
        /// <returns>
        ///  <para>zh-cn:返回已加载的程序集上下文</para>
        ///  <para>en-us: Returns the loaded assembly context</para>
        /// </returns>
        public AssemblyLoadContext GetAssemblyLoadContext(AssemblyName Name);

        /// <summary>
        /// <para>zh-cn:尝试获取已加载的模组或插件主程序集</para>
        /// <para>en-us: Try to get the loaded module or plugin assembly</para>
        /// </summary>
        /// <param name="Name">
        ///  <para>zh-cn:程序集名称</para>
        ///  <para>en-us: Assembly name</para>
        /// </param>
        /// <returns>
        ///  <para>zh-cn:返回已加载的程序集</para>
        ///  <para>en-us: Returns the loaded assembly</para>
        /// </returns>
        /// 
        public Assembly GetLoadedMainAssembly(AssemblyName Name);

        /// <summary>
        /// <para>zh-cn:注入动态应用程序集到全局应用程序部件管理器</para>
        /// <para>en-us: Inject dynamic application assemblies into the global application part manager</para>
        /// </summary>
        /// <returns>
        /// <para>zh-cn:返回注入后的应用程序部件管理器</para>
        /// <para>en-us: Returns the injected application part manager</para>
        /// </returns>
        public ApplicationPartManager InjectDynamicAppPartManager();


        /// <summary>
        /// <para>zh-cn:添加动态应用程序集到全局应用程序部件管理器</para>
        /// <para>en-us: Add dynamic application assemblies to the global application part manager</para>
        /// </summary>
        /// <param name="assemblyName">
        ///  <para>zh-cn:程序集名称</para>
        ///  <para>en-us: Assembly name</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:返回添加后的应用程序部件管理器</para>
        /// <para>en-us: Returns the added application part manager</para>
        /// </returns>
        public ApplicationPartManager AddDynamicAppPart(AssemblyName assemblyName);


        /// <summary>
        /// <para>zh-cn:移除动态应用程序集从全局应用程序部件管理器</para>
        /// <para>en-us: Remove dynamic application assemblies from the global application part manager</para>
        /// </summary>
        /// <param name="Name">
        ///  <para>zh-cn:程序集名称</para>
        ///  <para>en-us: Assembly name</para>
        /// </param>
        /// <returns>
        ///  <para>zh-cn:返回移除后的应用程序部件管理器</para>
        ///  <para>en-us: Returns the removed application part manager</para>
        /// </returns>
        public ApplicationPartManager RemoveDynamicAppPart(AssemblyName Name);
    }
}
