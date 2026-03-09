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
using Air.Cloud.EntityFrameWork.Core.Entities.Configures;

namespace Air.Cloud.EntityFrameWork.Core.Contexts.Builders.Models;

/// <summary>
/// 数据库上下文关联类型
/// </summary>
internal sealed class DbContextCorrelationType
{
    /// <summary>
    /// 构造函数
    /// </summary>
    internal DbContextCorrelationType()
    {
        EntityTypes = new List<Type>();
        EntityNoKeyTypes = new List<Type>();
        EntityTypeBuilderTypes = new List<Type>();
        EntitySeedDataTypes = new List<Type>();
        EntityChangedTypes = new List<Type>();
        ModelBuilderFilterTypes = new List<Type>();
        EntityMutableTableTypes = new List<Type>();
        ModelBuilderFilterInstances = new List<IPrivateModelBuilderFilter>();
        DbFunctionMethods = new List<MethodInfo>();
    }

    /// <summary>
    /// 关联的数据库上下文
    /// </summary>
    internal Type DbContextLocator { get; set; }

    /// <summary>
    /// 所有关联类型
    /// </summary>
    internal List<Type> Types { get; set; }

    /// <summary>
    /// 实体类型集合
    /// </summary>
    internal List<Type> EntityTypes { get; set; }

    /// <summary>
    /// 无键实体类型集合
    /// </summary>
    internal List<Type> EntityNoKeyTypes { get; set; }

    /// <summary>
    /// 实体构建器类型集合
    /// </summary>
    internal List<Type> EntityTypeBuilderTypes { get; set; }

    /// <summary>
    /// 种子数据类型集合
    /// </summary>
    internal List<Type> EntitySeedDataTypes { get; set; }

    /// <summary>
    /// 实体数据改变类型
    /// </summary>
    internal List<Type> EntityChangedTypes { get; set; }

    /// <summary>
    /// 模型构建筛选器类型集合
    /// </summary>
    internal List<Type> ModelBuilderFilterTypes { get; set; }

    /// <summary>
    /// 可变表实体类型集合
    /// </summary>
    internal List<Type> EntityMutableTableTypes { get; set; }

    /// <summary>
    /// 数据库函数方法集合
    /// </summary>
    internal List<MethodInfo> DbFunctionMethods { get; set; }

    /// <summary>
    /// 模型构建器筛选器实例
    /// </summary>
    internal List<IPrivateModelBuilderFilter> ModelBuilderFilterInstances { get; set; }
}