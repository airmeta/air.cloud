
﻿/*
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
using Air.Cloud.Core.App.Options;
using Air.Cloud.Core.Extensions;
using Air.Cloud.Core.Plugins.Http.Extensions;
using Air.Cloud.Core.Plugins.InternalAccess;
using Air.Cloud.Core.Standard.Assemblies.Model;
using Air.Cloud.Core.Standard.DynamicServer;
using Air.Cloud.Core.Standard.Security;
using Air.Cloud.Core.Standard.Security.Model;
using Air.Cloud.Core.Standard.Security.Options;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.DependencyInjection;

using System.ComponentModel;
using System.Reflection;

public static class SecurityExtensions
{
    /// <summary>
    /// <para>zh-cn:启用安全认证中间件</para>
    /// <para>en-us:Enable security authentication middleware</para>
    /// </summary>
    /// <param name="app"></param>
    /// <returns></returns>
    public static IApplicationBuilder UseSecurityServer(this IApplicationBuilder app)
    {
        var options = AppCore.GetOptions<AuthenticaOptions>();
        app.Map(new PathString(options.PushRoute), application =>
        {
            application.Use(next =>
            {
                return async (context) =>
                {
                    var endpointDatas = await context.Request.ReadFromJsonAsync<SecurityPushData>();
                    if (endpointDatas != null)
                    {
                        foreach (var item in endpointDatas.endpointDatas)
                        {
                            ISecurityServerStandard.ServerEndpointDatas.Add(item);
                        }
                    }
                    await context.Response.WriteAsJsonAsync(new SecurityPushResult
                    (){
                        IsSuccess = true
                    });
                };
            });
        });
        return app;
    }

    /// <summary>
    /// <para>zh-cn:注入微服务安全认证客户端服务</para>
    /// <para>en-us:Inject microservice security authentication client services</para>  
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddMicroServiceSecurity(this IServiceCollection services)
    {
        AppRealization.AssemblyScanning.Add(new AssemblyScanningEvent()
        {
            Key = "AddAuthenticationClient",
            Description = "注入授权服务客户端",
            TargetType = typeof(IDynamicService),
            Action = (type) =>
            {
                var TypeRouteTemplate = type.GetCustomAttribute<RouteAttribute>(false);
                string InterfaceTemplate = TypeRouteTemplate == null ? null : TypeRouteTemplate.Template;
                EndpointData endpointData;
                //读取客户端信息
                MethodInfo[] methods = type.GetMethods().Where(s => s.IsPublic && s.IsStatic == false).ToArray();
                foreach (MethodInfo method in methods)
                {
                    var HttpMethods = method.GetCustomAttribute<HttpMethodAttribute>();
                    var Route = method.GetCustomAttribute<RouteAttribute>();
                    if (HttpMethods != null || Route != null)
                    {
                        if (HttpMethods == null)
                        {
                            continue;
                        }
                        string? Template = HttpMethods != null ?
                                   HttpMethods.Template.IsNullOrEmpty() ?
                                       Route?.Template : HttpMethods?.Template
                                    : Route?.Template;

                        string Method = HttpMethods == null ? "GET" : string.Join(",", HttpMethods.HttpMethods);
                        string Path = (InterfaceTemplate.IsNullOrEmpty() ? string.Empty : (InterfaceTemplate + "/")) + Template;
                        //找到授权特性
                        var AllowAnonymous = method.GetCustomAttribute<AllowAnonymousAttribute>();
                        var Authorize = method.GetCustomAttribute<AuthorizeAttribute>();
                        var DescriptionAttributes = method.GetCustomAttribute<DescriptionAttribute>();
                        if (AllowAnonymous == null && Authorize == null)
                        {
                            endpointData = new EndpointData()
                            {
                                IsAllowAnonymous = false,
                                RequiresAuthorization = true,
                                AuthorizeData = null,
                                Method = Method,
                                Path = Path,
                                Description = DescriptionAttributes?.Description
                            };
                        }
                        else if (AllowAnonymous != null)
                        {
                            endpointData = new EndpointData()
                            {
                                IsAllowAnonymous = true,
                                RequiresAuthorization = false,
                                AuthorizeData = null,
                                Method = Method,
                                Path = Path,
                                Description = DescriptionAttributes?.Description
                            };
                        }
                        else
                        {
                            endpointData = new EndpointData()
                            {
                                IsAllowAnonymous = false,
                                RequiresAuthorization = true,
                                AuthorizeData = new EndPointAuthorizeData()
                                {
                                    AuthenticationSchemes = Authorize?.AuthenticationSchemes,
                                    Policy = Authorize?.Policy,
                                    Roles = Authorize?.Roles,
                                },
                                Method = Method,
                                Path = Path,
                                Description = DescriptionAttributes?.Description
                            };
                        }
                        ISecurityClientStandard.ClientEndpointDatas.Add(endpointData);
                    }
                }
            },
            Finally = () =>
            {
                AppRealization.Output.Print("安全防护", $"当前服务数据信息已收集完成,共计发现:{ISecurityClientStandard.ClientEndpointDatas.Count}个接口服务,稍后将自动上报给网关服务");
                try
                {
                    var httpClientFactory = AppCore.GetService<IHttpClientFactory>();
                    var Appsettings = AppCore.GetOptions<AppSettingsOptions>();
                    IDictionary<string, string> Headers = new Dictionary<string, string>();
                    IInternalAccessValidPlugin internalAccessValidPlugin = AppRealization.AppPlugin.GetPlugin<IInternalAccessValidPlugin>();
                    if (internalAccessValidPlugin != null)
                    {
                        var AccessToken = internalAccessValidPlugin.CreateInternalAccessToken();
                        Headers.Add(AccessToken.Item1, AccessToken.Item2);
                    }
                    using (var client = httpClientFactory.CreateClient())
                    {
                        client.Timeout = TimeSpan.FromSeconds(10);
                        //string Url= $"{Appsettings.GateWayAddress.TrimEnd('/')}{AppCore.GetOptions<AuthenticaOptions>().PushRoute}";
                        Uri Url = new Uri(new Uri("http://localhost:5125"), AppCore.GetOptions<AuthenticaOptions>().PushRoute);
                        var response = client.PostAsync(Url, client.SetHeaders(Headers).SetBody(new SecurityPushData()
                        {
                            ApplicationName = AppConst.ApplicationName,
                            ApplicationPID = AppRealization.PID.Get(),
                            endpointDatas = ISecurityClientStandard.ClientEndpointDatas
                        })).GetAwaiter().GetResult();
                        string Content = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                        if (response.StatusCode==System.Net.HttpStatusCode.OK)
                        {
                            try
                            {
                                var Result = AppRealization.JSON.Deserialize<SecurityPushResult>(Content);
                                AppRealization.Output.Print("安全防护", $"服务数据信息上报网关服务完成,状态码:{response.StatusCode},结果:{Result.IsSuccess}");
                            }
                            catch (Exception)
                            {
                                AppRealization.Output.Print("安全防护", $"服务数据信息上报网关服务失败,状态码:{response.StatusCode}", Air.Cloud.Core.Modules.AppPrint.AppPrintLevel.Error, AdditionalParams: new Dictionary<string, object>()
                                {
                                     {"message", "上报失败"},
                                     {"Url",Url},
                                     {"Content",Content},
                                });
                                Environment.Exit(1);
                            }
                        }
                        else
                        {
                            AppRealization.Output.Print("安全防护", $"服务数据信息上报网关服务失败,状态码:{response.StatusCode}",Air.Cloud.Core.Modules.AppPrint.AppPrintLevel.Error, AdditionalParams: new Dictionary<string, object>()
                                {
                                     {"message", "上报失败"},
                                     {"Url",Url}
                                });
                            Environment.Exit(1);
                        }
                    }
                }
                catch (Exception ex)
                {
                    AppRealization.Output.Print("安全防护", $"服务数据上报失败,已强制停止运行!",Air.Cloud.Core.Modules.AppPrint.AppPrintLevel.Error,AdditionalParams:new Dictionary<string, object>()
                    {
                         {"message",ex.Message },
                         {"StackTrace",ex.StackTrace }
                    });
                    Environment.Exit(1);
                }
               
            }
        });
        return services;
    }
    public class SecurityPushData
    {
        /// <summary>
        /// 应用程序名
        /// </summary>
        public string ApplicationName { get; set; }

        /// <summary>
        /// 应用程序PID
        /// </summary>
        public string ApplicationPID { get; set; }
        /// <summary>
        /// 端点信息
        /// </summary>
        public IList<EndpointData> endpointDatas { get; set; }
    }
    public class SecurityPushResult
    {
        public bool IsSuccess { get; set; }
    }
}