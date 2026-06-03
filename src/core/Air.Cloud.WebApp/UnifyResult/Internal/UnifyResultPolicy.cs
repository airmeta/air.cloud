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

using Air.Cloud.Core.Extensions;
using Air.Cloud.WebApp.Extensions;
using Air.Cloud.WebApp.UnifyResult.Attributes;
using Air.Cloud.WebApp.UnifyResult.Options;
using Air.Cloud.WebApp.UnifyResult.Providers;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using System.Reflection;

namespace Air.Cloud.WebApp.UnifyResult.Internal;

/// <summary>
/// 统一返回策略判断。
/// </summary>
[IgnoreScanning]
internal static class UnifyResultPolicy
{
    public static bool CheckSucceededNonUnify(
        MethodInfo method,
        IServiceProvider requestServices,
        UnifyResultRuntimeOptions runtimeOptions,
        out IUnifyResultProvider unifyResult,
        bool isWebRequest = true)
    {
        var isSkip = IsSucceededSkip(method, runtimeOptions);

        if (!isWebRequest)
        {
            unifyResult = null;
            return isSkip;
        }

        unifyResult = isSkip ? null : requestServices?.GetService<IUnifyResultProvider>();
        return unifyResult == null || isSkip;
    }

    public static bool CheckFailedNonUnify(
        MethodInfo method,
        IServiceProvider requestServices,
        UnifyResultRuntimeOptions runtimeOptions,
        out IUnifyResultProvider unifyResult)
    {
        var isSkip = IsFailedSkip(method, runtimeOptions);

        unifyResult = isSkip ? null : requestServices?.GetService<IUnifyResultProvider>();
        return unifyResult == null || isSkip;
    }

    public static bool CheckStatusCodeNonUnify(
        HttpContext context,
        UnifyResultRuntimeOptions runtimeOptions,
        out IUnifyResultProvider unifyResult)
    {
        var endpoint = context.GetEndpoint();
        if (endpoint == null)
        {
            unifyResult = null;
            return true;
        }

        var isSkip = runtimeOptions?.Enabled != true
                     || context.GetMetadata<NonUnifyAttribute>() != null
                     || endpoint.Metadata.GetMetadata<NonUnifyAttribute>() != null;

        unifyResult = isSkip ? null : context.RequestServices.GetService<IUnifyResultProvider>();
        return unifyResult == null || isSkip;
    }

    public static bool CheckSupportMvcController(
        HttpContext httpContext,
        ControllerActionDescriptor actionDescriptor,
        out UnifyResultSettingsOptions unifyResultSettings)
    {
        unifyResultSettings = httpContext.RequestServices.GetService<IOptions<UnifyResultSettingsOptions>>()?.Value;

        if (actionDescriptor == null) return false;
        if (unifyResultSettings?.SupportMvcController == false && typeof(Controller).IsAssignableFrom(actionDescriptor.ControllerTypeInfo)) return false;

        return true;
    }

    public static bool IsSucceededSkip(MethodInfo method, UnifyResultRuntimeOptions runtimeOptions)
    {
        return runtimeOptions?.Enabled != true
               || method.GetRealReturnType().HasImplementedRawGeneric(runtimeOptions.ResultModelType)
               || method.CustomAttributes.Any(attribute =>
                   typeof(NonUnifyAttribute).IsAssignableFrom(attribute.AttributeType)
                   || typeof(ProducesResponseTypeAttribute).IsAssignableFrom(attribute.AttributeType)
                   || typeof(IApiResponseMetadataProvider).IsAssignableFrom(attribute.AttributeType))
               || method.ReflectedType != null && method.ReflectedType.IsDefined(typeof(NonUnifyAttribute), true);
    }

    private static bool IsFailedSkip(MethodInfo method, UnifyResultRuntimeOptions runtimeOptions)
    {
        return runtimeOptions?.Enabled != true
               || method.CustomAttributes.Any(attribute => typeof(NonUnifyAttribute).IsAssignableFrom(attribute.AttributeType))
               || !method.CustomAttributes.Any(attribute =>
                      typeof(ProducesResponseTypeAttribute).IsAssignableFrom(attribute.AttributeType)
                      || typeof(IApiResponseMetadataProvider).IsAssignableFrom(attribute.AttributeType))
                  && method.ReflectedType != null
                  && method.ReflectedType.IsDefined(typeof(NonUnifyAttribute), true);
    }
}
