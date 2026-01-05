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
using Air.Cloud.Core.Extensions;
namespace air.gateway.Middleware
{
    public class IPMiddleware
    {
        private readonly RequestDelegate next;

        public IPMiddleware(RequestDelegate next)
        {
            this.next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var ipFeature = context.Connection.RemoteIpAddress?.ToString();
            if (ipFeature.IsNullOrEmpty())
            {
                context.Request.Headers.Add("REMOTE_IP", ipFeature);
            }
            await next(context); // 继续处理请求
        }
    }
}
