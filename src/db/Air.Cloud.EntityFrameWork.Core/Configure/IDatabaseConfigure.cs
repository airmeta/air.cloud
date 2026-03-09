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
namespace Air.Cloud.EntityFrameWork.Core.Configure
{
    /// <summary>
    /// <para>zh-cn:数据库构建器</para>
    /// <para>en-us:Database builder</para>
    /// </summary>
    public interface IDatabaseConfigure
    {
        /// <summary>
        /// <para>zh-cn:配置数据库上下文</para>
        /// <para>en-us:Configure the database context</para>
        /// </summary>
        /// <typeparam name="TDbContext">
        /// <para>zh-cn:数据库上下文类型</para>
        /// <para>en-us:Database context type</para>
        /// </typeparam>
        /// <param name="builder">
        ///  <para>zh-cn:数据库上下文选项构建器</para>
        ///  <para>en-us:Database context options builder</para>
        /// </param>
        /// <param name="connectionMetadata">
        ///  <para>zh-cn:连接元数据</para>
        ///  <para>en-us:Connection metadata</para>
        /// </param>
        /// <returns>
        ///  <para>zh-cn:配置后的数据库上下文选项构建器</para>
        ///  <para>en-us:Configured database context options builder</para>
        /// </returns>
        public DbContextOptionsBuilder Configure<TDbContext>(DbContextOptionsBuilder builder, string connectionMetadata) where TDbContext : DbContext;

    }
}
