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

using Air.Cloud.EntityFrameWork.Core.Interceptors;

using Microsoft.EntityFrameworkCore.Diagnostics;

using System.Collections.Concurrent;

namespace Air.Cloud.EntityFrameWork.Core.Extensions.DatabaseProvider;

/// <summary>
/// 常量、公共方法配置类
/// <para>zh-cn:该成员的数据库访问行为。</para>
/// <para>en-us:Database access behavior provided by this member.</para>
/// </summary>
internal static class Penetrates
{
    /// <summary>
    /// 数据库上下文描述器
    /// <para>zh-cn:该成员的数据库访问行为。</para>
    /// <para>en-us:Database access behavior provided by this member.</para>
    /// </summary>
    internal static readonly ConcurrentDictionary<Type, Type> DbContextDescriptors;

    /// <summary>
    /// 构造函数
    /// <para>zh-cn:该成员的数据库访问行为。</para>
    /// <para>en-us:Database access behavior provided by this member.</para>
    /// </summary>
    static Penetrates()
    {
        DbContextDescriptors = new ConcurrentDictionary<Type, Type>();
    }

    /// <summary>
    /// 配置 SqlServer 数据库上下文
    /// <para>zh-cn:该成员的数据库访问行为。</para>
    /// <para>en-us:Database access behavior provided by this member.</para>
    /// </summary>
    /// <param name="optionBuilder"><para>zh-cn:数据库上下文选项构建器</para><para>en-us:The optionBuilder value.</para></param>
    /// <param name="interceptors"><para>zh-cn:拦截器</para><para>en-us:The interceptors value.</para></param>
    /// <returns><para>zh-cn:操作结果。</para><para>en-us:The operation result.</para></returns>
    internal static Action<IServiceProvider, DbContextOptionsBuilder> ConfigureDbContext(Action<IServiceProvider, DbContextOptionsBuilder> optionBuilder, params IInterceptor[] interceptors)
    {
        return (serviceProvider, options) =>
        {
            // 只有开发环境开启
            if (AppEnvironment.IsDevelopment)
            {
                options.EnableDetailedErrors().EnableSensitiveDataLogging();
            }

            optionBuilder?.Invoke(serviceProvider, options);

            // 添加拦截器
            AddInterceptors(interceptors, options);
        };
    }

    /// <summary>
    /// 检查数据库上下文是否绑定
    /// <para>zh-cn:该成员的数据库访问行为。</para>
    /// <para>en-us:Database access behavior provided by this member.</para>
    /// </summary>
    /// <param name="dbContextLocatorType"><para>zh-cn:参数值。</para><para>en-us:The dbContextLocatorType value.</para></param>
    /// <param name="dbContextType"><para>zh-cn:参数值。</para><para>en-us:The dbContextType value.</para></param>
    /// <returns><para>zh-cn:操作结果。</para><para>en-us:The operation result.</para></returns>
    internal static void CheckDbContextLocator(Type dbContextLocatorType, out Type dbContextType)
    {
        if (!DbContextDescriptors.TryGetValue(dbContextLocatorType, out var foundDbContextType)) throw new InvalidCastException($" The dbcontext locator `{dbContextLocatorType.Name}` is not bind.");
        dbContextType = foundDbContextType;
    }

    /// <summary>
    /// 数据库数据库拦截器
    /// <para>zh-cn:该成员的数据库访问行为。</para>
    /// <para>en-us:Database access behavior provided by this member.</para>
    /// </summary>
    /// <param name="interceptors"><para>zh-cn:拦截器</para><para>en-us:The interceptors value.</para></param>
    /// <param name="options"><para>zh-cn:参数值。</para><para>en-us:The options value.</para></param>
    private static void AddInterceptors(IInterceptor[] interceptors, DbContextOptionsBuilder options)
    {
        // 添加拦截器
        var interceptorList = GetDefaultInterceptors();

        if (interceptors != null && interceptors.Length > 0)
        {
            interceptorList.AddRange(interceptors);
        }
        options.AddInterceptors(interceptorList.ToArray());
    }

    /// <summary>
    /// 获取默认拦截器
    /// <para>zh-cn:该成员的数据库访问行为。</para>
    /// <para>en-us:Database access behavior provided by this member.</para>
    /// </summary>
    public static List<IInterceptor> GetDefaultInterceptors()
    {
        return new List<IInterceptor>
            {
                new SqlConnectionProfilerInterceptor(),
                new SqlCommandProfilerInterceptor(),
                new DbContextSaveChangesInterceptor()
            };
    }

}
