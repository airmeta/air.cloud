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
    public static  class IISModuleExtensions
    {
        /// <summary>
        /// 添加IIS容器支持
        /// </summary>
        /// <remarks>
        /// 暂时只支持 读取,停机,启动的操作
        /// </remarks>
        /// <param name="services"></param>
        /// <returns></returns>
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
