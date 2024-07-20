/*
 * Copyright (c) 2024 星曳数据
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
using Air.Cloud.Modules.Docker.Extensions;
using Air.Cloud.Modules.Docker.Model;
using Air.Cloud.WebApp.App;
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



