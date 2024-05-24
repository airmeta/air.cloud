
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
using Air.Cloud.Core;
using Air.Cloud.Core.App;
using Air.Cloud.Core.Enums;
using Air.Cloud.Core.Standard.Authentication.Jwt;
using Air.Cloud.Plugins.Jwt;

using Microsoft.AspNetCore.Authorization;

namespace unit.webapp.common.JwtHandler
{
    public class AppJwtHandler : IAuthorizationHandler
    {
        public async Task HandleAsync(AuthorizationHandlerContext context)
        {
            //如果开发环境则不检查授权
            if (AppEnvironment.VirtualEnvironment == EnvironmentEnums.Development)
            {
                await AppRealization.Jwt.AuthorizeHandleAsync(context); return;
            }
            //检查授权
            var Result = JWTEncryption.ValidateToken(context, context.GetCurrentHttpContext());

            if (Result)
            {
                //授权成功
                await AppRealization.Jwt.AuthorizeHandleAsync(context); return;
            }
            //授权失败
            await AppRealization.Jwt.UnAuthorizeHandleAsync(context);
        }
    }
}
