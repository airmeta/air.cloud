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
using Air.Cloud.Core.Modules.AppAssembly.Builder;
using Air.Cloud.Core.Modules.DynamicApp.Enums;
using Air.Cloud.Core.Modules.DynamicApp.Model;
using Air.Cloud.Core.Plugins.Banner;
using Air.Cloud.Core.Plugins.Security.MD5;
using Air.Cloud.Core.Standard.DynamicServer;

using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.Configuration;

using System.Reflection;
using System.Runtime.Loader;

namespace Air.Cloud.Core.Modules.DynamicApp
{
    /// <summary>
    /// <para>zh-cn:动态应用存储标准接口实现</para>                                         
    /// <para>en-us: Dynamic Application Loading Standard Interface Implementation</para>
    /// </summary>
    public class DynamicAppStoreDependency : IDynamicAppStoreStandard,ISingleton
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
                DllDirectoryPath = IDynamicAppStoreStandard.LIB_DIRECTORY;
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
        public IConfiguration TryLoadConfiguration(DynamicAppType dynamicAppType)
        {
            switch (dynamicAppType)
            {
                case DynamicAppType.Mod:
                   
                    break;
                case DynamicAppType.Plugin:
                    break;
            }
            return null;
        }
        /// <inheritdoc/>
        public IList<AssemblyName> TryLoadApplication(DynamicAppType dynamicAppType)
        {
            switch (dynamicAppType)
            {
                case DynamicAppType.Mod:
                    if (IDynamicAppStoreStandard.DynamicModules == null)
                    {
                        var paths = TryLoadDll(MODS_DIRECTORY);
                        AppRealization.AppPlugin.GetPlugin<IAppBannerPlugin>().PrintSystemModuleInformation(paths);
                        IList<AssemblyName> assemblyNames = new List<AssemblyName>();
                        foreach (var item in paths)
                        {
                            AssemblyName assembly = AppAssemblyBuilderFactory.Create(item).MainAssemblyName;
                            assemblyNames.Add(assembly);
                        }
                        IDynamicAppStoreStandard.DynamicModules = assemblyNames;
                    }
                    return IDynamicAppStoreStandard.DynamicModules;
                case DynamicAppType.Plugin:
                    if (IDynamicAppStoreStandard.DynamicPlugins == null)
                    {
                        var paths = TryLoadDll(PLUGINS_DIRECTORY);
                        AppRealization.AppPlugin.GetPlugin<IAppBannerPlugin>().PrintSystemPluginInformation(paths);
                        IList<AssemblyName> assemblyNames = new List<AssemblyName>();
                        foreach (var item in paths)
                        {
                            AssemblyName assembly = AppAssemblyBuilderFactory.Create(item).MainAssemblyName;
                            assemblyNames.Add(assembly);
                        }
                        IDynamicAppStoreStandard.DynamicPlugins = assemblyNames;
                    }
                    return IDynamicAppStoreStandard.DynamicPlugins;
            }
            return new List<AssemblyName>();
        }

        /// <inheritdoc/>
        public IList<DynamicAppInformation> TryLoadDynamicApps(DynamicAppUsage pluginUsageEnum)
        {
             return IDynamicAppStoreStandard.DynamicAppScanningResult.Where(d => d.Usage == pluginUsageEnum).DistinctBy(s=>s.Type.FullName).ToList();   
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
        /// <inheritdoc/>
        public ApplicationPartManager InjectDynamicAppPartManager()
        {
            var ExternalModuleAssemblies= AppCore.AppExternal.ExternalModuleAssemblies;
            // 载入模块化部件
            if (ExternalModuleAssemblies != null && ExternalModuleAssemblies.Any())
            {
                var Services = AppCore.AppExternal.ExternalModuleCrucialTypes.Where(s => s.GetInterfaces().Contains(typeof(IDynamicService))).ToList();

                IDictionary<string, Assembly> assemblies = new Dictionary<string, Assembly>();

                foreach (var item in Services)
                {
                    var AssemblyKey = MD5Encryption.GetMd5By32($"{item.Assembly.FullName} {item.Assembly.Location}");

                    assemblies.TryAdd(AssemblyKey, item.Assembly);
                }
                foreach (var assembly in assemblies.Values)
                {
                    if (AppCore.ApplicationPart.ApplicationParts.Any(u => u.Name != assembly.GetName().Name))
                    {
                        AppCore.ApplicationPart.ApplicationParts.Add(new AssemblyPart(assembly));
                    }
                }
            }
            return AppCore.ApplicationPart;
        }
        /// <inheritdoc/>
        public ApplicationPartManager AddDynamicAppPart(AssemblyName assemblyName)
        {
            if (AppCore.ApplicationPart.ApplicationParts.Any(u => u.Name != assemblyName.Name))
            {
                AppCore.ApplicationPart.ApplicationParts.Add(new AssemblyPart(Assembly.Load(assemblyName)));
            }
            return AppCore.ApplicationPart;
        }
        /// <inheritdoc/>
        public ApplicationPartManager RemoveDynamicAppPart(AssemblyName assemblyName)
        {
            if (AppCore.ApplicationPart.ApplicationParts.Any(u => u.Name == assemblyName.Name))
            {
                AppCore.ApplicationPart.ApplicationParts.Remove(new AssemblyPart(Assembly.Load(assemblyName)));
            }
            return AppCore.ApplicationPart;
        }
    }

}
