﻿
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
using Air.Cloud.Core.App;
using Air.Cloud.Core.App.Startups;
using Air.Cloud.DataBase.Extensions;
using Air.Cloud.DataBase.Extensions.DatabaseProvider;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using unit.webapp.repository;
using unit.webapp.repository.DbContexts;

using DbContextSaveChangesInterceptor = unit.webapp.repository.DbContexts.DbContextSaveChangesInterceptor;

namespace unit.webapp.model
{
    public class Startup : AppStartup
    {
        public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {

        }

        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddDatabaseAccessor(options =>
            {
                options.AddDbPool<DefaultDbContext>(providerName: default,
                       (services, opt) =>
                       {
                           var conn = AppCore.Configuration["ConnectionStrings:OracleConnectionString"];
                           //设置oracle使用的版本
                           opt.EnableSensitiveDataLogging().UseOracle(conn, b =>
                           {
                               b.UseOracleSQLCompatibility("11");
                           });
                       },
                       //注册拦截器
                       interceptors: new Microsoft.EntityFrameworkCore.Diagnostics.IInterceptor[]
                       {
                        new DbContextSaveChangesInterceptor(),
                        new SqlCommandAuditInterceptor()
                       });
            }, "Air.Database.Migrations");

            //使用EF的分析工具
            HibernatingRhinos.Profiler.Appender.EntityFramework.EntityFrameworkProfiler.Initialize();
        }
    }
}