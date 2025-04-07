
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
using Air.Cloud.Modules.Kafka.Model;
using Air.Cloud.WebApp.App;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
var app = builder.WebInjectInFile();
app.MapGet("test", ([FromServices] IEventPublisher eventPublisher) =>
{
    AppRealization.Queue.Publish(new ProducerConfigModel()
    {
        TopicName= "fcj_workflow_audit_test"
    },new T()
    {
        Content = "这是测试用的",
    });
    //eventPublisher.PublishAsync("test1", "这是测试用的");
    return "发送成功";
});
app.Run();

public class T
{
    public string Content { get; set; }
}