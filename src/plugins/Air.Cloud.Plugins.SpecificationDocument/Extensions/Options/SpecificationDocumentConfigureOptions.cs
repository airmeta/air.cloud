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

using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace Air.Cloud.Plugins.SpecificationDocument.Extensions.Options;

/// <summary>
/// 规范化结果中间件配置选项
/// </summary>
public sealed class SpecificationDocumentConfigureOptions
{
    /// <summary>
    /// Swagger 配置
    /// </summary>
    public Action<SwaggerOptions> SwaggerConfigure { get; set; }

    /// <summary>
    /// Swagger UI 配置
    /// </summary>
    public Action<SwaggerUIOptions> SwaggerUIConfigure { get; set; }
}