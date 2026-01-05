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
using air.gateway.Modules.WhiteRouteLoader;

namespace air.gateway.Middleware
{
    /// <summary>
    /// 白名单中间件
    /// </summary>
    public class WhiteListRequestMiddleware
    {
        private readonly RequestDelegate next;
        private List<string> WhiteListJSON = new List<string>();
        public const string WHITE_HEADER_KEY = "WHITE_LIST_REQUEST";
        public WhiteListRequestMiddleware(RequestDelegate next)
        {
            this.next = next;
        }
        public async Task InvokeAsync(HttpContext context)
        {
            if (!WhiteListJSON.Any()) WhiteListJSON = await new AirWhiteRouteLoader().GetWhiteRouteLoaderAsync();

            if (context.Request.Headers.ContainsKey(WHITE_HEADER_KEY))
            {
                await next(context);
                return;
            }
            string Path = context.Request.Path;
            if (WhiteListJSON.Contains(Path) || Path == "/")
            {
                context.Request.Headers.Add(WHITE_HEADER_KEY, "true");
                await next(context); // 继续处理请求
                return;
            }
            bool IsWhiteList = false;
            foreach (var item in WhiteListJSON)
            {
                if (Path.StartsWith(item) || Path.EndsWith(item))
                {
                    IsWhiteList = true;
                    break;
                }
            }
            context.Request.Headers.Add(WHITE_HEADER_KEY, IsWhiteList.ToString());
            await next(context); // 继续处理请求
        }
    }
}
