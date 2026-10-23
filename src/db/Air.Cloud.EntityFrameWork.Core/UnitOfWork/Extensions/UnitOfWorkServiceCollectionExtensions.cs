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
/// <para>zh-cn:注册工作单元实现和 MVC 集成的扩展方法。</para>
/// <para>en-us:Extension methods for registering a work-unit implementation and MVC integration.</para>
/// </summary>
[IgnoreScanning]
public static class UnitOfWorkServiceCollectionExtensions
{
    /// <summary>
    /// <para>zh-cn:将指定工作单元实现注册到 MVC 构建器关联的服务集合。</para>
    /// <para>en-us:Registers the specified work-unit implementation in the service collection associated with the MVC builder.</para>
    /// </summary>
    /// <typeparam name="TUnitOfWork"><para>zh-cn:工作单元实现类型。</para><para>en-us:The work-unit implementation type.</para></typeparam>
    /// <param name="mvcBuilder"><para>zh-cn:要配置的 MVC 构建器。</para><para>en-us:The MVC builder to configure.</para></param>
    /// <returns><para>zh-cn:原 MVC 构建器。</para><para>en-us:The original MVC builder.</para></returns>
    public static IMvcBuilder AddUnitOfWork<TUnitOfWork>(this IMvcBuilder mvcBuilder)
        where TUnitOfWork : class, IUnitOfWork
    {
        mvcBuilder.Services.AddUnitOfWork<TUnitOfWork>();

        return mvcBuilder;
    }

    /// <summary>
    /// <para>zh-cn:将指定工作单元实现注册为瞬时服务。</para>
    /// <para>en-us:Registers the specified work-unit implementation as a transient service.</para>
    /// </summary>
    /// <typeparam name="TUnitOfWork"><para>zh-cn:工作单元实现类型。</para><para>en-us:The work-unit implementation type.</para></typeparam>
    /// <param name="services"><para>zh-cn:要配置的服务集合。</para><para>en-us:The service collection to configure.</para></param>
    /// <returns><para>zh-cn:原服务集合。</para><para>en-us:The original service collection.</para></returns>
    public static IServiceCollection AddUnitOfWork<TUnitOfWork>(this IServiceCollection services)
        where TUnitOfWork : class, IUnitOfWork
    {
        // 注册工作单元服务
        services.AddTransient<IUnitOfWork, TUnitOfWork>();
        return services;
    }
}
