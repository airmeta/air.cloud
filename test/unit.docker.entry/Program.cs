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
using Air.Cloud.Core.Plugins.Security.RSA;
using Air.Cloud.Modules.Docker.Extensions;
using Air.Cloud.Modules.Docker.Model;
using Air.Cloud.WebApp.App;
/// <summary>
/// 私钥
/// </summary>
 string PRIVATE_KEY = @"-----BEGIN RSA PRIVATE KEY-----
MIIEogIBAAKCAQEAiv0axFNMfnBgWOcnoSTVz7zKaYxUbpI+UsZKzRwiw3WQx2NZ
8tJQpUBDdDfDAYol+VtpLq0e1thOZkGDf8j7e1FUji4nyIRLs8EXlitWlTZfO6px
ZXQjpMzpuRkUYwMXOMUFBHgO8wGbqvBiXBKXwLruBiaOwT7DIROL1qCsi7y0h6y3
exlejvNz6tF71kGnbqkOy9zh/yjubvBCopHBIir7GeqYqZZzPuhG9AtkPlq77w1f
EE+DPIZGs3f12hjJmPiLkOCX++cnO2k32QqK4F6gddSuG4sDTXAlodpLRA54knbd
P0qlkFWuABrTc5NbDIutJXvdQh39v0qv7wKUgwIDAQABAoIBAAJNVeupi4tOljHy
xjPDle0Gbf3Yjq90KacaVLPYAvVk1ZyP1zYP6LkL+vIKWGcGoEFkk7XrtSaO16GW
9TQhIpU00Cc4uXz3P/++s4LPvfPjvikRViZ9iXeZ00c3FKDdNL3CeaZMzMaWoLrw
l1h6EFxnXDjq25N01CuQlNDdwW3hEIa/AineR/Zxj8A2xoOhxFC9fe6/7fHHjVYo
cqopDLmmH60wrQLsZ35p9Y4nVABZQL+TApzZMoWW3yKuHDOxUgbQWnwXUYBkTkg/
c/nWsL6hWU1LDSBMQEyZ2BgYTqfqPzKDhAOErVr1ahoIeuTE05NIchgjhqsD9MHh
5RWimNECgYEAvVr0DR0ZvtI04LRu7hgLeQIlfObkGbAYLDM8tYjBO/y4CHSYTaa9
+VNtw8Pd831qYN1EZ26fthTb6tfs3yDY+B6kVOd6iNFlHgpkInAedOtrEbhC1WmZ
145t18G/9mWX1FzQX9kcDvJbP07hm23NowXTOjOW5uCI6is/7bL9JvECgYEAu+gV
uV8w1e0z+bxzmRXCKZZrl39b59yRYUFLWn0NCf43y04Y0cSa0xvWBisicAEk7J79
ZZCD92t+UoelS1yPeqiUwx159y3a57CracU2eO6uAH92J54pKLcTHtcqEshRWlTZ
MCej8lNFxIyIH44InXgI5eUhf/tRcfNm3EmJ+rMCgYAUeHm71TKMU5NN25PGf6j1
2wqMdzWfpU7nsF5WzcL+JjMLDvfMJUOSabeLG2iqQxu1/xW/DNGNULH5sIA2Gwn5
wO5JE4FGu0RwO/VZV7+jKjQ4BTCMe88a45XyZkrHa3I/jg0k34bOAttke7WeJP+/
KQkN2Lfum8WRcz2FB/2gMQKBgAcRTqffRbX2KwtMpEhwwhHQX5GeL7XD3Q/8Zbos
k+35St3xvQs+ytf/5wfqXWw7Dsl7nWpRijUnLOQrx/LSs27YomfIVwsOBXrLcVcU
HN1llNPd93K8By5J3IU/cIyuTikIofamtwrpSOmAo6oULIzHtAX5nU30BPc1QXwt
o+vzAoGAPGmCxTJ1QWqyhBfgfqg8OP8lmvhNWhzho3pUtoD4X8mW+dCizR8bmO+u
AQc7ivJ3vunCeVMW5FjfcyyawJhTgD0WYC7iH80phAMCbsLw4DiW/Xaa8rggwldl
elw2IpoNLtuHIgV8caI5VrLqgsO8ZEV6munU/VBli22HcKCGF6Y=
-----END RSA PRIVATE KEY-----
";
/// <summary>
/// 公钥
/// </summary>
  string PUBLIC_KEY = @"-----BEGIN PUBLIC KEY-----
MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAiv0axFNMfnBgWOcnoSTV
z7zKaYxUbpI+UsZKzRwiw3WQx2NZ8tJQpUBDdDfDAYol+VtpLq0e1thOZkGDf8j7
e1FUji4nyIRLs8EXlitWlTZfO6pxZXQjpMzpuRkUYwMXOMUFBHgO8wGbqvBiXBKX
wLruBiaOwT7DIROL1qCsi7y0h6y3exlejvNz6tF71kGnbqkOy9zh/yjubvBCopHB
Iir7GeqYqZZzPuhG9AtkPlq77w1fEE+DPIZGs3f12hjJmPiLkOCX++cnO2k32QqK
4F6gddSuG4sDTXAlodpLRA54knbdP0qlkFWuABrTc5NbDIutJXvdQh39v0qv7wKU
gwIDAQAB
-----END PUBLIC KEY-----
";
string Content = RsaEncryption.Decrypt("mtD1ZBL8sGxP69u14W3QjD8nG0v70jFx0T1zYKqo9BIGEoQfWieqB6AJAZFrSWQooETHzm90eKVWx0BaGy+YJyMrwi/oeya4r8Q9y3iGP4dNC4gccOrq8xGg9snc4v8tf/6qVTBgqVirsxaICGaZzgDwmJPShDBhrqFot9MGIZrfHCPUT1Zu58tBh2dQbo2sW2rhV9Aq4Fq86OOYIu0hOvwgdmUr4Z0sOI2uyrM5kWvXnk1ihFmtHvEHwO5rDNkIAGxFj+92E0Pw3Y0zkCWhJvdbac9vfVzCfnt0kOsQN00HsEGP+1qlQoUbfcedAgzL+fmFHmhPeDKp0k5l7Vnz",PUBLIC_KEY,PRIVATE_KEY);
string Content1 = RsaEncryption.Decrypt("NC+pKBGhkkf9LQzKbwNIgh4xRBcdyOwFbPlRGZ3MTvAaZZoRDzby0CWHXEwffTNxr6s+n9nUq0Be4LnuUcsaXHJEmXSyRsZG/itGZs/bXGxsEQIcW3A4lZLAYmVLP+Og2jODy5OZSAXErdK3lQNn3QeWITftQWakIByabNaTrHf/SkgZsRoVOMHn4WZY8wOwmZBuykF92nl0rou8QvFvXNuxU/XY1aqRXPbOUwx+BhLBvny9AM747Fh4rEqcINm4rU+rtt4uvL+XlOTe3p9ZRBPJMIlAqLpLXd9G8X56P188hRaptGebOjWL9M/PhybznEtNco2eSYI7w987XwHx+w==", PUBLIC_KEY, PRIVATE_KEY);
string Content2 = RsaEncryption.Decrypt("djL4J7sNikjZRxp4fBOH/LibicsLKFZyjJr6Q6aFhRTPpB/x/R6Bn/24MptY5odKvbGyej4UL0VM1Ei6V68tNbWfq2qr+sEB5frBbRo3affkpdMNjrP8jMJEp+FO5w23PVKMcwBGnf32vWEKEvMAexM6QTjGjO6Rah2XYhhTB50tL5QT5I54HFVDFdk+7GIj1KwRLG/zvoZ73E7R77s0f/KP6OgtX29cgCfma49xdiH8gduuPNoM32ahtf3xiEeQNXk3pdcYxrsUugWJ9DTv7fU1cX/ytEfYziR8ydd5miQHYTgZCLq2Ep9CplDAl0lRXeFE7qMY/FsaXPLzIUbL1w==", PUBLIC_KEY, PRIVATE_KEY);


var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDockerEngineService();
var app = builder.WebInjectInFile();
#region  数据读取
//string path = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, "1.txt");
//File.WriteAllText(path, AppRealization.JSON.Serialize
#endregion
app.MapGet("/stop", async (s) =>
{
    DockerContainer<DockerContainerInstance> containers = new DockerContainer<DockerContainerInstance>();
    await containers.Load();
    string ID = s.Request.Query["ID"];
    var Container = containers.Containers.FirstOrDefault(s=>s.Container.ID==ID);
    Container=await AppRealization.Container.StopAsync(Container);
});
app.MapGet("/start", async (s) =>
{
    DockerContainer<DockerContainerInstance> containers = new DockerContainer<DockerContainerInstance>();
    await containers.Load();
    string ID = s.Request.Query["ID"];
    var Container = containers.Containers.FirstOrDefault(s => s.Container.ID == ID);
    Container = await AppRealization.Container.StartAsync(Container);
});

app.MapGet("/", async () =>
{
    DockerContainer<DockerContainerInstance> container = new DockerContainer<DockerContainerInstance>();
    await container.Load();
    string JSTR = container.Serialize();
    DockerContainer<DockerContainerInstance> container1 = DockerContainer<DockerContainerInstance>.DeSerialize(JSTR);
    return JSTR;
});

app.Run();



