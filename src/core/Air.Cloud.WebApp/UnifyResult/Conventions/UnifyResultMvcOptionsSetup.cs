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

using Air.Cloud.WebApp.UnifyResult.Options;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Air.Cloud.WebApp.UnifyResult.Conventions;

/// <summary>
/// 统一返回 MVC 选项配置。
/// </summary>
[IgnoreScanning]
internal sealed class UnifyResultMvcOptionsSetup : IPostConfigureOptions<MvcOptions>
{
    private readonly IOptions<UnifyResultRuntimeOptions> _runtimeOptions;

    public UnifyResultMvcOptionsSetup(IOptions<UnifyResultRuntimeOptions> runtimeOptions)
    {
        _runtimeOptions = runtimeOptions;
    }

    public void PostConfigure(string name, MvcOptions options)
    {
        if (_runtimeOptions.Value.Enabled != true) return;

        options.Conventions.Add(new UnifyResultApplicationModelConvention(_runtimeOptions.Value));
    }
}
