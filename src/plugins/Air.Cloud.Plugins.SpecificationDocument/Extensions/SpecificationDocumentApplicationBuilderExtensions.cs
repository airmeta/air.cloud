// Copyright (c) 2020-2022 百小僧, Baiqian Co.,Ltd.
// Furion is licensed under Mulan PSL v2.
// You can use this software according to the terms and conditions of the Mulan PSL v2.
// You may obtain a copy of Mulan PSL v2 at:
//             https://gitee.com/dotnetchina/Furion/blob/master/LICENSE
// THIS SOFTWARE IS PROVIDED ON AN "AS IS" BASIS, WITHOUT WARRANTIES OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO NON-INFRINGEMENT, MERCHANTABILITY OR FIT FOR A PARTICULAR PURPOSE.
// See the Mulan PSL v2 for more details.

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

        // 判断是否启用规范化文档
        if (AppCore.Settings.InjectSpecificationDocument != true) return app;

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