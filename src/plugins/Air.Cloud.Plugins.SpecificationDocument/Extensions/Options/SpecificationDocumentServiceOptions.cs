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


using Swashbuckle.AspNetCore.SwaggerGen;

namespace Air.Cloud.Plugins.SpecificationDocument.Extensions.Options;

/// <summary>
/// 规范化文档服务配置选项
/// </summary>
public sealed class SpecificationDocumentServiceOptions
{
    /// <summary>
    /// Swagger 生成器配置
    /// </summary>
    public Action<SwaggerGenOptions> SwaggerGenConfigure { get; set; }
}