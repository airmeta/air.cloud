/*
 * Copyright (c) 2024 安徽三实软件科技有限公司
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 *
 * This file is provided under the Mozilla Public License Version 2.0,
 * and the "NO WARRANTY" clause of the MPL is hereby expressly
 * acknowledged.
 */
using air.gateway.Options;

using Air.Cloud.Core.App;
//using Air.Cloud.Service;
namespace air.gateway.Middleware
{

    public class AuthorizationMiddleware
    {
        private readonly AuthorizationSettings settings; 
        private readonly RequestDelegate next;
        public AuthorizationMiddleware(RequestDelegate next)
        {
            settings = AppConfigurationLoader.InnerConfiguration.GetConfig<AuthorizationSettings>("AuthorizationSettings");
            this.next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            context.Request.EnableBuffering();
            using (var str=new StreamReader(context.Request.Body))
            {
                var body = await str.ReadToEndAsync();
                context.Request.Body.Position = 0;
                context.Request.Body.Seek(0, SeekOrigin.Begin);
                if (settings.EnableAuthorizationService)
                {
                    bool IsWhiteList = Convert.ToBoolean(context.Request.Headers[WhiteListRequestMiddleware.WHITE_HEADER_KEY]);
                    if (IsWhiteList)
                    {
                        await next(context); // 继续处理请求
                        return;
                    }
                    string? WhiteHeader = context.Request.Headers[settings.WhiteHeader];
                    if (WhiteHeader != null)
                    {

                        context.Response.Headers.Add("Authorization", context.Request.Headers["Authorization"]);
                        context.Response.Headers.Add("X-Authorization", context.Request.Headers["X-Authorization"]);
                        //验证通过
                        await next(context); // 继续处理请求
                        return;
                    }
                     await next(context); 
                }
                else
                {
                    await next(context); // 继续处理请求
                }
            }
        }
    }
}
