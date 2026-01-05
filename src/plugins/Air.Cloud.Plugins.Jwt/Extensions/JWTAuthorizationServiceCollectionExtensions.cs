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
using Air.Cloud.Core.Enums;
using Air.Cloud.Plugins.Jwt.Options;
using Air.Cloud.Plugins.Jwt.Provider;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Air.Cloud.Plugins.Jwt.Extensions;

/// <summary>
/// JWT 授权服务拓展类
/// </summary>
public static class JWTAuthorizationServiceCollectionExtensions
{
    /// <summary>
    /// 添加 JWT 授权
    /// </summary>
    /// <typeparam name="TAuthorizationHandler"></typeparam>
    /// <param name="services"></param>
    /// <param name="authenticationConfigure"></param>
    /// <param name="tokenValidationParameters"></param>
    /// <param name="jwtBearerConfigure"></param>
    /// <param name="enableGlobalAuthorize"></param>
    /// <returns></returns>
    public static AuthenticationBuilder AddJwt<TAuthorizationHandler>(this IServiceCollection services, Action<AuthenticationOptions> authenticationConfigure = null, object tokenValidationParameters = default, Action<JwtBearerOptions> jwtBearerConfigure = null, bool enableGlobalAuthorize = false)
        where TAuthorizationHandler : class, IAuthorizationHandler
    {
        services.AddAuthorizationPolicy<TAuthorizationHandler, AppAuthorizationPolicyProvider>((options) =>
        {

        }, enableGlobalAuthorize, AppEnvironment.VirtualEnvironment == EnvironmentEnums.Development);
       
        var authenticationBuilder=services.AddAuthentication(options =>
        {
            //options.AddScheme<InternalAuthenticationHandler>(ISecurityHandlerStandard.AuthenticationSchemeName, ISecurityHandlerStandard.AuthenticationSchemeName);
            //options.AddScheme<InternalAuthenticationHandler>("123312132", "123312132");
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            // 添加自定义配置
            authenticationConfigure?.Invoke(options);
        });


        // 添加授权
        authenticationBuilder.AddJwtBearer(options =>
        {
            // 反射获取全局配置
            var jwtSettings = AppCore.GetOptions<JWTSettingsOptions>();
            // 配置 JWT 验证信息
            options.TokenValidationParameters = tokenValidationParameters as TokenValidationParameters ?? JWTEncryption.CreateTokenValidationParameters(jwtSettings);
            // 添加自定义配置
            jwtBearerConfigure?.Invoke(options);
        });

        services.Configure<MvcOptions>(options =>
        {
            options.Filters.Add(new AuthorizeFilter());
        });

        return authenticationBuilder;
    }
}