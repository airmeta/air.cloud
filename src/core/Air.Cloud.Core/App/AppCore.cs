﻿/*
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
using Air.Cloud.Core.App.Startups;
using Air.Cloud.Core.Enhance;
using Air.Cloud.Core.Enums;
using Air.Cloud.Core.Modules;
using Air.Cloud.Core.Plugins;
using Air.Cloud.Core.Plugins.Reflection;
using Air.Cloud.Core.Standard;
using Air.Cloud.Core.Standard.DataBase.Model;
using Air.Cloud.Core.Standard.DynamicServer;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.Options;

using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.Loader;
using System.Security.Claims;

namespace Air.Cloud.Core.App
{
    /// <summary>
    /// <para>zh-cn:应用程序核心</para>
    /// <para>en-us:Application core</para>
    /// </summary>
    [IgnoreScanning]
    public static partial class AppCore
    {
        /// <summary>
        /// <para>zh-cn:模块关键词</para>
        /// <para>en-us:Module keyword</para>
        /// </summary>
        public const string ASSEMBLY_MODULES_KEY = "Modules";
        /// <summary>
        /// <para>zh-cn:插件关键词</para>
        /// <para>en-us:Plugin keyword</para>
        /// </summary>
        public const string ASSEMBLY_PLUGINS_KEY = "Plugins";
        /// <summary>
        /// <para>zh-cn:增强关键词</para>
        /// <para>en-us:Enhance keyword</para>
        /// </summary>
        public const string ASSEMBLY_ENHANCE_KEY = "Enhances";
        /// <summary>
        /// <para>zh-cn:核心程序集名称</para>
        /// <para>en-us:Core assembly name</para>
        /// </summary>
        static readonly AssemblyName CoreAssemblyName = typeof(AppCore).Assembly.GetName();
        static AppCore()
        {
            // 加载程序集
            Assemblies = AssemblyLoader.GetAssemblies();
            CrucialAssemblies = Assemblies.Where(s => s.Name != null &&
                         (s.Name.Contains(ASSEMBLY_MODULES_KEY)
                             || s.Name.Contains(ASSEMBLY_PLUGINS_KEY)
                             || s.Name.Contains(ASSEMBLY_ENHANCE_KEY)
                             || s.Name == CoreAssemblyName.Name)).ToList();
            Modules = Assemblies.Where(s => s.FullName.Contains(ASSEMBLY_MODULES_KEY)).ToList();
            Plugins = Assemblies.Where(s => s.FullName.Contains(ASSEMBLY_PLUGINS_KEY)).ToList();
            Enhances = Assemblies.Where(s => s.FullName.Contains(ASSEMBLY_ENHANCE_KEY)).ToList();
            //加载所有的关键类型
            CrucialTypes = Assemblies.SelectMany(AssemblyLoader.GetTypes);
            // 未托管的对象
            UnmanagedObjects = new ConcurrentBag<IDisposable>();
            AppStartups = new ConcurrentBag<AppStartup>();
        }
        #region  Types
        /// <summary>
        /// 关键类型
        /// </summary>
        public static IEnumerable<Type> CrucialTypes;
        /// <summary>
        /// 有效程序集类型
        /// </summary>
        public static IEnumerable<Type> EffectiveTypes=> CrucialTypes.Where(s => !(
                                                                s.GetInterfaces().Contains(typeof(IModule))
                                                            || s.GetInterfaces().Contains(typeof(IPlugin))
                                                            || s.GetInterfaces().Contains(typeof(IEnhance))
                                                            || s.BaseType == typeof(AppStartup)
                                                            || s.GetInterfaces().Contains(typeof(IStandard))
                                                            || s.GetInterfaces().Contains(typeof(IPrivateEntity))
                                                            )).ToList();
        /// <summary>
        /// 所有的模组
        /// </summary>
        public static IEnumerable<Type> ModuleTypes => CrucialTypes.Where(s => s.GetInterfaces().Contains(typeof(IModule))).ToList();

        /// <summary>
        /// 所有的插件
        /// </summary>
        public static IEnumerable<Type> PluginTypes => CrucialTypes.Where(s => s.GetInterfaces().Contains(typeof(IPlugin))).ToList();

        /// <summary>
        /// 所有的增强实现
        /// </summary>
        public static IEnumerable<Type> EnhanceTypes => CrucialTypes.Where(s => s.GetInterfaces().Contains(typeof(IEnhance))).ToList();

        /// <summary>
        /// 所有的项目启动项
        /// </summary>
        public static IEnumerable<Type> StartTypes => CrucialTypes.Where(s => s.BaseType==typeof(AppStartup)).ToList();

        /// <summary>
        /// 所有的约定类型
        /// </summary>
        public static IEnumerable<Type> StandardTypes => CrucialTypes.Where(s => s.GetInterfaces().Contains(typeof(IStandard))).ToList();
        /// <summary>
        /// 数据库实体类引用
        /// </summary>
        /// <remarks>
        /// 在没有使用数据库模块的情况下 该属性为空集合
        /// </remarks>
        public static IEnumerable<Type> EntityTypes=>CrucialTypes.Where(t => t.GetInterfaces().Contains(typeof(IPrivateEntity))&& t.IsClass && !t.IsAbstract && !t.IsGenericType && !t.IsInterface).ToList();
        #endregion
        #region Assemblies
        /// <summary>
        /// 所有的模组
        /// </summary>
        public static IEnumerable<AssemblyName> Modules;
        /// <summary>
        /// 所有的插件
        /// </summary>
        public static IEnumerable<AssemblyName> Plugins;
        /// <summary>
        /// 所有的增强实现
        /// </summary>
        public static IEnumerable<AssemblyName> Enhances;
        /// <summary>
        /// 关键类库 核心库,模组,插件,增强实现
        /// </summary>
        public static IEnumerable<AssemblyName> CrucialAssemblies;
        /// <summary>
        /// 所有应用程序集信息
        /// </summary>
        public static IEnumerable<AssemblyName> Assemblies;
        #endregion
        #region  Config
        /// <summary>
        /// 应用程序配置信息
        /// </summary>
        public static IConfiguration Configuration => AppConfigurationLoader.Configurations;

        /// <summary>
        /// 私有设置，避免重复解析
        /// </summary>
        internal static AppSettingsOptions InternalSettings;

        /// <summary>
        /// <para>zh-cn:应用全局配置</para>
        /// <para>en-us:Application global settings</para>
        /// </summary> 
        /// <remarks>
        /// <para>zh-cn:如果在依赖注入的方法ConfigureServices中使用该配置时需要确认该配置是否注入成功</para>
        /// <para>en-us:If you use this configuration in the dependency injection method ConfigureServices, you need to confirm whether the configuration is successfully injected.</para>
        /// </remarks>
        public static AppSettingsOptions Settings => InternalSettings ??= AppConfiguration.GetConfig<AppSettingsOptions>("AppSettings", true);

        #endregion

        #region AssemblyLoader
        /// <summary>
        /// 程序集加载器
        /// </summary>
        internal static class AssemblyLoader
        {
            /// <summary>
            /// 获取应用有效程序集
            /// </summary>
            /// <returns>IEnumerable</returns>
            internal static IEnumerable<AssemblyName> GetAssemblies()
            {
                // 排除数据库迁移程序集 这里可以换成内存动态生成一个Database.Migrations程序集
                var ExcludeAssemblyNames = new string[] { "Database.Migrations" };
                IEnumerable<AssemblyName> scanAssemblies  = DependencyContext.Default.RuntimeLibraries
                 .Where(u =>
                        (u.Type == "project" && !ExcludeAssemblyNames.Any(j => u.Name.EndsWith(j))) ||
                        (u.Type == "package" && (u.Name.StartsWith(nameof(Air)) || Settings.SupportPackageNamePrefixs.Any(p => u.Name.StartsWith(p)))) ||
                        (Settings.EnabledReferenceAssemblyScan == true && u.Type == "reference"))    // 判断是否启用引用程序集扫描
                 .Select(u => new AssemblyName(u.Name));
                IEnumerable<Assembly> externalAssemblies = new List<Assembly>();

                #region  加载插件程序集
                //插件检查-是否具有插件 如果有则加载插件
                if (Directory.Exists(AppConst.AppPluginsPath))
                {
                    var PluginFiles = Directory.GetFiles(AppConst.AppPluginsPath, "*.dll", SearchOption.AllDirectories);
                    var PluginAssemlies = new List<AssemblyName>();
                    foreach (var PluginPath in PluginFiles)
                    {
                        var loadedAssembly = Reflect.LoadAssembly(PluginPath);
                        if (loadedAssembly == default)
                        {
                            Console.WriteLine($"加载插件失败:{Path.GetFileName(PluginPath)}");
                            continue;
                        }
                        PluginAssemlies.Add(loadedAssembly.GetName());
                    }
                    scanAssemblies.Concat(PluginAssemlies);
                }
                #endregion
                return scanAssemblies;
            }

            /// <summary>
            /// 加载程序集中的所有类型
            /// </summary>
            /// <param name="ass">程序集名称</param>
            /// <returns></returns>
            internal static IEnumerable<Type> GetTypes(AssemblyName ass)
            {
                IEnumerable<Type> types = new List<Type>();
                try
                {
                    types = AssemblyLoadContext.Default.LoadFromAssemblyName(ass).GetTypes().Where(t =>
                    {
                        var instances = t.GetInterfaces();
                        if ((instances.Contains(typeof(IPlugin))
                        || instances.Contains(typeof(IModule))
                        || instances.Contains(typeof(IEnhance))
                        || instances.Contains(typeof(IStandard))
                        || instances.Contains(typeof(IDynamicService))
                        || instances.Contains(typeof(IPrivateEntity))
                        ) && t.IsPublic&& !t.IsDefined(typeof(IgnoreScanningAttribute), false))
                            return true;
                        if (t.IsDefined(typeof(NeedScanningAttribute)))
                            return true;
                        if (t.BaseType == typeof(AppStartup))
                            return true;
                        return false;
                    }).ToList();
                }
                catch
                {
                    Console.WriteLine($"构建类库分析器时失败,失败类库:[{ass.FullName}]");
                }
                return types;
            }
        }

        #endregion


        /// <summary>
        /// 存储根服务，可能为空
        /// </summary>
        public static IServiceProvider RootServices;
        /// <summary>
        /// 内部服务集合
        /// </summary>
        public static IServiceCollection InternalServices;
        /// <summary>
        /// 获取请求上下文
        /// </summary>
        public static HttpContext HttpContext => RootServices?.GetService<IHttpContextAccessor>()?.HttpContext;

        /// <summary>
        /// 未托管的对象集合
        /// </summary>
        public static readonly ConcurrentBag<IDisposable> UnmanagedObjects;

        /// <summary>
        /// 应用程序启动类型:Host/Web
        /// </summary>
        public static AppStartupTypeEnum AppStartType { get; set; }

        /// <summary>
        /// 应用所有启动类
        /// </summary>
        public static ConcurrentBag<AppStartup> AppStartups;

        /// <summary>
        /// 外部程序集
        /// </summary>
        public static IEnumerable<Assembly> ExternalAssemblies;


        /// <summary>
        /// <para>zh-cn:获取服务提供器</para>
        /// <para>en-us:GetAsync service provider</para>
        /// </summary>
        /// <param name="serviceType">
        /// <para>zh-cn:服务类型</para>
        /// <para>en-us:Service type</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:返回服务提供器</para>
        /// <para>en-us:Return service provider</para>
        /// </returns>
        public static IServiceProvider GetServiceProvider(Type serviceType)
        {
            // 处理控制台应用程序
            if (AppStartType == AppStartupTypeEnum.WEB && RootServices != null) return RootServices;

            // 第一选择，判断是否是单例注册且单例服务不为空，如果是直接返回根服务提供器
            if (RootServices != null && InternalServices.Where(u => u.ServiceType == (serviceType.IsGenericType ? serviceType.GetGenericTypeDefinition() : serviceType))
                                                                    .Any(u => u.Lifetime == ServiceLifetime.Singleton)) return RootServices;

            // 第二选择是获取 HttpContext 对象的 RequestServices
            var httpContext = HttpContext;
            if (httpContext?.RequestServices != null) return httpContext.RequestServices;
            // 第三选择，创建新的作用域并返回服务提供器
            else if (RootServices != null)
            {
                var scoped = RootServices.CreateScope();
                UnmanagedObjects.Add(scoped);
                return scoped.ServiceProvider;
            }
            // 第四选择，构建新的服务对象（性能最差）
            else
            {
                var serviceProvider = InternalServices.BuildServiceProvider();
                UnmanagedObjects.Add(serviceProvider);
                return serviceProvider;
            }
        }

        /// <summary>
        /// <para>zh-cn:获取请求生存周期的服务</para>
        /// <para>en-us:GetAsync request survival cycle service</para>
        /// </summary>
        /// <typeparam name="TService">
        /// <para>zh-cn:服务类型</para>
        /// <para>en-us:Service type</para>
        /// </typeparam>
        /// <param name="serviceProvider">
        /// <para>zh-cn:服务提供器</para>
        /// <para>en-us:Service provider</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:返回服务</para>
        /// <para>en-us:Return service</para>
        /// </returns>
        public static TService GetService<TService>(IServiceProvider serviceProvider = default)
            where TService : class
        {
            return GetService(typeof(TService), serviceProvider) as TService;
        }

        /// <summary>
        /// <para>zh-cn:获取请求生存周期的服务</para>
        /// <para>en-us:GetAsync request survival cycle service</para>
        /// </summary>
        /// <param name="serviceProvider">
        /// <para>zh-cn:服务提供器</para>
        /// <para>en-us:Service provider</para>
        /// </param>
        /// <param name="type">
        /// <para>zh-cn:服务类型</para>
        /// <para>en-us:Service type</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:返回服务</para>
        /// <para>en-us:Return service</para>
        /// </returns>
        public static object GetService(Type type, IServiceProvider serviceProvider = default)
        {
            return (serviceProvider ?? GetServiceProvider(type)).GetService(type);
        }

        /// <summary>
        /// 获取请求生存周期的服务
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <param name="serviceProvider"></param>
        /// <returns></returns>
        public static TService GetRequiredService<TService>(IServiceProvider serviceProvider = default)
            where TService : class
        {
            return GetRequiredService(typeof(TService), serviceProvider) as TService;
        }

        /// <summary>
        /// 获取请求生存周期的服务
        /// </summary>
        /// <param name="type"></param>
        /// <param name="serviceProvider"></param>
        /// <returns></returns>
        public static object GetRequiredService(Type type, IServiceProvider serviceProvider = default)
        {
            return (serviceProvider ?? GetServiceProvider(type)).GetRequiredService(type);
        }


        /// <summary>
        /// 获取选项
        /// </summary>
        /// <typeparam name="TOptions">强类型选项类</typeparam>
        /// <param name="serviceProvider"></param>
        /// <returns>TOptions</returns>
        public static TOptions GetOptions<TOptions>(IServiceProvider serviceProvider = default)
            where TOptions : class, new()
        {
            return GetService<IOptions<TOptions>>(serviceProvider ?? RootServices)?.Value;
        }

        /// <summary>
        /// 获取选项
        /// </summary>
        /// <typeparam name="TOptions">强类型选项类</typeparam>
        /// <param name="serviceProvider"></param>
        /// <returns>TOptions</returns>
        public static TOptions GetOptionsMonitor<TOptions>(IServiceProvider serviceProvider = default)
            where TOptions : class, new()
        {
            return GetService<IOptionsMonitor<TOptions>>(serviceProvider ?? RootServices)?.CurrentValue;
        }

        /// <summary>
        /// 获取选项
        /// </summary>
        /// <typeparam name="TOptions">强类型选项类</typeparam>
        /// <param name="serviceProvider"></param>
        /// <returns>TOptions</returns>
        public static TOptions GetOptionsSnapshot<TOptions>(IServiceProvider serviceProvider = default)
            where TOptions : class, new()
        {
            // 这里不能从根服务解析，因为是 Scoped 作用域
            return GetService<IOptionsSnapshot<TOptions>>(serviceProvider)?.Value;
        }
       
        /// <summary>
        /// 释放所有未托管的对象
        /// </summary>
        public static void DisposeUnmanagedObjects()
        {
            UnmanagedObjects.Clear();
        }
    }
}
