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
using air.cloud.open.repository.DbContexts;

using Air.Cloud.Core.App.Startups;
using Air.Cloud.EntityFrameWork.Core.Extensions;
using Air.Cloud.EntityFrameWork.Core.Extensions.DatabaseProvider;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

using DbContextSaveChangesInterceptor = air.cloud.open.repository.DbContexts.DbContextSaveChangesInterceptor;

namespace air.cloud.open.repository
{
    public class Startup : AppStartup
    {
        public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {

        }

        public override void ConfigureServices(IServiceCollection services)
        {
            // zh-cn:数据库 Provider 由 Air.Cloud.EntityFrameWork.PostgreSQL 的 IDatabaseConfigure 注入，AddDbPool 会读取 OpenConnectionString。
            // en-us:The database provider is supplied by Air.Cloud.EntityFrameWork.PostgreSQL through IDatabaseConfigure; AddDbPool reads OpenConnectionString.
            services.AddDatabaseAccessor(options =>
            {
                options.AddDbPool<DefaultDbContext>(
                       //注册拦截器
                       interceptors: new Microsoft.EntityFrameworkCore.Diagnostics.IInterceptor[]
                       {
                        new DbContextSaveChangesInterceptor(),
                        new SqlCommandAuditInterceptor()
                       });
            }, "air.cloud.Database.Migrations");
        }
    }
}
