// Copyright (c) 2020-2022 百小僧, Baiqian Co.,Ltd.
// Furion is licensed under Mulan PSL v2.
// You can use this software according to the terms and conditions of the Mulan PSL v2.
// You may obtain a copy of Mulan PSL v2 at:
//             https://gitee.com/dotnetchina/Furion/blob/master/LICENSE
// THIS SOFTWARE IS PROVIDED ON AN "AS IS" BASIS, WITHOUT WARRANTIES OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO NON-INFRINGEMENT, MERCHANTABILITY OR FIT FOR A PARTICULAR PURPOSE.
// See the Mulan PSL v2 for more details.

using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Air.Cloud.DataBase.Entities.Configures;

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