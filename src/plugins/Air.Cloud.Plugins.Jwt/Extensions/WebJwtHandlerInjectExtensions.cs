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
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Air.Cloud.Plugins.Jwt.Extensions
{
    /// <summary>
    /// Web Jwt Handler 注入拓展
    /// </summary>
    public static class WebJwtHandlerInjectExtensions
    {
        /// <summary>
        /// Web Jwt Handler 注入
        /// </summary>
        /// <param name="services"></param>
        public static void WebJwtHandlerInject<TAuthorizationHandler>(this IServiceCollection services, Action<AuthenticationOptions> authenticationConfigure = null, object tokenValidationParameters = default, Action<JwtBearerOptions> jwtBearerConfigure = null, bool enableGlobalAuthorize = false) where TAuthorizationHandler : class, IAuthorizationHandler, new()
        {
            services.AddJwt<TAuthorizationHandler>(authenticationConfigure, tokenValidationParameters, jwtBearerConfigure, enableGlobalAuthorize);
        }

    }
}
