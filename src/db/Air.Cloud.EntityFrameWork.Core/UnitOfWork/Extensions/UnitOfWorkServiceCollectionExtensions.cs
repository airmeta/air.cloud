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
using Microsoft.Extensions.DependencyInjection;

namespace Air.Cloud.EntityFrameWork.Core.UnitOfWork.Extensions;

/// <summary>
/// 工作单元拓展类
/// </summary>
[IgnoreScanning]
public static class UnitOfWorkServiceCollectionExtensions
{
    /// <summary>
    /// 添加工作单元服务
    /// </summary>
    /// <param name="mvcBuilder">Mvc构建器</param>
    /// <returns>Mvc构建器</returns>
    public static IMvcBuilder AddUnitOfWork<TUnitOfWork>(this IMvcBuilder mvcBuilder)
        where TUnitOfWork : class, IUnitOfWork
    {
        mvcBuilder.Services.AddUnitOfWork<TUnitOfWork>();

        return mvcBuilder;
    }

    /// <summary>
    /// 添加工作单元服务
    /// </summary>
    /// <param name="services"></param>
    /// <returns>Mvc构建器</returns>
    public static IServiceCollection AddUnitOfWork<TUnitOfWork>(this IServiceCollection services)
        where TUnitOfWork : class, IUnitOfWork
    {
        // 注册工作单元服务
        services.AddTransient<IUnitOfWork, TUnitOfWork>();
        return services;
    }
}