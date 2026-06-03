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
using Air.Cloud.WebApp.FriendlyException.Attributes;

using System.Reflection;

namespace Air.Cloud.WebApp.FriendlyException.Internal;

/// <summary>
/// 方法级异常覆盖元数据。
/// </summary>
internal sealed class MethodIfException
{
    /// <summary>
    /// 空方法级异常覆盖元数据。
    /// </summary>
    public static readonly MethodIfException Empty = new()
    {
        IfExceptionAttributes = Array.Empty<IfExceptionAttribute>()
    };

    /// <summary>
    /// 出现异常的方法。
    /// </summary>
    public MethodBase ErrorMethod { get; set; }

    /// <summary>
    /// 方法堆栈上声明的异常覆盖特性集合。
    /// </summary>
    public IEnumerable<IfExceptionAttribute> IfExceptionAttributes { get; set; } = Array.Empty<IfExceptionAttribute>();
}
