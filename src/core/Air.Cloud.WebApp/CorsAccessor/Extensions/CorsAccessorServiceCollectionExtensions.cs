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
using Air.Cloud.Core.App;
using Air.Cloud.Core.Extensions;
using Air.Cloud.WebApp.CorsAccessor.Options;

using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Air.Cloud.WebApp.CorsAccessor.Extensions;

/// <summary>
/// 跨域访问服务注册扩展。
/// </summary>
[IgnoreScanning]
public static class CorsAccessorServiceCollectionExtensions
{
    /// <summary>
    /// 默认暴露给客户端读取的 Token 响应头。
    /// </summary>
    private static readonly string[] DefaultExposedHeaders =
    [
        "access-token",
        "x-access-token"
    ];

    /// <summary>
    /// 注册跨域策略、跨域配置选项和响应缓存服务。
    /// </summary>
    /// <param name="services">服务集合。</param>
    /// <param name="corsOptionsHandler">CORS 全局选项后置配置。</param>
    /// <param name="corsPolicyBuilderHandler">CORS 策略构建器后置配置。</param>
    /// <returns>服务集合。</returns>
    public static IServiceCollection AddCorsAccessor(
        this IServiceCollection services,
        Action<CorsOptions> corsOptionsHandler = default,
        Action<CorsPolicyBuilder> corsPolicyBuilderHandler = default)
    {
        services.AddConfigurableOptions<CorsAccessorSettingsOptions>();

        var corsAccessorSettings = AppConfiguration.GetConfig<CorsAccessorSettingsOptions>("CorsAccessorSettings", true);
        if (corsAccessorSettings.EnableResponseCaching == true)
        {
            services.AddResponseCaching();
        }

        services.AddCors(options =>
        {
            options.AddPolicy(corsAccessorSettings.PolicyName, builder =>
            {
                ConfigureOrigins(builder, corsAccessorSettings);
                ConfigureHeaders(builder, corsAccessorSettings);
                ConfigureMethods(builder, corsAccessorSettings);
                ConfigureCredentials(builder, corsAccessorSettings);
                ConfigureExposedHeaders(builder, corsAccessorSettings);
                ConfigurePreflightMaxAge(builder, corsAccessorSettings);

                corsPolicyBuilderHandler?.Invoke(builder);
            });

            corsOptionsHandler?.Invoke(options);
        });

        return services;
    }

    /// <summary>
    /// 配置允许访问的来源；未配置来源时允许所有来源。
    /// </summary>
    /// <param name="builder">CORS 策略构建器。</param>
    /// <param name="settings">CORS 配置选项。</param>
    private static void ConfigureOrigins(CorsPolicyBuilder builder, CorsAccessorSettingsOptions settings)
    {
        if (HasValues(settings.WithOrigins))
        {
            builder.WithOrigins(settings.WithOrigins)
                   .SetIsOriginAllowedToAllowWildcardSubdomains();
            return;
        }

        builder.AllowAnyOrigin();
    }

    /// <summary>
    /// 配置允许请求的 Header；未配置时允许所有 Header。
    /// </summary>
    /// <param name="builder">CORS 策略构建器。</param>
    /// <param name="settings">CORS 配置选项。</param>
    private static void ConfigureHeaders(CorsPolicyBuilder builder, CorsAccessorSettingsOptions settings)
    {
        if (HasValues(settings.WithHeaders)) builder.WithHeaders(settings.WithHeaders);
        else builder.AllowAnyHeader();
    }

    /// <summary>
    /// 配置允许请求的方法；未配置时允许所有方法。
    /// </summary>
    /// <param name="builder">CORS 策略构建器。</param>
    /// <param name="settings">CORS 配置选项。</param>
    private static void ConfigureMethods(CorsPolicyBuilder builder, CorsAccessorSettingsOptions settings)
    {
        if (HasValues(settings.WithMethods)) builder.WithMethods(settings.WithMethods);
        else builder.AllowAnyMethod();
    }

    /// <summary>
    /// 配置跨域凭据；启用凭据时必须配置明确来源。
    /// </summary>
    /// <param name="builder">CORS 策略构建器。</param>
    /// <param name="settings">CORS 配置选项。</param>
    /// <exception cref="InvalidOperationException">启用凭据但未配置明确来源时抛出。</exception>
    private static void ConfigureCredentials(CorsPolicyBuilder builder, CorsAccessorSettingsOptions settings)
    {
        if (settings.AllowCredentials != true) return;

        if (!HasValues(settings.WithOrigins))
        {
            throw new InvalidOperationException(
                $"{nameof(CorsAccessorSettingsOptions.AllowCredentials)} cannot be true when {nameof(CorsAccessorSettingsOptions.WithOrigins)} is empty.");
        }

        builder.AllowCredentials();
    }

    /// <summary>
    /// 配置允许客户端读取的响应 Header。
    /// </summary>
    /// <param name="builder">CORS 策略构建器。</param>
    /// <param name="settings">CORS 配置选项。</param>
    private static void ConfigureExposedHeaders(CorsPolicyBuilder builder, CorsAccessorSettingsOptions settings)
    {
        var exposedHeaders = GetExposedHeaders(settings);
        if (exposedHeaders.Length == 0) return;

        builder.WithExposedHeaders(exposedHeaders);
    }

    /// <summary>
    /// 配置预检请求缓存时间。
    /// </summary>
    /// <param name="builder">CORS 策略构建器。</param>
    /// <param name="settings">CORS 配置选项。</param>
    private static void ConfigurePreflightMaxAge(CorsPolicyBuilder builder, CorsAccessorSettingsOptions settings)
    {
        builder.SetPreflightMaxAge(TimeSpan.FromSeconds(settings.PreflightMaxAgeSeconds.GetValueOrDefault(24 * 60 * 60)));
    }

    /// <summary>
    /// 获取最终暴露给客户端的响应 Header 集合。
    /// </summary>
    /// <param name="settings">CORS 配置选项。</param>
    /// <returns>去重后的响应 Header 集合。</returns>
    private static string[] GetExposedHeaders(CorsAccessorSettingsOptions settings)
    {
        var defaultHeaders = settings.ExposeDefaultTokenHeaders.GetValueOrDefault(true) ? DefaultExposedHeaders : Array.Empty<string>();
        var configuredHeaders = settings.WithExposedHeaders ?? Array.Empty<string>();

        return defaultHeaders.Concat(configuredHeaders)
                             .Where(header => !string.IsNullOrWhiteSpace(header))
                             .Distinct(StringComparer.OrdinalIgnoreCase)
                             .ToArray();
    }

    /// <summary>
    /// 判断字符串数组是否包含有效配置值。
    /// </summary>
    /// <param name="values">待检查数组。</param>
    /// <returns>包含至少一个非空白字符串时返回 true。</returns>
    private static bool HasValues(string[] values)
    {
        return values?.Any(value => !string.IsNullOrWhiteSpace(value)) == true;
    }
}
