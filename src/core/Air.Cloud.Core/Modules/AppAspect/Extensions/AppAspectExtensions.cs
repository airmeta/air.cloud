
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
using Microsoft.Extensions.DependencyInjection;

namespace Air.Cloud.Core.Modules.AppAspect.Extensions
{
    /// <summary>
    /// <para>zh-cn:Aspect服务扩展</para>
    /// <para>en-us:Aspect service extensions</para>
    /// </summary>
    public static class AppAspectExtensions
    {
        /// <summary>
        /// <para>zh-cn:初始化Aspect服务</para>
        /// <para>en-us: Initialize Aspect service</para>    
        /// </summary>
        /// <param name="services">
        /// <para>zh-cn:服务集合</para>
        /// <para>en-us: Service collection</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:服务集合</para>
        /// <para>en-us: Service collection</para>
        /// </returns>
        public static IServiceCollection AddAspect(this IServiceCollection services)
        {
            AppAspectOptionsBuilder appAspectOptionsBuilder = new AppAspectOptionsBuilder();
            appAspectOptionsBuilder.Build(services);
            return services;
        }

    }
}
