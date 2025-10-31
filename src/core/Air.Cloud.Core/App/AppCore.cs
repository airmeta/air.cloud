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
using Air.Cloud.Core.App.Startups;
using Air.Cloud.Core.Enhances;
using Air.Cloud.Core.Enums;
using Air.Cloud.Core.Modules;
using Air.Cloud.Core.Modules.AppAssembly;
using Air.Cloud.Core.Plugins;
using Air.Cloud.Core.Standard;
using Air.Cloud.Core.Standard.DataBase.Model;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using System.Collections.Concurrent;
using System.Reflection;

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
            Assemblies = AppAssemblyLoader.GetAssemblies();
            CrucialAssemblies = Assemblies.Where(s => s.Name != null &&
                         (s.Name.Contains(ASSEMBLY_MODULES_KEY)
                             || s.Name.Contains(ASSEMBLY_PLUGINS_KEY)
                             || s.Name.Contains(ASSEMBLY_ENHANCE_KEY)
                             || s.Name == CoreAssemblyName.Name)).ToList();
            Modules = Assemblies.Where(s => s.FullName.Contains(ASSEMBLY_MODULES_KEY)).ToList();
            Plugins = Assemblies.Where(s => s.FullName.Contains(ASSEMBLY_PLUGINS_KEY)).ToList();
            Enhances = Assemblies.Where(s => s.FullName.Contains(ASSEMBLY_ENHANCE_KEY)).ToList();
            //加载所有的关键类型
            CrucialTypes = Assemblies.SelectMany(AppAssemblyLoader.GetTypes);
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
        /// <para>zh-cn:应用程序部件管理器</para>
        /// <para>en-us:Application Part Manager</para>
        /// </summary>
        public static ApplicationPartManager ApplicationPart;

 
        public static class AppExternal
        {
            /// <summary>
            /// 模组程序集 包含了功能服务 在启动时会被装配到程序中 可能会提供接口服务 API服务等各类型系统内支持的服务
            /// </summary>
            public static IEnumerable<AssemblyName> ExternalModuleAssemblies => AppRealization.DynamicAppStore.TryLoadApplication(Core.Modules.DynamicApp.Enums.DynamicAppType.Mod);

            /// <summary>
            /// 插件程序集 在全局API中生效的插件扩展,是对API请求的功能性扩展,不会影响到API本身的功能
            /// </summary>
            public static IEnumerable<AssemblyName> ExternalPluginAssemblies => AppRealization.DynamicAppStore.TryLoadApplication(Core.Modules.DynamicApp.Enums.DynamicAppType.Plugin);

            /// <summary>
            /// <para>zh-cn:外部模组关键类</para>
            /// <para>en-us:External module crucial types</para>
            /// </summary>
            public static IEnumerable<Type> ExternalModuleCrucialTypes = new List<Type>();

            /// <summary>
            /// <para>zh-cn:外部插件关键类</para>
            /// <para>en-us:External plugin crucial types</para>
            /// </summary>
            public static IEnumerable<Type> ExternalPluginCrucialTypes = new List<Type>();



        }
    }
}
