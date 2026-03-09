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
/// 动态模型缓存工厂
/// </summary>
/// <remarks>主要用来实现数据库分表分库</remarks>
[IgnoreScanning]
public class DynamicModelCacheKeyFactory : IModelCacheKeyFactory
{
    /// <summary>
    /// 动态模型缓存Key
    /// </summary>
    private static int cacheKey;

    /// <summary>
    /// 重写构建模型
    /// </summary>
    /// <remarks>动态切换表之后需要调用该方法</remarks>
    public static void RebuildModels()
    {
        Interlocked.Increment(ref cacheKey);
    }


    public object Create(DbContext context, bool designTime)
    {
        return (context.GetType(), cacheKey);

    }
}