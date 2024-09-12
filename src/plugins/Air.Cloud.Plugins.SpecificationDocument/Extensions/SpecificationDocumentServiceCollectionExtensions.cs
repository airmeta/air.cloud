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

using Air.Cloud.Core.Extensions;
using Air.Cloud.Plugins.SpecificationDocument.Builders;
using Air.Cloud.Plugins.SpecificationDocument.Extensions.Options;
using Air.Cloud.Plugins.SpecificationDocument.Options;

namespace Air.Cloud.Plugins.SpecificationDocument.Extensions;

/// <summary>
/// 规范化接口服务拓展类
/// </summary>
[IgnoreScanning]
public static class SpecificationDocumentServiceCollectionExtensions
{
    /// <summary>
    /// 添加规范化文档服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configure">自定义配置</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddSpecificationDocuments(this IServiceCollection services, Action<SpecificationDocumentServiceOptions> configure = default)
    {
        // 判断是否启用规范化文档
        if (AppCore.Settings.InjectSpecificationDocument != true) return services;

        // 添加配置
        services.AddConfigurableOptions<SpecificationDocumentSettingsOptions>();

        // 载入服务配置选项
        var configureOptions = new SpecificationDocumentServiceOptions();
        configure?.Invoke(configureOptions);

        // 添加Swagger生成器服务
        services.AddSwaggerGen(options => SpecificationDocumentBuilder.BuildGen(options, configureOptions?.SwaggerGenConfigure));

        return services;
    }
}