﻿/*
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
using Air.Cloud.Core.Dependencies.Enums;

namespace Air.Cloud.WebApp.Dependencies.Internal;

/// <summary>
/// 外部注册类型模型
/// </summary>
[IgnoreScanning]
public sealed class ExternalService
{
    /// <summary>
    /// 接口类型，格式："程序集名称;接口完整名称"
    /// </summary>
    public string Interface { get; set; }

    /// <summary>
    /// 实例类型，格式："程序集名称;接口完整名称"
    /// </summary>
    public string Service { get; set; }

    /// <summary>
    /// 注册类型
    /// </summary>
    public RegisterType RegisterType { get; set; }

    /// <summary>
    /// 添加服务方式，存在不添加，或继续添加
    /// </summary>
    public InjectionActions Action { get; set; } = InjectionActions.Add;

    /// <summary>
    /// 注册选项
    /// </summary>
    public InjectionPatterns Pattern { get; set; } = InjectionPatterns.All;

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
    /// 代理类型，格式："程序集名称;接口完整名称"
    /// </summary>
    public string Proxy { get; set; }
}