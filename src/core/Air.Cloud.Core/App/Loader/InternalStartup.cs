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
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

using Air.Cloud.Core.App.Loader;
namespace Air.Cloud.Core.App.Loader
{
    /// <summary>
    /// 内置Startup 进行初始化配置
    /// </summary>
    public class InternalStartup : IStartup
    {
        /// <summary>
        /// 注入
        /// </summary>
        /// <param name="app"></param>
        /// <exception cref="NotImplementedException"></exception>
        public void Configure(IApplicationBuilder app)
        {
            //这里逐步进行逐步应用
        }
        /// <summary>
        /// 注入
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            throw new NotImplementedException();
        }
    }
}
