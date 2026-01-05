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
using Air.Cloud.Core;
using Air.Cloud.Core.Extensions;
using Air.Cloud.Core.Plugins.Router;
using Air.Cloud.Core.Standard.SkyMirror;
using Air.Cloud.Core.Standard.SkyMirror.Model;
using Air.Cloud.Modules.SkyMirrorShield.Utils;
using Air.Cloud.WebApp.UnifyResult;
using Air.Cloud.WebApp.UnifyResult.Internal;

using Microsoft.AspNetCore.Http;

using Newtonsoft.Json;

using System.Text;

namespace Air.Cloud.Modules.SkyMirrorShield.Middleware
{
    public class SkyMirrorShieldMiddleware
    {
        private readonly RequestDelegate next;

        private static IRouterMatcherPlugin routerMatcher = null;
        private readonly IHttpClientFactory httpClientFactory;
        /// <summary>
        /// 部分参数不足
        /// </summary>
        public static readonly string HEADERLOSE_ITEM = JsonConvert.SerializeObject(new RESTfulResult<string>()
        {
            Code = 403,
            Data = "",
            Extras = UnifyContext.Take(),
            Succeeded = true,
            Message = "{0}",
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        }, Formatting.Indented, new JsonSerializerSettings() { ContractResolver = new LowercaseContractResolver() });

        public static readonly string VALIDATE_TICKIT_FAIL = JsonConvert.SerializeObject(new RESTfulResult<string>()
        {
            Code = 401,
            Data = "",
            Extras = UnifyContext.Take(),
            Succeeded = true,
            Message = "{0}",
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        }, Formatting.Indented, new JsonSerializerSettings() { ContractResolver = new LowercaseContractResolver() });



        public SkyMirrorShieldMiddleware(RequestDelegate next, IHttpClientFactory httpClientFactory)
        {
            this.next = next;
            this.httpClientFactory = httpClientFactory;

        }

        public async Task InvokeAsync(HttpContext context)
        {
            //检查当前请求是否在白名单内
            string Method = context.Request.Method;
            string Path = context.Request.Path;
            if (routerMatcher == null) routerMatcher = AppRealization.AppPlugin.GetPlugin<IRouterMatcherPlugin>();
            string Key = ISkyMirrorShieldServerStandard.ServerEndpointDatas.Keys.Select(s =>
            {
                string[] Items = s.Split("|");
                return new
                {
                    Method = Items[0],
                    Path = Items[1],
                    Key= s
                };
            }).Where(s => s.Method.ToLower() == Method.ToLower() && routerMatcher.Match(s.Path, Path)).FirstOrDefault()?.Key;
            if (Key.IsNullOrEmpty())
            {
                AppRealization.Output.Print("天镜安全校验", $"请求路径：{Path}，请求方法：{Method}，未查询到匹配的路由,已拒绝此请求");

                context.Response.StatusCode = 403;
                _ = await context.Response.BodyWriter.WriteAsync(Encoding.UTF8.GetBytes(string.Format(HEADERLOSE_ITEM, "未查询到匹配的路由,已拒绝此请求")));
                return;
            }
            EndpointData? RouteMatch = ISkyMirrorShieldServerStandard.ServerEndpointDatas[Key];
            if (RouteMatch == null)
            {
                AppRealization.Output.Print("天镜安全校验", $"请求路径：{Path}，请求方法：{Method}，未查询到匹配的路由,已拒绝此请求");

                context.Response.StatusCode = 403;
                _ = await context.Response.BodyWriter.WriteAsync(Encoding.UTF8.GetBytes(string.Format(HEADERLOSE_ITEM, "未查询到匹配的路由,已拒绝此请求")));
                return;
            }
            AppRealization.Output.Print("天镜安全校验", $"请求路径：{Path}，请求方法：{Method}，是否允许匿名访问：{RouteMatch.Value.IsAllowAnonymous},是否需要身份验证：{RouteMatch.Value.RequiresAuthorization}");

            bool IsAllowAnonymous = RouteMatch.Value.IsAllowAnonymous;
            if (IsAllowAnonymous)
            {
                await next(context); // 继续处理请求
                return;
            }
            bool IsRequiresAuthorization = RouteMatch.Value.RequiresAuthorization;
            if (!IsRequiresAuthorization)
            {
                await next(context); // 继续处理请求
                return;
            }
            string Tickit = context.Request.Headers["Tickit"];
            if (Tickit.IsNullOrEmpty())
            {
                context.Response.StatusCode = 401;
                _ = await context.Response.BodyWriter.WriteAsync(Encoding.UTF8.GetBytes(string.Format(VALIDATE_TICKIT_FAIL, "未提供身份验证凭据,已拒绝此请求")));
                return;
            }
            string ClientId = context.Request.Headers["ClientUUID"];
            var verifyResult = await SkyMirrorShieldUtil.VerifyTickitAsync(httpClientFactory,Tickit, ClientId);
            if (!verifyResult)
            {
                context.Response.StatusCode = 401;
                _ = await context.Response.BodyWriter.WriteAsync(Encoding.UTF8.GetBytes(string.Format(VALIDATE_TICKIT_FAIL, "身份验证凭据无效或已过期,已拒绝此请求")));
                return;
            }
            await next(context);

        }
    }
}
