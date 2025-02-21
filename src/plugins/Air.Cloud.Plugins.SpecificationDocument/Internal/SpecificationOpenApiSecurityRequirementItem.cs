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
using Microsoft.OpenApi.Models;

namespace Air.Cloud.Plugins.SpecificationDocument.Internal;

/// <summary>
/// 安全定义需求子项
/// </summary>
[IgnoreScanning]
public sealed class SpecificationOpenApiSecurityRequirementItem
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public SpecificationOpenApiSecurityRequirementItem()
    {
        Accesses = Array.Empty<string>();
    }

    /// <summary>
    /// 安全Schema
    /// </summary>
    public OpenApiSecurityScheme Scheme { get; set; }

    /// <summary>
    /// 权限
    /// </summary>
    public string[] Accesses { get; set; }
}