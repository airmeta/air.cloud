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

using Air.Cloud.Plugins.SpecificationDocument.Builders;
using Air.Cloud.Plugins.SpecificationDocument.Extensions.Options;
using Air.Cloud.Plugins.SpecificationDocument.Options;

namespace Air.Cloud.Plugins.SpecificationDocument.Extensions;

/// <summary>
/// 规范化文档中间件拓展
/// </summary>
[IgnoreScanning]
public static class SpecificationDocumentApplicationBuilderExtensions
{
    /// <summary>
    /// 注入Swagger文档插件
    /// </summary>
    /// <param name="app"></param>
    /// <param name="routePrefix">空字符串将为首页</param>
    /// <param name="configure"></param>
    /// <returns></returns>
    public static IApplicationBuilder UseSwaggerDocumentPlugin(this IApplicationBuilder app, string routePrefix = default, Action<SpecificationDocumentInjectConfigureOptions> configure = null)
    {
        // 载入中间件配置选项
        var configureOptions = new SpecificationDocumentInjectConfigureOptions();
        configure?.Invoke(configureOptions);
        // 载入服务配置选项
        var specificationDocumentConfigureOptions = new SpecificationDocumentConfigureOptions();
        configureOptions?.SpecificationDocumentConfigure?.Invoke(specificationDocumentConfigureOptions);

        // 配置 Swagger 全局参数
        app.UseSwagger(options => SpecificationDocumentBuilder.Build(options, specificationDocumentConfigureOptions?.SwaggerConfigure));

        // 配置 Swagger UI 参数
        app.UseSwaggerUI(options => SpecificationDocumentBuilder.BuildUI(options, routePrefix, specificationDocumentConfigureOptions?.SwaggerUIConfigure));

        return app;
    }
}