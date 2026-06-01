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
using Air.Cloud.Core;
using Air.Cloud.Core.Standard.Container;
using Air.Cloud.Modules.IIS.Dependencies;
using Air.Cloud.Modules.IIS.Helper;

using Microsoft.Extensions.DependencyInjection;

namespace Air.Cloud.Modules.IIS.Extensions
{
    /// <summary>
    /// <para>zh-cn:提供 IIS 容器模块服务注册相关的扩展方法。</para>
    /// <para>en-us:Provides extension methods for registering IIS container module services.</para>
    /// </summary>
    public static  class IISModuleExtensions
    {
        /// <summary>
        /// <para>zh-cn:添加 IIS 容器管理支持。</para>
        /// <para>en-us:Adds IIS container management support.</para>
        /// </summary>
        /// <remarks>
        /// <para>zh-cn:当前仅支持读取、停机和启动操作，并会在注册前检查 IIS 是否存在且正在运行。</para>
        /// <para>en-us:Currently supports only query, stop, and start operations, and checks whether IIS exists and is running before registration.</para>
        /// </remarks>
        /// <param name="services">
        /// <para>zh-cn:应用服务集合。</para>
        /// <para>en-us:The application service collection.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:完成 IIS 容器管理服务注册后的服务集合。</para>
        /// <para>en-us:The service collection after registering IIS container management services.</para>
        /// </returns>
        public static IServiceCollection AddIISService(this IServiceCollection services)
        {
            //这里增加IIS运行状态检查
            if (!(IISContainerHelper.IsIISExist() && IISContainerHelper.IsIISRunning()))
            {
                AppRealization.Output.Error(new Exception("IIS服务器状态异常"));
                return services;
            }
            services.AddSingleton<IContainerStandard, IISContainerDependency>();
            return services;
        }
    }
}
