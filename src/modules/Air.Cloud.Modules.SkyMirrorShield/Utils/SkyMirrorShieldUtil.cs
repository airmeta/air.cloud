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
using air.security.common.Dtos.RequestValidDtos;

using Air.Cloud.Core;
using Air.Cloud.Core.App;
using Air.Cloud.Modules.SkyMirrorShield.Model;
using Air.Cloud.WebApp.UnifyResult.Internal;

namespace Air.Cloud.Modules.SkyMirrorShield.Utils
{
    public static class SkyMirrorShieldUtil
    {
        /// <summary>
        /// <para
        /// </summary>
        public static bool Debugger()
        {
            if (AppEnvironment.IsDevelopment)
            {
                return true;
            }
            string DebuggerStr = AppConfigurationLoader.InnerConfiguration["SkyMirrorShieldSettings:Debugger"];
            if (DebuggerStr == null) { return false; }
            return Convert.ToBoolean(DebuggerStr);
        }

        /// <summary>
        /// <para>zh-cn:是否启用票据校验</para>
        /// <para>en-us:Is ticket verification enabled</para>
        /// </summary>
        public  static bool EnableTicketValid => Convert.ToBoolean(AppConfigurationLoader.InnerConfiguration["SkyMirrorShieldSettings:TicketCheck:EnableTicketCheck"]);

        /// <summary>
        /// <para>zh-cn:票据验证地址</para>
        /// <para>en-us:Ticket validation URL</para>
        /// </summary>
        private  static string TicketValidUrl => AppConfigurationLoader.InnerConfiguration["SkyMirrorShieldSettings:TicketCheck:TicketValidUrl"];

        /// <summary>
        /// <para
        /// </summary>
        public static bool EnableRouteValid => Convert.ToBoolean(AppConfigurationLoader.InnerConfiguration["SkyMirrorShieldSettings:AppRouteCheck:EnableAppRouteAuthCheck"]);

        /// <summary>
        /// 路由验证地址
        /// </summary>
        private static string RouteValidUrl => AppConfigurationLoader.InnerConfiguration["SkyMirrorShieldSettings:AppRouteCheck:RouteQueryUrl"];

        /// <summary>
        /// <para>zh-cn:验证票据的有效性</para>
        /// <para>en-us:Verify the validity of the ticket</para>
        /// </summary>
        /// <param name="httpClientFactory"></param>
        /// <param name="Ticket"></param>
        /// <param name="ClientId"></param>
        /// <returns></returns>
        public static async Task<bool> VerifyTicketAsync(IHttpClientFactory httpClientFactory, string Ticket,string ClientId=null)
        {
            try
            {
                if (string.IsNullOrEmpty(ClientId))
                {
                    ClientId = "0";
                }
                if (string.IsNullOrEmpty(TicketValidUrl))
                {
                    AppRealization.Output.Print("天镜安全校验", "未配置票据验证地址，默认验证失败");
                    return false;
                }
                using (HttpClient client = httpClientFactory.CreateClient())
                {
                    client.Timeout = new TimeSpan(0, 3, 0);

                    var result = await client.GetAsync(TicketValidUrl.Replace("{Ticket}", Ticket).Replace("{ClientId}", ClientId));

                    if (result.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        string Result = await result.Content.ReadAsStringAsync();
                        var data = AppRealization.JSON.Deserialize<RESTfulResult<RequestValidResult>>(Result);
                        return data.Data.Valid;
                    }
                    return false;
                }
            }
            catch (Exception)
            {
                AppRealization.Output.Print("天镜安全校验", $"验证票据时发生异常，默认验证失败;校验地址: {TicketValidUrl},票据信息:{Ticket},客户端编号:{ClientId}");
                return false;
            }
           
        }

        /// <summary>
        ///  <para>zh-cn:根据AppId获取应用的路由许可信息</para>
        ///  <para>en-us:Get application route permission information based on AppId</para>
        /// </summary>
        /// <param name="httpClientFactory"></param>
        /// <param name="AppId"></param>
        /// <returns></returns>
        public static async Task<AppAuthResult> GetAppAuthRoutes(IHttpClientFactory httpClientFactory,string AppId)
        {
            try
            {
                var Data = AppRealization.Cache.GetCache<AppAuthResult>($"SkyMirrorShield_AppAuthRoutes_{AppId}");
                if (Data == null)
                {
                    //加载当前应用的路由信息
                    using (HttpClient client = httpClientFactory.CreateClient())
                    {
                        client.Timeout = new TimeSpan(0, 3, 0);
                        var result = await client.GetAsync(RouteValidUrl.Replace("{AppId}", AppId));

                        if (result.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            string Result = await result.Content.ReadAsStringAsync();
                            AppRealization.Output.Print("天镜安全校验", $"获取应用路由许可信息成功;路由查询地址: {RouteValidUrl},应用编号:{AppId},路由信息:{Result}",Core.Modules.AppPrint.AppPrintLevel.Debug);
                            var data = AppRealization.JSON.Deserialize<RESTfulResult<AppAuthResult>>(Result);
                            await AppRealization.RedisCache.String.SetAsync($"SkyMirrorShield_AppAuthRoutes_{AppId}", AppRealization.JSON.Serialize(data.Data), new TimeSpan(0, 10, 0));
                            return data.Data;
                        }
                        return null;
                    }
                }
                return Data;
            }
            catch (Exception ex)
            {
                AppRealization.Output.Print("天镜安全校验", $"获取应用路由许可信息时发生异常:{ex.Message}，默认返回空;路由查询地址: {RouteValidUrl},应用编号:{AppId}");
                return null;
            }
           
        }




    }
}
