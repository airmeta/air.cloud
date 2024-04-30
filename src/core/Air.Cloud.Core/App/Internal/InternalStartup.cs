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
using Air.Cloud.Core.App.Startups;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
namespace Air.Cloud.Core.App.Loader
{
    /// <summary>
    /// <para>zh-cn: 内置Startup,防止出现未配置启动项导致失败的情况</para>
    /// <para>en-us: Built-in Startup to prevent failure due to unconfigured startup items</para>
    /// </summary>
    public class InternalStartup : AppStartup
    {
        public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            
        }
        public override void ConfigureServices(IServiceCollection services)
        {

        }
    }
}
