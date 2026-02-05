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
using Air.Cloud.Core.App;
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
    public class SkyMirrorShieldMiddlewareRe
    {
        private readonly RequestDelegate next;

        private static IRouterMatcherPlugin routerMatcher = null;
        private readonly IHttpClientFactory httpClientFactory;

        /// <summary>
        /// <para>zh-cn:获取响应结果</para>
        /// <para>en-us:Get response result</para>
        /// </summary>
        /// <param name="Code"></param>
        /// <param name="Message"></param>
        /// <returns></returns>
        public static string GetResponseResult(int Code, string Message)
        {
            return JsonConvert.SerializeObject(new RESTfulResult<string>()
            {
                Code = Code,
                Data = "",
                Extras = UnifyContext.Take(),
                Succeeded = true,
                Message = Message,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            }, Formatting.Indented, new JsonSerializerSettings() { ContractResolver = new LowercaseContractResolver() });

        }
        public SkyMirrorShieldMiddlewareRe(RequestDelegate next, IHttpClientFactory httpClientFactory)
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
                    Key = s
                };
            }).Where(s => s.Method.ToLower() == Method.ToLower() && routerMatcher.Match(s.Path, Path)).FirstOrDefault()?.Key;

            EndpointData? RouteMatch = null;

            #region  当没有命中存储进来的路由时,根据AppId去查询当前应用的路由许可信息
            if (Key.IsNullOrEmpty())
            {
                bool EnableRouteValid = SkyMirrorShieldUtil.EnableRouteValid;
                if (EnableRouteValid)
                {
                    string AppId = context.Request.Headers["AppId"];
                    if (!AppId.IsNullOrEmpty())
                    {
                        var appAuthRoutes = await SkyMirrorShieldUtil.GetAppAuthRoutes(httpClientFactory, AppId);
                        if (appAuthRoutes != null)
                        {
                            if (!appAuthRoutes.AppExist)
                            {
                                AppRealization.Output.Print("天镜安全校验", $"请求路径：{Path}，请求方法：{Method}，应用编号: {AppId},应用不存在,已拒绝此请求");
                                context.Response.StatusCode = 403;
                                _ = await context.Response.BodyWriter.WriteAsync(Encoding.UTF8.GetBytes(GetResponseResult(403, "应用异常,已拒绝此请求")));
                                return;
                            }
                            if (!appAuthRoutes.AppStatus.AppIsEnable)
                            {
                                AppRealization.Output.Print("天镜安全校验", $"请求路径：{Path}，请求方法：{Method}，应用编号: {AppId},应用已被禁用,已拒绝此请求");
                                context.Response.StatusCode = 403;
                                _ = await context.Response.BodyWriter.WriteAsync(Encoding.UTF8.GetBytes(GetResponseResult(403, "该应用已被冻结")));
                                return;
                            }
                            if (appAuthRoutes.AppStatus.AppIsDelete)
                            {
                                AppRealization.Output.Print("天镜安全校验", $"请求路径：{Path}，请求方法：{Method}，应用编号: {AppId},应用已被删除,已拒绝此请求");
                                context.Response.StatusCode = 403;
                                _ = await context.Response.BodyWriter.WriteAsync(Encoding.UTF8.GetBytes(GetResponseResult(403, "该应用已被删除,目标地址已不可达")));
                                return;
                            }
                            var RouteMatches = appAuthRoutes.EndpointDatas.Where(s => s.Method.ToLower() == Method.ToLower() && routerMatcher.Match(s.Path, Path));
                            //再次尝试匹配路由
                            RouteMatch = RouteMatches.Any() ? RouteMatches.First() : null;
                            if (RouteMatch == null)
                            {
                                AppRealization.Output.Print("天镜安全校验", $"请求路径：{Path}，请求方法：{Method}，应用编号: {AppId},未查询到匹配的路由,已拒绝此请求");
                                context.Response.StatusCode = 403;
                                _ = await context.Response.BodyWriter.WriteAsync(Encoding.UTF8.GetBytes(GetResponseResult(403, "未查询到匹配的路由,已拒绝此请求")));
                                return;
                            }
                        }
                        else
                        {
                            AppRealization.Output.Print("天镜安全校验", $"请求路径：{Path}，请求方法：{Method}，未查询到匹配的路由,已拒绝此请求");

                            context.Response.StatusCode = 403;
                            _ = await context.Response.BodyWriter.WriteAsync(Encoding.UTF8.GetBytes(GetResponseResult(403, "未查询到匹配的路由,已拒绝此请求")));
                            return;
                        }
                    }
                    else
                    {
                        AppRealization.Output.Print("天镜安全校验", $"请求路径：{Path}，请求方法：{Method}，未查询到匹配的路由,已拒绝此请求");
                        context.Response.StatusCode = 403;
                        _ = await context.Response.BodyWriter.WriteAsync(Encoding.UTF8.GetBytes(GetResponseResult(403, "未查询到匹配的路由,已拒绝此请求")));
                        return;
                    }
                }
            }
            else
            {
                RouteMatch = ISkyMirrorShieldServerStandard.ServerEndpointDatas[Key];
                if (RouteMatch == null)
                {
                    AppRealization.Output.Print("天镜安全校验", $"请求路径：{Path}，请求方法：{Method}，未查询到匹配的路由,已拒绝此请求");
                    context.Response.StatusCode = 403;
                    _ = await context.Response.BodyWriter.WriteAsync(Encoding.UTF8.GetBytes(GetResponseResult(403, "未查询到匹配的路由,已拒绝此请求")));
                    return;
                }
            }
            #endregion

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
            string Ticket = context.Request.Headers["Ticket"];
            if (Ticket.IsNullOrEmpty())
            {
                context.Response.StatusCode = 401;
                _ = await context.Response.BodyWriter.WriteAsync(Encoding.UTF8.GetBytes(GetResponseResult(401, "未提供身份验证凭据,已拒绝此请求")));
                return;
            }
            string ClientId = context.Request.Headers["ClientUUID"];
            var verifyResult = await SkyMirrorShieldUtil.VerifyTicketAsync(httpClientFactory, Ticket, ClientId);
            if (!verifyResult)
            {
                context.Response.StatusCode = 401;
                _ = await context.Response.BodyWriter.WriteAsync(Encoding.UTF8.GetBytes(GetResponseResult(401, "身份验证凭据无效或已过期,已拒绝此请求")));
                return;
            }
            await next(context);
        }

    }
}
