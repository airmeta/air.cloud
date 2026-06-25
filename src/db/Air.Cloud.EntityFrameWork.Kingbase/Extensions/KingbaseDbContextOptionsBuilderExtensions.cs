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

namespace Air.Cloud.EntityFrameWork.Kingbase.Extensions
{
    /// <summary>
    /// <para>zh-cn:KingbaseES V9 的 EF Core 选项构建扩展，封装 Kdbndp.EntityFrameworkCore.KingbaseES_V9 提供的 UseKdbndp Provider 入口。</para>
    /// <para>en-us:EF Core options builder extensions for KingbaseES V9. They wrap the UseKdbndp provider entry supplied by Kdbndp.EntityFrameworkCore.KingbaseES_V9.</para>
    /// </summary>
    public static class KingbaseDbContextOptionsBuilderExtensions
    {
        /// <summary>
        /// <para>zh-cn:为 DbContextOptionsBuilder 应用 KingbaseES V9 Provider。connectionString 会原样传递给 UseKdbndp；migrationAssemblyName 非空时会配置迁移程序集，适合在迁移程序集与 DbContext 程序集分离时使用。该扩展依赖 Kingbase 驱动的 EF Core 6 运行时边界，不会注册 Air.Cloud.EntityFrameWork.Core 的 EF7 统一数据库配置器。</para>
        /// <para>en-us:Applies the KingbaseES V9 provider to a DbContextOptionsBuilder. connectionString is passed through to UseKdbndp; migrationAssemblyName configures the migrations assembly when it is not empty, which is useful when migrations live outside the DbContext assembly. This extension follows the EF Core 6 runtime boundary required by the Kingbase driver and does not register the EF7 unified database configurator from Air.Cloud.EntityFrameWork.Core.</para>
        /// </summary>
        /// <param name="builder">
        /// <para>zh-cn:待配置的 DbContextOptionsBuilder，不能为 null。</para>
        /// <para>en-us:The DbContextOptionsBuilder to configure. It must not be null.</para>
        /// </param>
        /// <param name="connectionString">
        /// <para>zh-cn:KingbaseES V9 连接字符串，透传给 UseKdbndp。</para>
        /// <para>en-us:The KingbaseES V9 connection string passed through to UseKdbndp.</para>
        /// </param>
        /// <param name="migrationAssemblyName">
        /// <para>zh-cn:可选迁移程序集名称；为空时使用 Provider 默认行为。</para>
        /// <para>en-us:Optional migrations assembly name. When empty, the provider default is used.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:已应用 KingbaseES Provider 的 DbContextOptionsBuilder。</para>
        /// <para>en-us:The DbContextOptionsBuilder configured with the KingbaseES provider.</para>
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <para>zh-cn:当 builder 为 null 时抛出。</para>
        /// <para>en-us:Thrown when builder is null.</para>
        /// </exception>
        public static DbContextOptionsBuilder UseKingbase(
            this DbContextOptionsBuilder builder,
            string connectionString,
            string migrationAssemblyName = null)
        {
            AppRealization.Output.Print("Kingbase使用警告", "Kingbase 不支持EF 7.0版本, 当前已为你强制降低到EF 6.0,请勿使用EF 7.0特性");
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.UseKdbndp(connectionString, options =>
            {
                if (!string.IsNullOrWhiteSpace(migrationAssemblyName))
                {
                    options.MigrationsAssembly(migrationAssemblyName);
                }
            });
        }
    }
}
