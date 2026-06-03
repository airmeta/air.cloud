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

using Microsoft.AspNetCore.Http;

namespace Air.Cloud.WebApp.UnifyResult.Internal;

/// <summary>
/// 统一返回状态码解析器。
/// </summary>
[IgnoreScanning]
internal static class UnifyResultStatusCodeResolver
{
    public static void SetResponseStatusCodes(HttpContext context, int statusCode, UnifyResultSettingsOptions unifyResultSettings)
    {
        if (unifyResultSettings == null) return;

        if (unifyResultSettings.AdaptStatusCodes != null && unifyResultSettings.AdaptStatusCodes.Length > 0)
        {
            var adaptStatusCode = unifyResultSettings.AdaptStatusCodes.FirstOrDefault(item => item.Length > 1 && item[0] == statusCode);
            if (adaptStatusCode != null && adaptStatusCode[0] > 0)
            {
                context.Response.StatusCode = adaptStatusCode[1];
                return;
            }
        }

        if (unifyResultSettings.Return200StatusCodes == null) context.Response.StatusCode = StatusCodes.Status200OK;
        else if (unifyResultSettings.Return200StatusCodes.Contains(statusCode)) context.Response.StatusCode = StatusCodes.Status200OK;
    }
}
