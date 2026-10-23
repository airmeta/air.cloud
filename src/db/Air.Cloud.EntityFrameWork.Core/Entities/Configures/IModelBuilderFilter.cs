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
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Air.Cloud.EntityFrameWork.Core.Entities.Configures;

/// <summary>
/// 数据库模型构建筛选器依赖接口
/// <para>zh-cn:该成员的数据库访问行为。</para>
/// <para>en-us:Database access behavior provided by this member.</para>
/// </summary>
public interface IPrivateModelBuilderFilter : IPrivateModelBuilder
{
    /// <summary>
    /// 模型构建之前
    /// <para>zh-cn:该成员的数据库访问行为。</para>
    /// <para>en-us:Database access behavior provided by this member.</para>
    /// </summary>
    /// <param name="modelBuilder"><para>zh-cn:模型构建器</para><para>en-us:The modelBuilder value.</para></param>
    /// <param name="entityBuilder"><para>zh-cn:实体构建器</para><para>en-us:The entityBuilder value.</para></param>
    /// <param name="dbContext"><para>zh-cn:数据库上下文</para><para>en-us:The dbContext value.</para></param>
    /// <param name="dbContextLocator"><para>zh-cn:数据库上下文定位器</para><para>en-us:The dbContextLocator value.</para></param>
    void OnCreating(ModelBuilder modelBuilder, EntityTypeBuilder entityBuilder, DbContext dbContext, Type dbContextLocator);

    /// <summary>
    /// 模型构建之后
    /// <para>zh-cn:该成员的数据库访问行为。</para>
    /// <para>en-us:Database access behavior provided by this member.</para>
    /// </summary>
    /// <param name="modelBuilder"><para>zh-cn:模型构建器</para><para>en-us:The modelBuilder value.</para></param>
    /// <param name="entityBuilder"><para>zh-cn:实体构建器</para><para>en-us:The entityBuilder value.</para></param>
    /// <param name="dbContext"><para>zh-cn:数据库上下文</para><para>en-us:The dbContext value.</para></param>
    /// <param name="dbContextLocator"><para>zh-cn:数据库上下文定位器</para><para>en-us:The dbContextLocator value.</para></param>
    void OnCreated(ModelBuilder modelBuilder, EntityTypeBuilder entityBuilder, DbContext dbContext, Type dbContextLocator) { }
}
