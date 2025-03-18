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
using Air.Cloud.Core.Standard.Event.Builders;
using Microsoft.Extensions.DependencyInjection;

namespace Air.Cloud.Core.Standard.Event.Attributes;
/// <summary>
/// 事件拦截器特性（基于 AOP 的拦截器模式实现）
/// </summary>
/// <remarks>
/// 该特性用于标记需要事件拦截处理的方法，
/// 通过中间件链实现事件的前置/后置处理逻辑
/// </remarks>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public sealed class EventInterceptorAttribute : Attribute
{
    /// <summary>
    /// 执行顺序（数值越小优先级越高）
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// 事件中间件类型集合（必须实现 IEventInterceptor 接口）
    /// </summary>
    private Type[] TMiddlewares { get; set; }

    /// <summary>
    /// 构造函数（默认顺序为1）
    /// </summary>
    /// <param name="tMiddlewares">事件中间件类型数组</param>
    public EventInterceptorAttribute(params Type[] tMiddlewares)
        : this(1, tMiddlewares) { }

    /// <summary>
    /// 带顺序参数的构造函数
    /// </summary>
    /// <param name="order">中间件执行顺序</param>
    /// <param name="tMiddlewares">事件中间件类型数组</param>
    /// <exception cref="ArgumentNullException">当中间件类型为空时抛出</exception>
    public EventInterceptorAttribute(int order, params Type[] tMiddlewares)
    {
        Order = order;
        TMiddlewares = tMiddlewares ?? throw new ArgumentNullException(nameof(tMiddlewares));
    }

    /// <summary>
    /// 构建事件处理委托链（管道模式）
    /// </summary>
    /// <param name="builder">事件处理器构建器</param>
    /// <param name="serviceProvider">依赖注入容器</param>
    /// <returns>配置完成的事件处理器构建器</returns>
    /// <remarks>
    /// 1. 通过反射创建中间件实例
    /// 2. 使用委托链实现中间件的流水线处理
    /// 3. 支持依赖注入容器的实例解析
    /// </remarks>
    public EventHandlerBuilder UseEventHandlerDelegate(
        EventHandlerBuilder builder,
        IServiceProvider serviceProvider)
    {
        foreach (var TMiddleware in TMiddlewares)
        {
            // 创建中间件委托链
            Func<EventHandlerDelegate, EventHandlerDelegate> delegates = next =>
            {
                // 通过 DI 容器创建中间件实例（支持构造函数注入）
                var middleware = ActivatorUtilities.CreateInstance(
                    serviceProvider,
                    TMiddleware,
                    next) as IEventInterceptor;

                // 构建处理委托，执行中间件的异步处理方法
                return async context =>
                {
                    await middleware.ExcuteAsync(context);
                };
            };

            // 将中间件添加到处理器管道
            builder.Use(delegates);
        }
        return builder;
    }
}
