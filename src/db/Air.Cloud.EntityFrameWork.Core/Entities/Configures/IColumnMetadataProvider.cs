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

namespace Air.Cloud.EntityFrameWork.Core.Entities.Configures;

/// <summary>
/// <para>zh-cn:列元数据提供器接口。业务项目可通过 services.AddSingleton&lt;IColumnMetadataProvider, MyColumnMetadataProvider&gt; 注册一个实现，在 EF Core 模型构建阶段动态调整列类型、列名等元数据。</para>
/// <para>en-us:Column metadata provider contract. Applications can register an implementation with services.AddSingleton&lt;IColumnMetadataProvider, MyColumnMetadataProvider&gt; to dynamically adjust column type, column name, and other metadata during EF Core model building.</para>
/// </summary>
public interface IColumnMetadataProvider
{
    /// <summary>
    /// <para>zh-cn:应用当前属性的列元数据规则。该方法会在每个实体属性模型构建时调用，未注册实现时框架不执行任何列元数据改写。</para>
    /// <para>en-us:Applies column metadata rules to the current property. This method is called while each entity property model is built; when no implementation is registered, the framework does not rewrite column metadata.</para>
    /// </summary>
    /// <param name="context">
    /// <para>zh-cn:当前实体属性的模型构建上下文。</para>
    /// <para>en-us:The model-building context for the current entity property.</para>
    /// </param>
    void Apply(ColumnMetadataContext context);
}
