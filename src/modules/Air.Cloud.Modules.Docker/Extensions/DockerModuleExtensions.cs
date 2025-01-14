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
using Air.Cloud.Modules.Docker.Events;

using Microsoft.Extensions.DependencyInjection;

namespace Air.Cloud.Modules.Docker.Extensions
{
    public static  class DockerModuleExtensions
    {
        /// <summary>
        /// 添加Docker容器支持
        /// </summary>
        /// <remarks>
        /// 暂时只支持 读取,停机,启动的操作 注意:使用该模组的应用程序必须在Docker容器的宿主机中运行,无法在容器中运行
        /// </remarks>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddDockerEngineService(this IServiceCollection services)
        {
            services.AddSingleton<IContainerStandard, DockerContainerDependency>();
            DockerEngineEvent.DockerEngineEventSubscription();
            return services;
        }
        

    }
}
