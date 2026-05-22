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
using Air.Cloud.EntityFrameWork.Core;
using Air.Cloud.EntityFrameWork.Core.Configure;
using Air.Cloud.EntityFrameWork.Core.Internal;

using Microsoft.EntityFrameworkCore;

namespace Air.Cloud.EntityFrameWork.MySQL.Configure
{
    public class MySQLDatabaseConfigure : IDatabaseConfigure
    {
        public DbContextOptionsBuilder Configure<TDbContext>(DbContextOptionsBuilder builder,string connectionMetadata) 
            where TDbContext : DbContext
        {
            var dbContextOptionsBuilder = builder;
            // 获取数据库上下文特性
            var dbContextAttribute = DbProvider.GetAppDbContextAttribute(typeof(TDbContext));

            dbContextOptionsBuilder = dbContextOptionsBuilder.UseMySql(connectionMetadata, ServerVersion.AutoDetect(connectionMetadata), (options) =>
            {
                options.MigrationsAssembly(Db.MigrationAssemblyName);
            });
            return dbContextOptionsBuilder;
        }
    }
}
