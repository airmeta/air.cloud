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
using Air.Cloud.Core.App.Options;

using Microsoft.Extensions.Configuration;

using System.ComponentModel.DataAnnotations;

namespace Air.Cloud.WebApp.CorsAccessor.Options;

/// <summary>
/// 跨域访问配置选项。
/// </summary>
[ConfigurationInfo("CorsAccessorSettings")]
public sealed class CorsAccessorSettingsOptions : IConfigurableOptions<CorsAccessorSettingsOptions>
{
    /// <summary>
    /// CORS 策略名称。
    /// </summary>
    [Required]
    public string PolicyName { get; set; }

    /// <summary>
    /// 允许访问的来源域名；未配置时允许所有来源。
    /// </summary>
    public string[] WithOrigins { get; set; }

    /// <summary>
    /// 允许请求的 Header；未配置时允许所有 Header。
    /// </summary>
    public string[] WithHeaders { get; set; }

    /// <summary>
    /// 允许客户端读取的响应 Header。
    /// </summary>
    public string[] WithExposedHeaders { get; set; }

    /// <summary>
    /// 允许请求的方法；未配置时允许所有方法。
    /// </summary>
    public string[] WithMethods { get; set; }

    /// <summary>
    /// 是否允许跨域请求携带凭据。
    /// </summary>
    public bool? AllowCredentials { get; set; }

    /// <summary>
    /// 预检请求缓存秒数。
    /// </summary>
    public int? PreflightMaxAgeSeconds { get; set; }

    /// <summary>
    /// 是否默认暴露 access-token 和 x-access-token 响应头。
    /// </summary>
    public bool? ExposeDefaultTokenHeaders { get; set; }

    /// <summary>
    /// 是否启用 ASP.NET Core 响应缓存中间件。
    /// </summary>
    public bool? EnableResponseCaching { get; set; }

    /// <summary>
    /// 旧配置字段：预检请求缓存秒数。建议改用 <see cref="PreflightMaxAgeSeconds"/>。
    /// </summary>
    public int? SetPreflightMaxAge { get; set; }

    /// <summary>
    /// 旧配置字段：是否默认暴露 Token 响应头。建议改用 <see cref="ExposeDefaultTokenHeaders"/>。
    /// </summary>
    public bool? FixedClientToken { get; set; }

    /// <summary>
    /// 后置配置默认值，并兼容旧配置字段。
    /// </summary>
    /// <param name="options">跨域访问配置选项。</param>
    /// <param name="configuration">应用配置。</param>
    public void PostConfigure(CorsAccessorSettingsOptions options, IConfiguration configuration)
    {
        PolicyName = NormalizePolicyName(PolicyName);
        WithOrigins = NormalizeValues(WithOrigins);
        WithHeaders = NormalizeValues(WithHeaders);
        WithExposedHeaders = NormalizeValues(WithExposedHeaders);
        WithMethods = NormalizeValues(WithMethods);
        AllowCredentials ??= WithOrigins.Length > 0;
        PreflightMaxAgeSeconds ??= SetPreflightMaxAge ?? 24 * 60 * 60;
        ExposeDefaultTokenHeaders ??= FixedClientToken ?? true;
        EnableResponseCaching ??= true;
    }

    /// <summary>
    /// 获取有效策略名称。
    /// </summary>
    /// <param name="policyName">配置中的策略名称。</param>
    /// <returns>有效策略名称。</returns>
    private static string NormalizePolicyName(string policyName)
    {
        return string.IsNullOrWhiteSpace(policyName) ? "App.Cors.Policy" : policyName.Trim();
    }

    /// <summary>
    /// 清理字符串数组中的空白项。
    /// </summary>
    /// <param name="values">配置值数组。</param>
    /// <returns>清理后的配置值数组。</returns>
    private static string[] NormalizeValues(string[] values)
    {
        return values?.Where(value => !string.IsNullOrWhiteSpace(value))
                      .Select(value => value.Trim())
                      .ToArray()
               ?? Array.Empty<string>();
    }
}
