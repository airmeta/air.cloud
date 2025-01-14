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

using System.Reflection;

namespace Air.Cloud.Core.Plugins.Reflection.Proxies;

/// <summary>
/// 异步代理分发类
/// </summary>
public abstract class AspectDispatchProxy
{
    /// <summary>
    /// 创建代理
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TProxy"></typeparam>
    /// <returns></returns>
    public static T Create<T, TProxy>() where TProxy : AspectDispatchProxy
    {
        return (T)AspectDispatchProxyGenerator.CreateProxyInstance(typeof(TProxy), typeof(T));
    }

    /// <summary>
    /// 执行同步代理
    /// </summary>
    /// <param name="method"></param>
    /// <param name="args"></param>
    /// <returns></returns>
    public abstract object Invoke(MethodInfo method, object[] args);

    /// <summary>
    /// 执行异步代理
    /// </summary>
    /// <param name="method"></param>
    /// <param name="args"></param>
    /// <returns></returns>
    public abstract Task InvokeAsync(MethodInfo method, object[] args);

    /// <summary>
    /// 执行异步返回 Task{T} 代理
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="method"></param>
    /// <param name="args"></param>
    /// <returns></returns>
    public abstract Task<T> InvokeAsyncT<T>(MethodInfo method, object[] args);
}