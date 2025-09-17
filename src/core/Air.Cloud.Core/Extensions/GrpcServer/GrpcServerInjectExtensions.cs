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
using Air.Cloud.Core.Extensions.GrpcServer.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Hosting;

using System.Net;

/// <summary>
/// <para>zh-cn:注册Grpc服务器</para>
/// <para>en-us:Register Grpc server</para>
/// </summary>
public static class GrpcServerInjectExtensions
{
    /// <summary>
    /// <para>zh-cn:初始化注册Grpc服务器</para>
    /// <para>en-us:Initialize and register the Grpc server</para>
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="ServiceAddress">
    ///  <para>zh-cn:系统运行地址提供器,传入一个Func函数,要求该函数返回一个string字符串作为系统启动地址,如果为空则自动获取</para>
    ///  
    /// </param>
    /// <returns></returns>
    public static WebApplicationBuilder InjectGrpcServer(this WebApplicationBuilder builder,
        Func<WebApplicationBuilder, string> ServiceAddress = null,string Scheme="http")
    {
        builder.WebHost.ConfigureKestrel(options =>
        {
            GrpcServiceOptions grpcServiceOptions = AppConfigurationLoader.InnerConfiguration.GetConfig<GrpcServiceOptions>();
            options.Listen(IPAddress.Any, grpcServiceOptions.Port, listenOptions =>
            {
                listenOptions.Protocols = HttpProtocols.Http2;
            });
            string RpcUrls = $"{Scheme}://{IPAddress.Any}:{grpcServiceOptions.Port}";
            if (ServiceAddress != null)
            {
                string ServiceAddressStr = ServiceAddress.Invoke(builder);
                if (!ServiceAddressStr.IsNullOrEmpty())
                {
                    Uri serverIpAddress = new Uri(ServiceAddressStr);
                    options.Listen(IPAddress.Any, serverIpAddress.Port, listenOptions =>
                    {
                        listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
                    });
                }
                builder.WebHost.UseUrls(RpcUrls, ServiceAddressStr);
            }
            else
            {
                string Urls = AppConst.GetApplicationUrls();
                IList<string> UrlArr = Urls.Split(";").ToList();
                foreach (string Url in UrlArr)
                {
                    Uri serverIpAddress = new Uri(Url);
                    if (Url.Contains("localhost"))
                    {
                        options.ListenLocalhost(serverIpAddress.Port, listenOptions =>
                        {
                            listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
                        });
                    }
                    else
                    {
                        options.Listen(IPAddress.Parse(Url), serverIpAddress.Port, listenOptions =>
                        {
                            listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
                        });
                    }
                }
                UrlArr.Add(RpcUrls);
            }
        });
        return builder;
    }

    /// <summary>
    /// <para>zh-cn:初始化注册Grpc服务器</para>
    /// <para>en-us:Initialize and register the Grpc server</para>
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="UseStartUp">
    ///  <para>zh-cn:启动配置</para>
    ///  <para>en-us:Startup configuration</para>
    /// </param>
    /// <returns>
    ///  <see cref="IHostBuilder"/>
    /// </returns>
    public static IHostBuilder InjectGrpcServer(this IHostBuilder builder, Action<IWebHostBuilder> UseStartUp=null)
    {
        GrpcServiceOptions grpcServiceOptions = AppConfigurationLoader.InnerConfiguration.GetConfig<GrpcServiceOptions>();
        builder.ConfigureWebHostDefaults(webBuilder =>
        {
            webBuilder.ConfigureKestrel(options =>
            {
                options.Listen(IPAddress.Any, grpcServiceOptions.Port, listenOptions =>
                {
                    listenOptions.Protocols = HttpProtocols.Http2;
                });
            });
            UseStartUp?.Invoke(webBuilder);
        });
        return builder;
    }
}
