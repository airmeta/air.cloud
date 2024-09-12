/*
 * Copyright (c) 2024 星曳数据
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
/// 规范化稳定安全配置
/// </summary>
public sealed class SpecificationOpenApiSecurityScheme : OpenApiSecurityScheme
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public SpecificationOpenApiSecurityScheme()
    {
    }

    /// <summary>
    /// 唯一Id
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// 安全需求
    /// </summary>
    public SpecificationOpenApiSecurityRequirementItem Requirement { get; set; }
}