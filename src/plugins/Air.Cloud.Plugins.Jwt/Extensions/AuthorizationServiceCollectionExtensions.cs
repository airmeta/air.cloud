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
using Air.Cloud.Core.Standard.Authentication;

using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Air.Cloud.Plugins.Jwt.Extensions
{
    public static class AuthorizationServiceCollectionExtensions
    {
        /// <summary>
        /// 添加策略授权服务
        /// </summary>
        /// <typeparam name="TAuthorizationHandler">策略授权处理程序</typeparam>
        /// <param name="services">服务集合</param>
        /// <param name="configure">自定义配置</param>
        /// <param name="enableGlobalAuthorize">是否启用全局授权</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddAuthorizationPolicy<TAuthorizationHandler, TAuthorizationPolicyProvider>(this IServiceCollection services, Action<IServiceCollection> configure = null, bool enableGlobalAuthorize = false, bool IsDebugState = false)
            where TAuthorizationHandler : class, IAuthorizationHandler
            where TAuthorizationPolicyProvider : class, IAuthorizationPolicyProvider
        {
            // 注册授权策略提供器
            services.TryAddSingleton<IAuthorizationPolicyProvider, TAuthorizationPolicyProvider>();
            // 注册策略授权处理程序
            services.TryAddSingleton<IAuthorizationHandler, TAuthorizationHandler>();

            services.AddAuthorization(option =>
             {
                 option.AddPolicy("123312132", policy => policy
                        .RequireAuthenticatedUser()
                        .AddAuthenticationSchemes("123312132")
                        );
                 option.AddPolicy(ISecurityHandlerStandard.AuthenticationSchemeName, policy => policy
                         .RequireAuthenticatedUser()
                         .AddAuthenticationSchemes(ISecurityHandlerStandard.AuthenticationSchemeName)
                         );
             });
            configure?.Invoke(services);
            return services;
        }
    }
}
