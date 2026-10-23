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
using Air.Cloud.WebApp.App;
using Air.Cloud.Core.Extensions;

using System.Net;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration
    .AddAirCloudConfigDirectory("config", builder.Environment.EnvironmentName, optional: true, reloadOnChange: true)
    .AddJsonFile("config/control-plane-auth.json", optional: true, reloadOnChange: true);
ConfigureControlPlaneManagementListener(builder);
builder.WebHost.UseKestrel(options =>
{
    options.Limits.MaxRequestLineSize = int.MaxValue;//HTTP 请求行的最大允许大小。 默认为 8kb
    options.Limits.MaxRequestBufferSize = int.MaxValue;//请求缓冲区的最大大小。 默认为 1M
    //任何请求正文的最大允许大小（以字节为单位）,默认 30,000,000 字节，大约为 28.6MB
    options.Limits.MaxRequestBodySize = int.MaxValue;//限制请求长度
});
var app = builder.WebInjectInFile();
app.Run();

static void ConfigureControlPlaneManagementListener(WebApplicationBuilder builder)
{
    var section = builder.Configuration.GetSection("ControlPlaneAuth:Management");
    if (!builder.Configuration.GetSection("ControlPlaneAuth").GetValue("Enabled", true)
        || !section.GetValue("Enabled", true))
    {
        return;
    }

    var port = section.GetValue("Port", 0);
    if (port <= 0)
    {
        return;
    }

    if (port > 65535)
    {
        throw new InvalidOperationException("ControlPlaneAuth:Management:Port must be between 1 and 65535.");
    }

    var bindAddress = section["BindAddress"];
    var listenAddress = string.IsNullOrWhiteSpace(bindAddress) || bindAddress == "*" ? "0.0.0.0" : bindAddress.Trim();
    if (listenAddress != "0.0.0.0" && !IPAddress.TryParse(listenAddress, out _))
    {
        throw new InvalidOperationException($"ControlPlaneAuth:Management:BindAddress '{listenAddress}' is not a valid IP address.");
    }

    var managementUrl = $"http://{listenAddress}:{port}";
    var endpointSection = builder.Configuration.GetSection("Kestrel:Endpoints");
    if (endpointSection.GetChildren().Any())
    {
        builder.Configuration["Kestrel:Endpoints:ControlPlaneManagement:Url"] = managementUrl;
        return;
    }

    var configuredUrls = builder.Configuration[WebHostDefaults.ServerUrlsKey]
        ?? Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
    var urls = string.IsNullOrWhiteSpace(configuredUrls)
        ? managementUrl
        : string.Join(';', configuredUrls.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Append(managementUrl));
    builder.WebHost.UseUrls(urls);
}
