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
using Air.Cloud.Core.Plugins.DefaultDependency;
using Air.Cloud.Core.Plugins.PID;
using Air.Cloud.Core.Standard;
using Air.Cloud.Core.Standard.Assemblies;
using Air.Cloud.Core.Standard.Cache;
using Air.Cloud.Core.Standard.Cache.Redis;
using Air.Cloud.Core.Standard.Container;
using Air.Cloud.Core.Standard.DefaultDependencies;
using Air.Cloud.Core.Standard.Exceptions;
using Air.Cloud.Core.Standard.JSON;
using Air.Cloud.Core.Standard.Jwt;
using Air.Cloud.Core.Standard.UtilStandard;
using Air.Cloud.Core.Standard.WebApp;

using Microsoft.AspNetCore.Mvc;

using System.Reflection;

namespace Air.Cloud.Core
{
    /// <summary>
    /// 所有标准实现
    /// </summary>
    public static partial class AppRealization
    {
        /// <summary>
        /// 内容打印标准实现
        /// </summary>
        public static IAppPrintStandard Print => InternalRealization.Print ?? DefaultRealization.Print;
        /// <summary>
        /// 容器标准实现
        /// </summary>
        public static IContainerStandard Container => InternalRealization.Container ?? DefaultRealization.Container;
        /// <summary>
        /// 系统注入标准实现
        /// </summary>
        public static IAppInjectStandard Inject => InternalRealization.Inject ?? DefaultRealization.Inject;
        /// <summary>
        /// 全局异常标准实现
        /// </summary>
        public static IAppDomainExceptionHandlerStandard DomainExceptionHandler => InternalRealization.DomainExceptionHandler ?? DefaultRealization.DomainExceptionHandler;
        /// <summary>
        /// 类库扫描标准实现
        /// </summary>
        public static IAssemblyScanningStandard AssemblyScanning => InternalRealization.AssemblyScanning ?? DefaultRealization.AssemblyScanning;
        /// <summary>
        /// JSON Web Token 标准实现
        /// </summary>
        public static IJwtHandlerStandard Jwt => InternalRealization.Jwt ?? DefaultRealization.Jwt;
        /// <summary>
        /// 缓存标准实现
        /// </summary>
        /// <remarks>
        /// 如果无实现,则使用Redis缓存标准实现中的String模块,如果无Redis缓存标准实现,则抛出异常信息
        /// </remarks>
        public static IAppCacheStandard Cache => InternalRealization.Cache ?? DefaultRealization.Cache;
        /// <summary>
        /// Redis缓存标准实现
        /// </summary>
        public static IRedisCacheStandard RedisCache => InternalRealization.RedisCache ?? DefaultRealization.RedisCache;
        /// <summary>
        /// 压缩与解压缩标准实现
        /// </summary>
        public static ICompressStandard Compress => InternalRealization.Compress ?? DefaultRealization.Compress;
        /// <summary>
        /// JSON序列化标准实现
        /// </summary>
        public static IJsonSerializerStandard JSON => InternalRealization.JSON ?? DefaultRealization.JSON;
        /// <summary>
        /// 应用程序PID信息 
        /// </summary>
        /// <remarks>
        ///  与linux 系统的不同的是,这个PID是为了在微服务架构下,多节点的统一注册时,每个实例的名称
        ///  (暂时只在Windows 环境下进行测试)
        /// </remarks>
        public static IPIDPlugin PID => InternalRealization.PID ?? DefaultRealization.PID;

        /// <summary>
        /// 设置约定实现
        /// </summary>
        /// <typeparam name="TDependency">约定类型</typeparam>
        /// <param name="standard">约定实现</param>
        public static void SetDependency<TDependency>(TDependency standard) where TDependency : class
        {
            BindingFlags flag = BindingFlags.Static;
            FieldInfo Field = typeof(InternalRealization).GetFields(flag).FirstOrDefault(s => s.FieldType == standard.GetType());
            Field.SetValue(null, standard);
        }

        /// <summary>
        /// 默认标准实现
        /// </summary>
        protected static class DefaultRealization
        {
            /// <summary>
            /// 压缩与解压缩标准实现
            /// </summary>
            public static ICompressStandard Compress => new DefaultCompressStandardDependency();

            /// <summary>
            /// 内容打印标准实现
            /// </summary>
            public static IAppPrintStandard Print => new DefaultAppPrintDependency();
            /// <summary>
            /// 容器标准实现
            /// </summary>
            public static IContainerStandard Container => null;
            /// <summary>
            /// 系统注入标准实现
            /// </summary>
            public static IAppInjectStandard Inject => new DefaultAppInjectDependency();
            /// <summary>
            /// 全局异常标准实现
            /// </summary>
            public static IAppDomainExceptionHandlerStandard DomainExceptionHandler => new DefaultAppDomainExcepetionHandlerDependency();
            /// <summary>
            /// 类库扫描标准实现
            /// </summary>
            public static IAssemblyScanningStandard AssemblyScanning => new DefaultAssemblyScanningDependency();
            /// <summary>
            /// JSON Web Token 标准实现
            /// </summary>
            public static IJwtHandlerStandard Jwt => new DefaultJwtHandlerDependency();
            /// <summary>
            /// 缓存标准实现
            /// </summary>
            public static IAppCacheStandard Cache => throw new NotImplementedException("未引入任何关于Cache的模组或插件,如果引入,则检查你的代码是否注入该实现");
            /// <summary>
            /// Redis缓存标准实现
            /// </summary>
            public static IRedisCacheStandard RedisCache => throw new NotImplementedException("未引入任何关于Redis的模组或插件,如果引入,则检查你的代码是否注入该实现");
            /// <summary>
            /// JSON序列化标准实现
            /// </summary>
            public static IJsonSerializerStandard JSON => new DefaultJsonSerializerStandardDependency(AppCore.GetOptions<JsonOptions>());

            /// <summary>
            /// 应用程序PID信息 
            /// </summary>
            /// <remarks>
            ///  与linux 系统的不同的是,这个PID是为了在微服务架构下,多节点的统一注册时,每个实例的名称
            ///  (暂时只在Windows 环境下进行测试)
            /// </remarks>
            public static IPIDPlugin PID => new DefaultPIDPluginDependency();

        }
        /// <summary>
        /// 自定义标准实现
        /// </summary>
        protected static class InternalRealization
        {
            /// <summary>
            /// 压缩与解压缩标准实现
            /// </summary>
            public static ICompressStandard Compress => AppCore.GetService<ICompressStandard>();
            /// <summary>
            /// 内容打印标准实现
            /// </summary>
            public static IAppPrintStandard Print = null;
            /// <summary>
            /// 容器标准实现
            /// </summary>
            public static IContainerStandard Container = null;
            /// <summary>
            /// 系统注入标准实现
            /// </summary>
            public static IAppInjectStandard Inject = null;
            /// <summary>
            /// 全局异常标准实现
            /// </summary>
            public static IAppDomainExceptionHandlerStandard DomainExceptionHandler = null;
            /// <summary>
            /// 类库扫描标准实现
            /// </summary>
            public static IAssemblyScanningStandard AssemblyScanning = null;
            /// <summary>
            /// JSON Web Token 标准实现
            /// </summary>
            public static IJwtHandlerStandard Jwt = null;
            /// <summary>
            /// 缓存缓存标准实现
            /// </summary>
            public static IAppCacheStandard Cache => AppCore.GetService<IAppCacheStandard>();
            /// <summary>
            /// Redis缓存缓存标准实现
            /// </summary>
            public static IRedisCacheStandard RedisCache => AppCore.GetService<IRedisCacheStandard>();
            /// <summary>
            /// JSON序列化标准实现
            /// </summary>
            public static IJsonSerializerStandard JSON => JsonConvert.GetJsonSerializer();

            /// <summary>
            /// 应用程序PID信息 
            /// </summary>
            /// <remarks>
            ///  与linux 系统的不同的是,这个PID是为了在微服务架构下,多节点的统一注册时,每个实例的名称
            ///  (暂时只在Windows 环境下进行测试)
            /// </remarks>
            public static IPIDPlugin PID=>AppCore.GetService<IPIDPlugin>();
        }
    }
}
