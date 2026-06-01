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
using Air.Cloud.EntityFrameWork.Oracle.Bulk;

namespace Air.Cloud.EntityFrameWork.Oracle.Configure
{
    /// <summary>
    /// <para>zh-cn:提供 Oracle 数据库上下文的连接、迁移程序集与批量操作配置。</para>
    /// <para>en-us:Provides Oracle DbContext configuration for connection, migration assembly, and batch operations.</para>
    /// </summary>
    public class OracleDatabaseConfigure : IDatabaseConfigure
    {
        /// <summary>
        /// <para>zh-cn:配置指定 DbContext 使用 Oracle 数据库提供程序。</para>
        /// <para>en-us:Configures the specified DbContext to use the Oracle database provider.</para>
        /// </summary>
        /// <typeparam name="TDbContext">
        /// <para>zh-cn:需要配置的 DbContext 类型。</para>
        /// <para>en-us:The DbContext type to configure.</para>
        /// </typeparam>
        /// <param name="builder">
        /// <para>zh-cn:当前数据库上下文选项构建器。</para>
        /// <para>en-us:The current database context options builder.</para>
        /// </param>
        /// <param name="connectionMetadata">
        /// <para>zh-cn:Oracle 数据库连接元数据，通常为连接字符串。</para>
        /// <para>en-us:The Oracle database connection metadata, usually the connection string.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:完成 Oracle 提供程序、迁移程序集与批量操作配置后的选项构建器。</para>
        /// <para>en-us:The options builder after configuring the Oracle provider, migration assembly, and batch operations.</para>
        /// </returns>
        public DbContextOptionsBuilder Configure<TDbContext>(DbContextOptionsBuilder builder,string connectionMetadata) 
            where TDbContext : DbContext
        {
            var dbContextOptionsBuilder = builder;
            // 获取数据库上下文特性
            var dbContextAttribute = DbProvider.GetAppDbContextAttribute(typeof(TDbContext));

            dbContextOptionsBuilder = OracleDbContextOptionsExtensions.UseOracle(builder, connectionMetadata, (options) =>
            {
                options.MigrationsAssembly(Db.MigrationAssemblyName);
            });
            dbContextOptionsBuilder=dbContextOptionsBuilder.UseBatchEF_Oracle();
            return dbContextOptionsBuilder;
        }
    }
}
