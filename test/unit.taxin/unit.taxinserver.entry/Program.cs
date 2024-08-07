
/*
 * Copyright (c) 2024 ÐÇÒ·Êý¾Ý
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 *
 * This file is provided under the Mozilla Public License Version 2.0,
 * and the "NO WARRANTY" clause of the MPL is hereby expressly
 * acknowledged.
 */
using Air.Cloud.Core.App;
using Air.Cloud.WebApp.App;
using Air.Cloud.WebApp.DataValidation.Attributes;

var builder = WebApplication.CreateBuilder(args);

var app = builder.WebInjectInFile();
//var validationTypes = AppCore.CrucialTypes.Where(u => u.IsDefined(typeof(ValidationTypeAttribute), true) && u.IsEnum);
app.Run();