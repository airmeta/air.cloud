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
using Air.Cloud.Core.App;
using Air.Cloud.Core.App.Startups;
using Air.Cloud.WebApp.CorsAccessor.Extensions;
using Air.Cloud.WebApp.Extensions;
using Air.Cloud.WebApp.UnifyResult.Extensions;

using Mapster;

using Microsoft.Extensions.DependencyInjection;

namespace Air.Cloud.WebApp
{
    [AppStartup(int.MaxValue)]
    public class Startup : AppStartup
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            // 配置跨域
            services.AddCorsAccessor();
            //领域注册
            services.AddEntityDomainInject();
            // 控制器和规范化结果
            services.AddControllers().AddInjectWithUnifyResult();

        }
        public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // 配置错误页
            if (AppEnvironment.IsDevelopment) app.UseDeveloperExceptionPage();

            // 401，403 规范化结果
            app.UseUnifyResultStatusCodes();
            // Https 重定向
            app.UseHttpsRedirection();

            // 配置静态
            app.UseStaticFiles();

            // 配置路由
            app.UseRouting();

            // 配置跨域
            app.UseCorsAccessor();

            // 配置路由
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
