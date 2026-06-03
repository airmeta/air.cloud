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

using Air.Cloud.WebApp.UnifyResult.Attributes;
using Air.Cloud.WebApp.UnifyResult.Options;

using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Air.Cloud.WebApp.UnifyResult.Conventions;

/// <summary>
/// 统一返回应用模型转换器。
/// </summary>
[IgnoreScanning]
internal sealed class UnifyResultApplicationModelConvention : IApplicationModelConvention
{
    private readonly UnifyResultRuntimeOptions _runtimeOptions;

    public UnifyResultApplicationModelConvention(UnifyResultRuntimeOptions runtimeOptions)
    {
        _runtimeOptions = runtimeOptions;
    }

    public void Apply(ApplicationModel application)
    {
        if (_runtimeOptions?.Enabled != true) return;

        foreach (var action in application.Controllers.SelectMany(controller => controller.Actions))
        {
            foreach (var attribute in GetUnifyResultAttributes(action))
            {
                attribute.ConfigureResultModel(_runtimeOptions.ResultModelType);
            }
        }
    }

    private static IEnumerable<UnifyResultAttribute> GetUnifyResultAttributes(ActionModel action)
    {
        return action.Filters.OfType<UnifyResultAttribute>()
                     .Concat(action.Attributes.OfType<UnifyResultAttribute>())
                     .Distinct();
    }
}
