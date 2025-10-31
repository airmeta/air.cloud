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
using Air.Cloud.Core.Modules.DynamicApp.Internal;

using Microsoft.AspNetCore.Mvc.Controllers;

using System.Reflection;

namespace Air.Cloud.Core.Modules.DynamicApp.Provider;

/// <summary>
/// <para>zh-cn:动态接口控制器特性提供器</para>
/// <para>en-us: Dynamic API Controller Feature Provider</para>
/// </summary>
[IgnoreScanning]
public sealed class DynamicApiControllerFeatureProvider : ControllerFeatureProvider
{

    /// <summary>
    /// <para>zh-cn:判断是否为控制器</para>
    /// <para>en-us: Determine if it is a controller</para>
    /// </summary>
    /// <param name="typeInfo">
    ///  <para>zh-cn:类型信息</para>
    ///  <para>en-us: Type information</para>
    /// </param>
    /// <returns>
    ///  <para>zh-cn:如果是控制器则返回 true，否则返回 false</para>
    ///  <para>en-us: Returns true if it is a controller, otherwise false</para>
    /// </returns>
    protected override bool IsController(TypeInfo typeInfo)
    {
        return Penetrates.IsApiController(typeInfo);
    }
}