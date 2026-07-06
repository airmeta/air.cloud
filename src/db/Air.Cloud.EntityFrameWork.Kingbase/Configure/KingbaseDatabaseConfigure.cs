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
using Air.Cloud.EntityFrameWork.Kingbase.Extensions;

namespace Air.Cloud.EntityFrameWork.Kingbase.Configure
{
    /// <summary>
    /// <para>zh-cn:KingbaseES V9 数据库配置器，负责把 Air.Cloud 统一数据库入口收到的连接元数据转换为 EF Core Kingbase Provider 配置。</para>
    /// <para>en-us:KingbaseES V9 database configurator that converts connection metadata received from the unified Air.Cloud database entry point into EF Core Kingbase provider configuration.</para>
    /// </summary>
    public class KingbaseDatabaseConfigure : IDatabaseConfigure
    {
        /// <summary>
        /// <para>zh-cn:配置指定 DbContext 使用 KingbaseES V9 Provider。connectionMetadata 会作为 Kingbase 连接字符串传递给 Kdbndp，迁移程序集统一使用 Air.Cloud 数据库核心中的 Db.MigrationAssemblyName。该方法只配置 DbContextOptionsBuilder，不会打开连接、创建数据库或执行迁移。</para>
        /// <para>en-us:Configures the specified DbContext to use the KingbaseES V9 provider. connectionMetadata is passed to Kdbndp as the Kingbase connection string, and the migrations assembly is taken from Db.MigrationAssemblyName in the Air.Cloud database core. This method only configures DbContextOptionsBuilder; it does not open a connection, create a database, or run migrations.</para>
        /// </summary>
        /// <typeparam name="TDbContext">
        /// <para>zh-cn:需要应用 Kingbase Provider 的 DbContext 类型。</para>
        /// <para>en-us:The DbContext type that should use the Kingbase provider.</para>
        /// </typeparam>
        /// <param name="builder">
        /// <para>zh-cn:当前数据库上下文选项构建器，不能为 null。</para>
        /// <para>en-us:The current database context options builder. It must not be null.</para>
        /// </param>
        /// <param name="connectionMetadata">
        /// <para>zh-cn:KingbaseES V9 连接元数据，通常是数据库连接字符串。</para>
        /// <para>en-us:The KingbaseES V9 connection metadata, usually the database connection string.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:已经完成 Kingbase Provider 和迁移程序集配置的数据库上下文选项构建器。</para>
        /// <para>en-us:The database context options builder configured with the Kingbase provider and migrations assembly.</para>
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <para>zh-cn:当 builder 为 null 时抛出。</para>
        /// <para>en-us:Thrown when builder is null.</para>
        /// </exception>
        public DbContextOptionsBuilder Configure<TDbContext>(DbContextOptionsBuilder builder, string connectionMetadata)
            where TDbContext : DbContext
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.UseKingbase(connectionMetadata, Db.MigrationAssemblyName);
        }
    }
}
