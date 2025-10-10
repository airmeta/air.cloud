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
using Air.Cloud.Core.Modules.AppAssembly.Builder;
using Air.Cloud.Core.Modules.DynamicApp.Enums;
using Air.Cloud.Core.Modules.DynamicApp.Model;
using Air.Cloud.Core.Standard;

using System.Reflection;
using System.Runtime.Loader;

namespace Air.Cloud.Core.Modules.DynamicApp
{

    /// <summary>
    /// <para>zh-cn:动态应用加载标准接口</para>
    /// <para>en-us: Dynamic Application Loading Standard Interface</para>
    /// </summary>
    public interface IDynamicAppLoaderStandard:IStandard
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
        public static IList<DynamicAppInformation> DynamicAppScanningResult=new List<DynamicAppInformation>();

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
        /// <para>zh-cn:尝试加载所有模组</para>
        /// <para>en-us: Try to load all modules</para>
        /// </summary>
        /// <returns>
        ///  <para>zh-cn:返回加载的模组程序集名称集合</para>
        ///  <para>en-us: Returns a collection of loaded module assembly names</para>
        /// </returns>
        public IList<AssemblyName> TryLoadModules();
        /// <summary>
        /// <para>zh-cn:尝试加载所有插件</para>
        /// <para>en-us: Try to load all plugins</para>
        /// </summary>
        /// <returns>
        ///  <para>zh-cn:返回加载的插件程序集名称集合</para>
        ///  <para>en-us: Returns a collection of loaded plugin assembly names</para>
        /// </returns>
        public IList<AssemblyName> TryLoadPlugins();

        /// <summary>
        ///  <para>zh-cn:尝试加载动态应用</para>
        ///  <para>en-us: Try to load dynamic applications</para>
        /// </summary>
        /// <param name="pluginUsageEnum">
        ///  <para>zh-cn:动态应用使用场景枚举</para>
        ///  <para>en-us: Dynamic Application Usage Scenario Enumeration</para>
        /// </param>
        /// <returns></returns>
        public IList<DynamicAppInformation> TryLoadDynamicApps(DynamicAppUsageEnum pluginUsageEnum);

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

    }
}
