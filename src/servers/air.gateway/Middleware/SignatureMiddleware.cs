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
using air.gateway.Const;
using air.gateway.Model;

using Air.Cloud.Core;
using Air.Cloud.Core.App;
using Air.Cloud.Core.Extensions;
using Air.Cloud.Core.Plugins.Security.MD5;
using Air.Cloud.Core.Plugins.Security.RSA;
using Air.Cloud.Core.Standard.SkyMirror.Model;
using Air.Cloud.WebApp.UnifyResult.Internal;

using Nest;

using System.Configuration;
using System.Text;

namespace air.gateway.Middleware
{
    public class SignatureMiddleware
    {
        private readonly RequestDelegate next;
        /// <summary>
        /// 路由验证白名单
        /// </summary>
        public IList<string>? RouteValidateWhiteList => (AppConfigurationLoader.InnerConfiguration["SkyMirrorShieldSettings:RouteValidateWhiteList"])?.Split(",");

        /// <summary>
        /// 查询应用
        /// </summary>
        public string? AppQueryUrl => AppConfigurationLoader.InnerConfiguration["SkyMirrorShieldSettings:SkyMirrorShieldHeaderValid:AppQueryUrl"];

        /// <summary>
        /// 应用路由查询
        /// </summary>
        public string? RouteQueryUrl => AppConfigurationLoader.InnerConfiguration["SkyMirrorShieldSettings:SkyMirrorShieldHeaderValid:RouteQueryUrl"];

        /// <summary>
        /// 票据验证
        /// </summary>
        public string? TicketValidUrl => AppConfigurationLoader.InnerConfiguration["SkyMirrorShieldSettings:SkyMirrorShieldHeaderValid:TicketValidUrl"];

        private readonly IHttpClientFactory httpClientFactory;

        public SignatureMiddleware(RequestDelegate next,IHttpClientFactory httpClientFactory)
        {
            this.next = next;
            this.httpClientFactory = httpClientFactory;

        }

        public async Task InvokeAsync(HttpContext context)
        {
            var ValidateRouteMetadata = new
            {
                Path = context.Request.Path,
                AppId = context.Request.Headers["APPID"],
                Ticket = context.Request.Headers["Ticket"],
                AppSecret = context.Request.Headers["APPSECRET"],
                TimeStamp = context.Request.Headers["TIMESTAMP"],
                Sign = context.Request.Headers["Signature"],
                Nonce = context.Request.Headers["Nonce"],
                RequestLimit = 1
            };
            if (RouteValidateWhiteList != null && RouteValidateWhiteList.Any(s => s == ValidateRouteMetadata.Path))
            {
                await next(context);
                return;
            }
            if (!context.Request.Headers["Launcher"].IsNullOrEmpty())
            {
                try
                {
                    var s = RsaEncryption.Decrypt(context.Request.Headers["Launcher"], RsaKeyConst.PUBLIC_KEY, RsaKeyConst.PRIVATE_KEY);
                    await next(context);
                    return;
                }
                catch (Exception)
                {
                    context.Response.StatusCode = 200;
                    _ = await context.Response.BodyWriter.WriteAsync(Encoding.UTF8.GetBytes(string.Format(HttpRequestErrorResultConst.HEADERLOSE_ITEM,"Launcher参数异常")));
                    return;
                }
            }
            else
            {
                if (ValidateRouteMetadata.TimeStamp.IsNullOrEmpty())
                {
                    context.Response.StatusCode = 200;
                    _ = await context.Response.BodyWriter.WriteAsync(Encoding.UTF8.GetBytes(string.Format(HttpRequestErrorResultConst.HEADERLOSE_ITEM, "TimeStamp参数缺失")));
                    return;
                }
                //校验TimeStamp与现在时间差值是否在允许范围内
                try
                {
                    Int64 begtime = Convert.ToInt64(ValidateRouteMetadata.TimeStamp) * 10000000;
                    DateTime dt_1970 = new DateTime(1970, 1, 1, 8, 0, 0);
                    long tricks_1970 = dt_1970.Ticks;//1970年1月1日刻度
                    long time_tricks = tricks_1970 + begtime;//日志日期刻度
                    DateTime dt = new DateTime(time_tricks);//转化为DateTime
                    
                    TimeSpan timeDifference = DateTime.Now - dt;
                    if (timeDifference.TotalSeconds > 300)
                    {
                        context.Response.StatusCode = 200;
                        _ = await context.Response.BodyWriter.WriteAsync(Encoding.UTF8.GetBytes(string.Format(HttpRequestErrorResultConst.HEADERLOSE_ITEM, "时间戳转换失败,请检查是否为有效时间戳(精确到秒)")));
                        //string m = $"时间戳校验失败,服务时间:{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}";
                        //string D = string.Format(HttpRequestErrorResultConst.HEADERLOSE_ITEM,m);
                        //context.Response.StatusCode = 200;
                        //_ = await context.Response.BodyWriter.WriteAsync(Encoding.UTF8.GetBytes(D));
                        return;
                    }
                }
                catch (Exception)
                {
                    context.Response.StatusCode = 200;
                    _ = await context.Response.BodyWriter.WriteAsync(Encoding.UTF8.GetBytes(string.Format(HttpRequestErrorResultConst.HEADERLOSE_ITEM, "时间戳转换失败,请检查是否为有效时间戳(精确到秒)")));
                    return;
                }
                if (ValidateRouteMetadata.AppId.IsNullOrEmpty())
                {
                    context.Response.StatusCode = 200;
                    _ = await context.Response.BodyWriter.WriteAsync(Encoding.UTF8.GetBytes(string.Format(HttpRequestErrorResultConst.HEADERLOSE_ITEM, "AppId参数缺失")));
                    return;
                }
                if (ValidateRouteMetadata.Sign.IsNullOrEmpty())
                {
                    context.Response.StatusCode = 200;
                    _ = await context.Response.BodyWriter.WriteAsync(Encoding.UTF8.GetBytes(string.Format(HttpRequestErrorResultConst.HEADERLOSE_ITEM, "Sign参数缺失")));
                    return;
                }
                if (ValidateRouteMetadata.Nonce.IsNullOrEmpty())
                {
                    context.Response.StatusCode = 200;
                    _ = await context.Response.BodyWriter.WriteAsync(Encoding.UTF8.GetBytes(string.Format(HttpRequestErrorResultConst.HEADERLOSE_ITEM, "Nonce参数缺失")));
                    return;
                }
                if (ValidateRouteMetadata.Nonce.ToString()?.Length<16)
                {
                    context.Response.StatusCode = 200;
                    _ = await context.Response.BodyWriter.WriteAsync(Encoding.UTF8.GetBytes(string.Format(HttpRequestErrorResultConst.HEADERLOSE_ITEM, "Nonce参数长度不得低于16位")));
                    return;
                }
            }
           
            #region 读取请求信息 并生成签名
            string requestData = GetRequestData(context, ValidateRouteMetadata.AppId,ValidateRouteMetadata.TimeStamp, ValidateRouteMetadata.Ticket, ValidateRouteMetadata.Nonce);
            requestData = requestData.Replace("\\u0022", "\\\"");
            string ApiCreateSign = MD5Encryption.GetMd5By32(requestData).ToUpper();
            #endregion

            #region  第三方平台 只检查平台不存在
            try
            {
                AppInformation? app = await QueryApp(ValidateRouteMetadata.AppId);
                if (app==null)
                {
                    context.Response.StatusCode = 200;
                    _ = await context.Response.BodyWriter.WriteAsync(Encoding.UTF8.GetBytes(HttpRequestErrorResultConst.APP_NOT_FOUND));
                    return;
                }
            }
            catch(ConfigurationErrorsException ex)
            {
                context.Response.StatusCode = 200;
                _ = await context.Response.BodyWriter.WriteAsync(Encoding.UTF8.GetBytes(string.Format(HttpRequestErrorResultConst.ERROR_ITEM, ex.Message)));
                return;
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 200;
                _ = await context.Response.BodyWriter.WriteAsync(Encoding.UTF8.GetBytes(HttpRequestErrorResultConst.APP_CHECK_ERROR));
                return;
            }
            #endregion
            #region 2.签名限流验证
            int Count = AppRealization.RedisCache.String.Get($"App:SignUse:{ValidateRouteMetadata.Sign}")?.ToInt()??0;
            //根据配置次数限流
            if (Count > 1)
            {
                context.Response.StatusCode = 200;
                _ = await context.Response.BodyWriter.WriteAsync(Encoding.UTF8.GetBytes(HttpRequestErrorResultConst.REREQUEST));
                return;
            }
            #endregion
            #region 3.验证签名
            if (ValidateRouteMetadata.Sign != ApiCreateSign)
            {
                context.Response.StatusCode = 200;
                _ = await context.Response.BodyWriter.WriteAsync(Encoding.UTF8.GetBytes(HttpRequestErrorResultConst.UNACCEPT));
                return;
            }
            #endregion
            _ = await AppRealization.RedisCache.String.SetAsync($"App:SignUse:{ValidateRouteMetadata.Sign}", Count + 1, new TimeSpan(0, 10, 0));
            //放行-----继续进行后续操作
            await next(context);
        }
        /// <summary>
        /// 获取当前请求的请求内容
        /// </summary>
        /// <param name="context">上下文</param>
        /// <param name="TimeStamp">时间戳</param>
        /// <param name="APPID">APPID</param>
        /// <returns></returns>
        public string GetRequestData(HttpContext context,string AppId, string TimeStamp, string Ticket,string Nonce)
        {
            string requestData = string.Empty;
            var method = context.Request.Method;
            //固定参数为三个": url,appid,timestamp 三个参数
            IDictionary<string, string> dic = new Dictionary<string, string>();
            IDictionary<string, string> keyValuePairs = new Dictionary<string, string>();
            dic.Add("APPID", AppId);
            dic.Add("URL", context.Request.Path);
            if (!Ticket.IsNullOrEmpty())
            {
                dic.Add("Ticket", Ticket);
            }
            dic.Add("NONCE", Nonce);
            switch (method.ToUpper())
            {
                case "POST":
                case "PUT":
                    /*
                     * 1. 读取POST请求体
                     * 2. 填充到dic请求参数字典中
                     */
                    if (context.Request.Body.CanSeek)
                    {
                        //是否包含文件上传
                        bool HasFile = context.Request.Headers["Content-Type"].ToString().Contains("multipart/form-data;");
                        requestData = "";
                        if (!HasFile)
                        {
                            context.Request.Body.Seek(0, SeekOrigin.Begin);
                            var reader = new StreamReader(context.Request.Body);
                            requestData = reader.ReadToEndAsync().Result;
                            context.Request.Body.Seek(0, SeekOrigin.Begin);
                        }
                    }
                    if (requestData == "{}")
                    {
                        requestData = null;
                    }
                    if (!string.IsNullOrEmpty(requestData))
                    {
                        //直接拿请求参数拼接 不做解析了
                        dic.Add("DATA", requestData);
                    }
                    break;

                case "DELETE":
                case "GET":
                    //删除和查询 直接拿到请求参数 然后组装到dic请求参数字典中
                    var queryString = context.Request.QueryString;
                    string[] args = new string[2];
                    if (!string.IsNullOrEmpty(queryString.Value))
                    {
                        queryString.Value.Substring(1).Split('&').ToList().ForEach(s =>
                        {
                            args = s.Split("=");
                            if (!args[1].IsNullOrEmpty())
                                dic.Add(args[0].ToUpper(), System.Web.HttpUtility.UrlDecode(args[1]));
                        });
                    }
                    break;
            }
            //按照Key来进行排序
            dic.OrderBy(s => s.Key).Select(s => new KeyValuePair<string, string>(s.Key.ToUpper(), s.Value)).ToList().ForEach(s =>
            {
                keyValuePairs.Add(s.Key, s.Value);
            });
            //序列化json字符串
            requestData = AppRealization.JSON.Serialize(keyValuePairs).Replace("\n", "").Replace("\t", "").Replace("\r", "");
            context.Request.Headers["UNIQUEKEY"] = MD5Encryption.GetMd5By32(requestData);
            keyValuePairs = new Dictionary<string, string>();
            dic.Add("TIMESTAMP", TimeStamp);
            //按照Key来进行排序
            dic.OrderBy(s => s.Key).Select(s => new KeyValuePair<string, string>(s.Key.ToUpper(), s.Value)).ToList().ForEach(s =>
            {
                keyValuePairs.Add(s.Key, s.Value);
            });
            //序列化json字符串
            string requestData1 = AppRealization.JSON.Serialize(keyValuePairs).Replace("\n", "").Replace("\t", "").Replace("\r", "");
            dic.Clear();
            keyValuePairs.Clear();
            dic = null;
            keyValuePairs = null;
            return requestData1;
        }


        private async Task<AppInformation?> QueryApp(string? AppId)
        {
            if (string.IsNullOrEmpty(AppId))
            {
                return null;
            }
            if (string.IsNullOrEmpty(AppQueryUrl))
            {
                throw new ConfigurationErrorsException("AppQueryUrl配置为空");
            }
            using (HttpClient client= httpClientFactory.CreateClient())
            {
                client.Timeout = new TimeSpan(0, 3, 0);
                string Url = AppQueryUrl.Replace("{AppId}", AppId);
                var result = await client.GetAsync(Url);
               
                if (result.StatusCode==System.Net.HttpStatusCode.OK)
                {
                    string Result = await result.Content.ReadAsStringAsync();
                    var data = AppRealization.JSON.Deserialize<RESTfulResult<AppInformation>>(Result);
                    return data.Data;
                }
                else
                {
                    return null;
                }
            }
        }

        private async Task<IList<AppRouteCacheDto>> QueryAppRouteAsync(string? AppId)
        {
            if (string.IsNullOrEmpty(AppId)) return null;
            if (string.IsNullOrEmpty(RouteQueryUrl))
            {
                throw new ConfigurationErrorsException("RouteQueryUrl配置为空");
            }
            var storeRoutes=AppRealization.RedisCache.String.Get($"App:RouteAuth:{AppId}");
            if (storeRoutes.IsNullOrEmpty())
            {
                using (HttpClient client = httpClientFactory.CreateClient())
                {
                    client.Timeout = new TimeSpan(0, 3, 0);
                    var result = await client.GetAsync(RouteQueryUrl.Replace("{AppId}", AppId));
                    if (result.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        string Result = await result.Content.ReadAsStringAsync();
                        var data = AppRealization.JSON.Deserialize<RESTfulResult<IList<AppRouteCacheDto>>>(Result);
                        return data.Data;
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            else
            {
                IList<AppRouteCacheDto> appRoutes=AppRealization.JSON.Deserialize<IList<AppRouteCacheDto>>(storeRoutes);
                return appRoutes;
            }
               
        }


    }
}
