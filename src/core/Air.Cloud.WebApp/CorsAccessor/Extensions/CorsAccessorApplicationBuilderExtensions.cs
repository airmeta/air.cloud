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
using Air.Cloud.WebApp.CorsAccessor.Options;

using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Air.Cloud.WebApp.CorsAccessor.Extensions;

/// <summary>
/// 跨域访问中间件扩展。
/// </summary>
[IgnoreScanning]
public static class CorsAccessorApplicationBuilderExtensions
{
    /// <summary>
    /// 启用跨域访问中间件和响应缓存中间件。
    /// </summary>
    /// <param name="app">应用构建器。</param>
    /// <param name="corsPolicyBuilderHandler">临时 CORS 策略构建器；传入后不使用配置中的策略名称。</param>
    /// <returns>应用构建器。</returns>
    public static IApplicationBuilder UseCorsAccessor(
        this IApplicationBuilder app,
        Action<CorsPolicyBuilder> corsPolicyBuilderHandler = default)
    {
        var corsAccessorSettings = app.ApplicationServices
                                      .GetRequiredService<IOptions<CorsAccessorSettingsOptions>>()
                                      .Value;

        UseConfiguredCors(app, corsAccessorSettings, corsPolicyBuilderHandler);
        UseResponseCaching(app, corsAccessorSettings);

        return app;
    }

    /// <summary>
    /// 按配置策略名称或临时策略构建器启用 CORS。
    /// </summary>
    /// <param name="app">应用构建器。</param>
    /// <param name="corsPolicyBuilderHandler">临时 CORS 策略构建器。</param>
    private static void UseConfiguredCors(
        IApplicationBuilder app,
        CorsAccessorSettingsOptions settings,
        Action<CorsPolicyBuilder> corsPolicyBuilderHandler)
    {
        if (corsPolicyBuilderHandler != null)
        {
            app.UseCors(corsPolicyBuilderHandler);
            return;
        }

        app.UseCors(settings.PolicyName);
    }

    /// <summary>
    /// 启用响应缓存中间件；对应服务由 AddCorsAccessor 注册。
    /// </summary>
    /// <param name="app">应用构建器。</param>
    private static void UseResponseCaching(IApplicationBuilder app, CorsAccessorSettingsOptions settings)
    {
        if (settings.EnableResponseCaching != true)
        {
            return;
        }

        app.UseResponseCaching();
    }
}
