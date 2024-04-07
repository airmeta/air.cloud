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
using Air.Cloud.Core.Standard.Container;
using Air.Cloud.Core.Standard.Exceptions;
using Air.Cloud.Core.Standard.Jwt;
using Air.Cloud.Core.Standard.WebApp;

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
        /// 默认标准实现
        /// </summary>
        public static class DefaultRealization
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
        }
        /// <summary>
        /// 自定义标准实现
        /// </summary>
        public static class InternalRealization
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
            public static IJwtHandlerStandard JwtHandlerStandard => new DefaultJwtHandlerDependency();
        }
    }
}
