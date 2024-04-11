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
using Air.Cloud.Core.Handler;
using Air.Cloud.Core.Standard.Assemblies;
using Air.Cloud.Core.Standard.Cache;
using Air.Cloud.Core.Standard.Cache.Redis;
using Air.Cloud.Core.Standard.Container;
using Air.Cloud.Core.Standard.Exceptions;
using Air.Cloud.Core.Standard.Jwt;
using Air.Cloud.Core.Standard.WebApp;

using Microsoft.AspNetCore.DataProtection.KeyManagement;

using Newtonsoft.Json.Linq;

using System.Reflection;
using System.Xml.Linq;

namespace Air.Cloud.Core.Standard
{
    /// <summary>
    /// 约定注入实现
    /// </summary>
    public static partial class AppStandardRealization
    {
        /// <summary>
        /// 
        /// </summary>
        public static IAppPrintStandard PrintStandard => InternalRealization.PrintStandard ?? DefaultRealization.PrintStandard;
        /// <summary>
        /// 容器约定注入
        /// </summary>
        public static IContainerStandard ContainerStandard => InternalRealization.ContainerStandard ?? DefaultRealization.ContainerStandard;
        /// <summary>
        /// web服务约定注入
        /// </summary>
        public static IAppInjectStandard AppInjectStandard => InternalRealization.AppInjectStandard ?? DefaultRealization.AppInjectStandard;
        /// <summary>
        ///  全局异常处理
        /// </summary>
        public static IAppDomainExceptionHandlerStandard AppDomainExceptionHandlerStandard => InternalRealization.AppDomainExceptionHandlerStandard ?? DefaultRealization.AppDomainExceptionHandlerStandard;
        /// <summary>
        /// 类库扫描
        /// </summary>
        public static IAssemblyScanningStandard AssemblyScanningStandard => InternalRealization.AssemblyScanningStandard ?? DefaultRealization.AssemblyScanningStandard;
        /// <summary>
        /// JWT Handler 约定注入
        /// </summary>
        public static IJwtHandlerStandard JwtHandlerStandard=>InternalRealization.JwtHandlerStandard ?? DefaultRealization.JwtHandlerStandard;

        /// <summary>
        /// 缓存约定注入
        /// </summary>
        public static IAppCacheStandard AppCacheStandard => InternalRealization.AppCacheStandard ?? DefaultRealization.AppCacheStandard;
        /// <summary>
        /// Redis缓存约定注入
        /// </summary>
        public static IRedisCacheStandard RedisCacheStandard => InternalRealization.RedisCacheStandard ?? DefaultRealization.RedisCacheStandard;
        /// <summary>
        /// 设置约定实现
        /// </summary>
        /// <typeparam name="TIStandard">约定类型</typeparam>
        /// <param name="standard">约定实现</param>
        public static void SetDependency<TIStandard>(TIStandard standard) where TIStandard : IStandard
        {
            BindingFlags flag = BindingFlags.Static;
            FieldInfo Field = typeof(InternalRealization).GetFields(flag).FirstOrDefault(s=>s.FieldType==standard.GetType());
            Field.SetValue(null, standard);
        }

        /// <summary>
        /// 默认标准实现
        /// </summary>
        protected static class DefaultRealization
        {
            /// <summary>
            /// 打印约定注入
            /// </summary>
            public static IAppPrintStandard PrintStandard => new DefaultAppPrintDependency();
            /// <summary>
            /// 容器约定注入
            /// </summary>
            public static IContainerStandard ContainerStandard => null;
            /// <summary>
            /// 程序注入约定
            /// </summary>
            public static IAppInjectStandard AppInjectStandard => new DefaultAppInjectDependency();
            /// <summary>
            /// 全局默认异常处理
            /// </summary>
            public static IAppDomainExceptionHandlerStandard AppDomainExceptionHandlerStandard => new DefaultAppDomainExcepetionHandlerDependency();
            /// <summary>
            /// 全局默认类库扫描实现
            /// </summary>
            public static IAssemblyScanningStandard AssemblyScanningStandard => new DefaultAssemblyScanningDependency();
            /// <summary>
            /// 默认的Jwt Handler 实现
            /// </summary>
            public static IJwtHandlerStandard JwtHandlerStandard => new DefaultJwtHandlerDependency();
            /// <summary>
            /// 缓存约定注入
            /// </summary>
            public static IAppCacheStandard AppCacheStandard=> throw new NotImplementedException("未引入任何关于Cache的模组或插件,如果引入,则检查你的代码是否注入该实现");
            /// <summary>
            /// Redis缓存约定注入
            /// </summary>
            public static IRedisCacheStandard RedisCacheStandard => throw new NotImplementedException("未引入任何关于Redis的模组或插件,如果引入,则检查你的代码是否注入该实现");
        }
        /// <summary>
        /// 自定义标准实现
        /// </summary>
        protected static class InternalRealization
        {
            /// <summary>
            /// 打印约定注入
            /// </summary>
            public static IAppPrintStandard PrintStandard = null;
            /// <summary>
            /// 容器约定注入
            /// </summary>
            public static IContainerStandard ContainerStandard = null;
            /// <summary>
            /// 程序注入约定
            /// </summary>
            public static IAppInjectStandard AppInjectStandard = null;
            /// <summary>
            /// 全局异常处理
            /// </summary>
            public static IAppDomainExceptionHandlerStandard AppDomainExceptionHandlerStandard = null;
            /// <summary>
            /// 全局类库扫描实现
            /// </summary>
            public static IAssemblyScanningStandard AssemblyScanningStandard = null;
            /// <summary>
            /// jwt handler 约定注入
            /// </summary>
            public static IJwtHandlerStandard JwtHandlerStandard = null;
            /// <summary>
            /// 缓存约定注入
            /// </summary>
            public static IAppCacheStandard AppCacheStandard => AppCore.GetService<IAppCacheStandard>();
            /// <summary>
            /// Redis缓存约定注入
            /// </summary>
            public static IRedisCacheStandard RedisCacheStandard => AppCore.GetService<IRedisCacheStandard>();
        }
    }
}
