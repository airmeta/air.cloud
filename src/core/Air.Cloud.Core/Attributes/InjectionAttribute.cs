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
using Air.Cloud.Core.Standard.DynamicServer.Enums;

namespace Air.Cloud.Core.Attributes;

/// <summary>
/// 设置依赖注入方式
/// </summary>
[IgnoreScanning, AttributeUsage(AttributeTargets.Class)]
public class InjectionAttribute : Attribute
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="exceptInterfaces"></param>
    public InjectionAttribute(params Type[] exceptInterfaces)
        : this(InjectionActions.Add, exceptInterfaces)
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="action"></param>
    /// <param name="exceptInterfaces"></param>
    public InjectionAttribute(InjectionActions action, params Type[] exceptInterfaces)
    {
        Action = action;
        Pattern = InjectionPatterns.All;
        ExceptInterfaces = exceptInterfaces ?? Array.Empty<Type>();
        Order = 0;
    }

    /// <summary>
    /// 添加服务方式，存在不添加，或继续添加
    /// </summary>
    public InjectionActions Action { get; set; }

    /// <summary>
    /// 注册选项
    /// </summary>
    public InjectionPatterns Pattern { get; set; }

    /// <summary>
    /// 注册别名
    /// </summary>
    /// <remarks>多服务时使用</remarks>
    public string Named { get; set; }

    /// <summary>
    /// 排序，排序越大，则在后面注册
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// 排除接口
    /// </summary>
    public Type[] ExceptInterfaces { get; set; }

    /// <summary>
    /// 代理类型，必须继承 DispatchProxy、IDispatchProxy
    /// </summary>
    public Type Proxy { get; set; }
}