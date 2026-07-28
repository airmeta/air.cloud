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
using Microsoft.EntityFrameworkCore;

namespace air.cloud.security.common.Extensions
{
    /// <summary>
    /// <para>zh-cn:PostgreSQL 模型构建扩展。</para>
    /// <para>en-us:PostgreSQL model builder extensions.</para>
    /// </summary>
    public static class PostgreSqlDateTimeModelBuilderExtensions
    {
        /// <summary>
        /// <para>zh-cn:将当前模型中的 DateTime 与 Nullable DateTime 属性统一映射为 PostgreSQL 的 timestamp without time zone，避免 Npgsql 将本地时间写入 timestamp with time zone 时拒绝 Local Kind。</para>
        /// <para>en-us:Maps DateTime and nullable DateTime properties in the current model to PostgreSQL timestamp without time zone so Npgsql does not reject Local DateTime values as timestamp with time zone parameters.</para>
        /// </summary>
        /// <param name="modelBuilder">
        /// <para>zh-cn:正在构建的 EF Core 模型。</para>
        /// <para>en-us:The EF Core model being built.</para>
        /// </param>
        public static void UseTimestampWithoutTimeZoneForDateTime(this ModelBuilder modelBuilder)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?))
                    {
                        property.SetColumnType("timestamp without time zone");
                    }
                }
            }
        }
    }
}
