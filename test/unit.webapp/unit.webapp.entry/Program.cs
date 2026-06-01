
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
using Air.Cloud.Modules.Consul.Extensions;

using unit.webapp.entry.Plugins;

var builder = WebApplication.CreateBuilder(args);
//var app = builder.InjectGrpcServer().WebInjectInConsul();
var app = builder.WebInjectInConsul();

AppRealization.Output.Print("内部访问令牌信息", RsaEncryption.Encrypt(AppRealization.PID.Get(), RsaKeyConst.PUBLIC_KEY, RsaKeyConst.PRIVATE_KEY));
app.Run();
