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
using Air.Cloud.Modules.SkyMirrorShield.Options;
using Air.Cloud.WebApp.UnifyResult.Internal;

using Microsoft.Extensions.Configuration;

namespace Air.Cloud.Modules.SkyMirrorShield.Utils
{
    public static class SkyMirrorShieldUtil
    {
        private const string RequestLogSectionName = "SkyMirrorShieldSettings:RequestLog";
        private static readonly TimeSpan RequestLogOptionsRefreshInterval = TimeSpan.FromSeconds(5);
        private static readonly object RequestLogOptionsLock = new();
        private static SkyMirrorShieldRequestLogOptions cachedRequestLogOptions = new();
        private static long requestLogOptionsReadTicks;
        private static long allowedRequestLogSequence;

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
        /// <para>zh-cn:读取天镜盾请求运行日志配置。未配置时返回兼容默认值；传入配置对象时从该对象读取，否则优先读取运行期配置，再回落到内部 appsettings 配置。</para>
        /// <para>en-us:Reads SkyMirrorShield request runtime log options. When no configuration is present, backward-compatible defaults are returned; when a configuration object is supplied it is used directly, otherwise runtime configuration is read first and internal appsettings configuration is used as fallback.</para>
        /// </summary>
        /// <param name="configuration">
        /// <para>zh-cn:可选配置对象，主要用于测试或显式指定读取来源。</para>
        /// <para>en-us:Optional configuration object, mainly used by tests or callers that need to specify the configuration source explicitly.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:标准化后的请求运行日志配置。</para>
        /// <para>en-us:The normalized request runtime log options.</para>
        /// </returns>
        public static SkyMirrorShieldRequestLogOptions ReadRequestLogOptions(IConfiguration configuration = null)
        {
            var options = new SkyMirrorShieldRequestLogOptions();

            if (TryReadBooleanConfiguration($"{RequestLogSectionName}:Enabled", configuration, out var enabled))
            {
                options.Enabled = enabled;
            }

            if (TryReadBooleanConfiguration($"{RequestLogSectionName}:LogAllowedRequests", configuration, out var logAllowedRequests))
            {
                options.LogAllowedRequests = logAllowedRequests;
            }

            if (TryReadBooleanConfiguration($"{RequestLogSectionName}:LogRejectedRequests", configuration, out var logRejectedRequests))
            {
                options.LogRejectedRequests = logRejectedRequests;
            }

            var sampleRateValue = ReadConfigurationValue($"{RequestLogSectionName}:SampleRate", configuration);
            if (int.TryParse(sampleRateValue, out var sampleRate))
            {
                options.SampleRate = sampleRate;
            }

            options.Normalize();
            return options;
        }

        /// <summary>
        /// <para>zh-cn:判断当前已放行请求是否需要输出天镜盾运行日志。默认读取缓存配置；显式传入配置时直接使用该配置，适合测试采样和边界值。</para>
        /// <para>en-us:Determines whether the current allowed request should write a SkyMirrorShield runtime log. Cached configuration is used by default; explicitly supplied options are used directly, which is useful for sampling and boundary tests.</para>
        /// </summary>
        /// <param name="options">
        /// <para>zh-cn:可选请求日志配置；为空时从运行期配置缓存读取。</para>
        /// <para>en-us:Optional request log options; when null, the runtime configuration cache is used.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:需要输出放行日志时返回 `true`；否则返回 `false`。</para>
        /// <para>en-us:Returns `true` when an allowed-request log should be written; otherwise returns `false`.</para>
        /// </returns>
        public static bool ShouldLogAllowedRequest(SkyMirrorShieldRequestLogOptions options = null)
        {
            options ??= GetCachedRequestLogOptions();
            options.Normalize();

            if (!options.Enabled)
            {
                return false;
            }

            if (options.LogAllowedRequests)
            {
                return true;
            }

            return options.SampleRate > 0 && Interlocked.Increment(ref allowedRequestLogSequence) % options.SampleRate == 0;
        }

        /// <summary>
        /// <para>zh-cn:判断当前被拒绝请求是否需要输出天镜盾运行日志。该判断只控制 SkyMirrorShield 直接输出，不影响响应状态码和 TraceLog 审计。</para>
        /// <para>en-us:Determines whether the current rejected request should write a SkyMirrorShield runtime log. This only controls direct SkyMirrorShield output and does not affect response status codes or TraceLog auditing.</para>
        /// </summary>
        /// <param name="options">
        /// <para>zh-cn:可选请求日志配置；为空时从运行期配置缓存读取。</para>
        /// <para>en-us:Optional request log options; when null, the runtime configuration cache is used.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:需要输出拒绝日志时返回 `true`；否则返回 `false`。</para>
        /// <para>en-us:Returns `true` when a rejected-request log should be written; otherwise returns `false`.</para>
        /// </returns>
        public static bool ShouldLogRejectedRequest(SkyMirrorShieldRequestLogOptions options = null)
        {
            options ??= GetCachedRequestLogOptions();
            options.Normalize();

            return options.Enabled && options.LogRejectedRequests;
        }

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
                            if (ShouldLogAllowedRequest())
                            {
                                AppRealization.Output.Print("天镜安全校验", $"获取应用路由许可信息成功;路由查询地址: {RouteValidUrl},应用编号:{AppId},路由信息:{Result}", Core.Modules.AppPrint.AppPrintLevel.Debug);
                            }
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

        private static SkyMirrorShieldRequestLogOptions GetCachedRequestLogOptions()
        {
            var nowTicks = DateTimeOffset.UtcNow.UtcTicks;
            var lastReadTicks = Volatile.Read(ref requestLogOptionsReadTicks);
            if (nowTicks - lastReadTicks <= RequestLogOptionsRefreshInterval.Ticks)
            {
                return Volatile.Read(ref cachedRequestLogOptions);
            }

            lock (RequestLogOptionsLock)
            {
                lastReadTicks = Volatile.Read(ref requestLogOptionsReadTicks);
                if (nowTicks - lastReadTicks <= RequestLogOptionsRefreshInterval.Ticks)
                {
                    return cachedRequestLogOptions;
                }

                cachedRequestLogOptions = ReadRequestLogOptions();
                Volatile.Write(ref requestLogOptionsReadTicks, nowTicks);
                return cachedRequestLogOptions;
            }
        }

        private static bool TryReadBooleanConfiguration(string key, IConfiguration configuration, out bool value)
        {
            var configValue = ReadConfigurationValue(key, configuration);
            return bool.TryParse(configValue, out value);
        }

        private static string ReadConfigurationValue(string key, IConfiguration configuration)
        {
            if (configuration != null)
            {
                return configuration[key];
            }

            var runtimeValue = AppCore.Configuration?[key];
            return !string.IsNullOrWhiteSpace(runtimeValue)
                ? runtimeValue
                : AppConfigurationLoader.InnerConfiguration?[key];
        }




    }
}
