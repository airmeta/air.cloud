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

using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Air.Cloud.EntityFrameWork.Core.Contexts.Dynamic;

/// <summary>
/// <para>zh-cn:动态模型缓存工厂。</para>
/// <para>en-us:Dynamic model cache key factory.</para>
/// </summary>
/// <remarks>
/// <para>zh-cn:主要用于数据库分表分库场景，通过切换缓存键触发 EF Core 重新构建模型。</para>
/// <para>en-us:Used mainly for database sharding scenarios, triggering EF Core to rebuild models by changing the cache key.</para>
/// </remarks>
[IgnoreScanning]
public class DynamicModelCacheKeyFactory : IModelCacheKeyFactory
{
    /// <summary>
    /// 动态模型缓存Key
    /// </summary>
    private static int cacheKey;

    /// <summary>
    /// <para>zh-cn:递增模型缓存键，使后续 DbContext 模型缓存失效并重新构建。</para>
    /// <para>en-us:Increments the model cache key so subsequent DbContext model caches are invalidated and rebuilt.</para>
    /// </summary>
    /// <remarks>
    /// <para>zh-cn:动态切换表或分库分表规则之后需要调用该方法。</para>
    /// <para>en-us:Call this method after dynamically switching tables or sharding rules.</para>
    /// </remarks>
    public static void RebuildModels()
    {
        Interlocked.Increment(ref cacheKey);
    }

    /// <summary>
    /// <para>zh-cn:创建 EF Core 模型缓存键。</para>
    /// <para>en-us:Creates the EF Core model cache key.</para>
    /// </summary>
    /// <param name="context">
    /// <para>zh-cn:当前正在构建模型的 DbContext 实例。</para>
    /// <para>en-us:The DbContext instance for which the model is being built.</para>
    /// </param>
    /// <param name="designTime">
    /// <para>zh-cn:指示当前模型是否用于设计时操作。</para>
    /// <para>en-us:Indicates whether the model is used for design-time operations.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:包含上下文类型和动态缓存版本的模型缓存键。</para>
    /// <para>en-us>A model cache key containing the context type and dynamic cache version.</para>
    /// </returns>
    public object Create(DbContext context, bool designTime)
    {
        return (context.GetType(), cacheKey);

    }
}
