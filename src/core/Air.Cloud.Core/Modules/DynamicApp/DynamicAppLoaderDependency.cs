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
using Air.Cloud.Core.Dependencies;
using Air.Cloud.Core.Extensions.Aspect;
using Air.Cloud.Core.Modules.AppAspect.Attributes;
using Air.Cloud.Core.Modules.AppAssembly.Builder;
using Air.Cloud.Core.Modules.DynamicApp.Attributes;
using Air.Cloud.Core.Modules.DynamicApp.Enums;
using Air.Cloud.Core.Modules.DynamicApp.Model;
using Air.Cloud.Core.Plugins.Banner;

using System.Reflection;
using System.Runtime.Loader;

namespace Air.Cloud.Core.Modules.DynamicApp
{
    /// <summary>
    /// <para>zh-cn:动态应用加载标准接口实现</para>                                         
    /// <para>en-us: Dynamic Application Loading Standard Interface Implementation</para>
    /// </summary>
    public class DynamicAppLoaderDependency : IDynamicAppLoaderStandard,ISingleton
    {
        /// <summary>
        /// <para>zh-cn:模组目录</para>
        /// <para>en-us: Module directory</para>
        /// </summary>
        private const string MODS_DIRECTORY = "Mods";
        /// <summary>
        /// <para>zh-cn:插件目录</para>
        /// <para>en-us: Plugin directory</para>
        /// </summary>
        private const string PLUGINS_DIRECTORY = "Plugins";
        /// <summary>
        /// <para>zh-cn:尝试加载指定目录下的所有插件或模组</para>
        /// <para>en-us: Try to load all plugins or modules in the specified directory</para>
        /// </summary>
        /// <param name="DllDirectory">
        /// <para>zh-cn:插件或模组目录</para>
        /// <para>en-us: Plugin or module directory</para>
        /// </param>
        /// <returns>
        ///  <para>zh-cn:返回加载的插件或模组路径集合</para>
        ///  <para>en-us: Returns a collection of paths to loaded plugins or modules</para>
        /// </returns>
        private IList<string> TryLoadDll(string DllDirectory)
        {
            string DllDirectoryPath = string.Empty;
            if (DllDirectory==MODS_DIRECTORY)
            {
                DllDirectoryPath = IDynamicAppLoaderStandard.LIB_DIRECTORY;
            }
            var BaseDirectory = Path.Combine(AppConst.ApplicationPath, DllDirectory);
            if (Directory.Exists(BaseDirectory))
            {
                //获取所有子目录 并根据目录名字加载目录下的同名dll
                var directories = Directory.GetDirectories(BaseDirectory);
                List<string> paths = new List<string>();
                foreach (var dir in directories)
                {
                    var dirInfo = new DirectoryInfo(dir);
                    var dllPath = Path.Combine(dir, DllDirectoryPath, $"{dirInfo.Name}.dll");
                    if (File.Exists(dllPath))
                    {
                        paths.Add(dllPath);
                    }
                }
                return paths;
            }
            return new List<string>();

        }

        /// <inheritdoc/>
        public IList<AssemblyName> TryLoadModules()
        {
            if (IDynamicAppLoaderStandard.DynamicModules == null)
            {
                var paths = TryLoadDll(MODS_DIRECTORY);
                AppRealization.AppPlugin.GetPlugin<IAppBannerPlugin>().PrintSystemModuleInformation(paths);
                IList<AssemblyName> assemblyNames = new List<AssemblyName>();
                foreach (var item in paths)
                {
                    AssemblyName assembly = AppAssemblyBuilderFactory.Create(item).MainAssemblyName;
                    assemblyNames.Add(assembly);
                }
                IDynamicAppLoaderStandard.DynamicModules = assemblyNames;
            }
            return IDynamicAppLoaderStandard.DynamicModules;
        }

        /// <inheritdoc/>
        public IList<AssemblyName> TryLoadPlugins()
        {
            if (IDynamicAppLoaderStandard.DynamicPlugins==null)
            {
                var paths = TryLoadDll(PLUGINS_DIRECTORY);
                AppRealization.AppPlugin.GetPlugin<IAppBannerPlugin>().PrintSystemPluginInformation(paths);
                IList<AssemblyName> assemblyNames = new List<AssemblyName>();
                foreach (var item in paths)
                {
                    AssemblyName assembly = AppAssemblyBuilderFactory.Create(item).MainAssemblyName;
                    assemblyNames.Add(assembly);
                }
                IDynamicAppLoaderStandard.DynamicPlugins= assemblyNames;
            }
            return IDynamicAppLoaderStandard.DynamicPlugins;
        }
        /// <inheritdoc/>
        public IList<DynamicAppInformation> TryLoadDynamicApps(DynamicAppUsageEnum pluginUsageEnum)
        {
             return IDynamicAppLoaderStandard.DynamicAppScanningResult.Where(d => d.Usage == pluginUsageEnum).DistinctBy(s=>s.Type.FullName).ToList();   
        }
        /// <inheritdoc/>
        public AssemblyLoadContext GetAssemblyLoadContext(AssemblyName Name)
        {
            var AssemblyLoadContext = AppAssemblyBuilderFactory.Get(Name).GetAssemblyLoadContext();
           
            if (AssemblyLoadContext == null)
            {
                throw new Exception($"The assembly named {Name.FullName} is not loaded.");
            }
            return AssemblyLoadContext;

        }
        /// <inheritdoc/>
        public Assembly GetLoadedMainAssembly(AssemblyName Name)
        {
            var Assembly= AppAssemblyBuilderFactory.Get(Name).GetLoadedMainAssembly();

            if (Assembly==null)
            {
                throw new Exception($"The assembly named {Name.FullName} is not loaded.");
            }
            return Assembly;
        }

    }

}
