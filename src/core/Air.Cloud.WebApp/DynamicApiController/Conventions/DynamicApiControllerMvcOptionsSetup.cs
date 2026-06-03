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

using Air.Cloud.WebApp.DynamicApiController.Options;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Air.Cloud.WebApp.DynamicApiController.Conventions;

/// <summary>
/// 动态接口控制器 MVC 选项配置。
/// </summary>
[IgnoreScanning]
internal sealed class DynamicApiControllerMvcOptionsSetup : IConfigureOptions<MvcOptions>
{
    private readonly DynamicApiControllerApplicationModelConvention _convention;

    public DynamicApiControllerMvcOptionsSetup(DynamicApiControllerApplicationModelConvention convention)
    {
        _convention = convention;
    }

    public void Configure(MvcOptions options)
    {
        options.Conventions.Add(_convention);
    }
}
