
/*
 * Copyright (c) 2024-2030 ��ҷ����
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
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
var app = builder.WebInjectInFile();
app.MapGet("test", ([FromServices] IEventPublisher eventPublisher) =>
{
    eventPublisher.PublishAsync("test1", "���ǲ����õ�");
    return "���ͳɹ�";
});
app.Run();