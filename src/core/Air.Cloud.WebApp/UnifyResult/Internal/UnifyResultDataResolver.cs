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

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Air.Cloud.WebApp.UnifyResult.Internal;

/// <summary>
/// 统一返回结果数据解析器。
/// </summary>
[IgnoreScanning]
internal static class UnifyResultDataResolver
{
    public static bool CheckValidResult(IActionResult result, out object data)
    {
        data = default;

        var isDataResult = result switch
        {
            ViewResult => false,
            PartialViewResult => false,
            FileResult => false,
            ChallengeResult => false,
            SignInResult => false,
            SignOutResult => false,
            RedirectToPageResult => false,
            RedirectToRouteResult => false,
            RedirectResult => false,
            RedirectToActionResult => false,
            LocalRedirectResult => false,
            ForbidResult => false,
            ViewComponentResult => false,
            PageResult => false,
            _ => true,
        };

        if (isDataResult) data = result switch
        {
            ContentResult content => content.Content,
            ObjectResult obj => obj.Value,
            JsonResult json => json.Value,
            _ => null,
        };

        return isDataResult;
    }
}
