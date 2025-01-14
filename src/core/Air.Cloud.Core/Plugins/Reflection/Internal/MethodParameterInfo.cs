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

namespace Air.Cloud.Core.Plugins.Reflection.Internal;

/// <summary>
/// 方法参数信息
/// </summary>
public class MethodParameterInfo
{
    /// <summary>
    /// 参数
    /// </summary>
    public ParameterInfo Parameter { get; set; }

    /// <summary>
    /// 参数名
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// 参数值
    /// </summary>
    public object Value { get; set; }
}