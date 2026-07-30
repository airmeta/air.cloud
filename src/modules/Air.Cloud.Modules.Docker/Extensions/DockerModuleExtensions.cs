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
using Air.Cloud.Core.Standard.Container;
using Air.Cloud.Modules.Docker.Dependencies;
using Air.Cloud.Modules.Docker.Options;

using Microsoft.Extensions.DependencyInjection;

namespace Air.Cloud.Modules.Docker.Extensions
{
    /// <summary>
    /// <para>zh-cn:提供 Docker 容器模块服务注册相关的扩展方法。</para>
    /// <para>en-us:Provides extension methods for registering Docker container module services.</para>
    /// </summary>
    public static  class DockerModuleExtensions
    {
        /// <summary>
        /// <para>zh-cn:添加 Docker 容器管理支持。</para>
        /// <para>en-us:Adds Docker container management support.</para>
        /// </summary>
        /// <remarks>
        /// <para>zh-cn:当前仅支持读取、停机和启动操作；使用该模块的应用程序必须运行在 Docker 宿主机中。</para>
        /// <para>en-us:Currently supports only query, stop, and start operations; applications using this module must run on the Docker host.</para>
        /// </remarks>
        /// <param name="services">
        /// <para>zh-cn:应用服务集合。</para>
        /// <para>en-us:The application service collection.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:完成 Docker 容器管理服务注册后的服务集合。</para>
        /// <para>en-us:The service collection after registering Docker container management services.</para>
        /// </returns>
        public static IServiceCollection AddDockerEngineService(this IServiceCollection services)
        {
            return services.AddDockerEngineContainerService(options => options.Enabled = true);
        }

        /// <summary>
        /// <para>zh-cn:添加 Docker Engine Socket 容器访问服务。该服务默认不会访问 Docker，只有配置 `Enabled=true` 后才会执行容器列表、检查、启动和停止请求。</para>
        /// <para>en-us:Adds the Docker Engine socket container access service. The service does not access Docker by default and only executes list, inspect, start, and stop requests after `Enabled=true` is configured.</para>
        /// </summary>
        /// <param name="services">
        /// <para>zh-cn:应用服务集合。</para>
        /// <para>en-us:The application service collection.</para>
        /// </param>
        /// <param name="configure">
        /// <para>zh-cn:Docker Engine 容器访问配置委托。</para>
        /// <para>en-us>The Docker Engine container access configuration delegate.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:完成 Docker Engine 容器访问服务注册后的服务集合。</para>
        /// <para>en-us>The service collection after registering the Docker Engine container access service.</para>
        /// </returns>
        public static IServiceCollection AddDockerEngineContainerService(this IServiceCollection services, Action<DockerEngineContainerServiceOptions> configure)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configure);

            var options = new DockerEngineContainerServiceOptions();
            configure(options);
            services.AddSingleton(options);
            services.AddSingleton<DockerEngineContainerService>();
            services.AddSingleton<IContainerEngineStandard>(provider => provider.GetRequiredService<DockerEngineContainerService>());
            return services;
        }

    }
}
