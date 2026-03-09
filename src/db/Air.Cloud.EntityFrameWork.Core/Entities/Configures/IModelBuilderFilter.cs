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
/// </summary>
public interface IPrivateModelBuilderFilter : IPrivateModelBuilder
{
    /// <summary>
    /// 模型构建之前
    /// </summary>
    /// <param name="modelBuilder">模型构建器</param>
    /// <param name="entityBuilder">实体构建器</param>
    /// <param name="dbContext">数据库上下文</param>
    /// <param name="dbContextLocator">数据库上下文定位器</param>
    void OnCreating(ModelBuilder modelBuilder, EntityTypeBuilder entityBuilder, DbContext dbContext, Type dbContextLocator);

    /// <summary>
    /// 模型构建之后
    /// </summary>
    /// <param name="modelBuilder">模型构建器</param>
    /// <param name="entityBuilder">实体构建器</param>
    /// <param name="dbContext">数据库上下文</param>
    /// <param name="dbContextLocator">数据库上下文定位器</param>
    void OnCreated(ModelBuilder modelBuilder, EntityTypeBuilder entityBuilder, DbContext dbContext, Type dbContextLocator) { }
}