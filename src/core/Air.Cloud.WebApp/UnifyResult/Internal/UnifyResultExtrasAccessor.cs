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

using Air.Cloud.Core.App;

namespace Air.Cloud.WebApp.UnifyResult.Internal;

/// <summary>
/// 统一返回附加信息访问器。
/// </summary>
[IgnoreScanning]
internal static class UnifyResultExtrasAccessor
{
    private const string UnifyResultExtrasKey = "UNIFY_RESULT_EXTRAS";

    public static void Fill(object extras)
    {
        var items = AppCore.HttpContext?.Items;
        if (items == null) return;

        items[UnifyResultExtrasKey] = extras;
    }

    public static object Take()
    {
        return AppCore.HttpContext?.Items?.TryGetValue(UnifyResultExtrasKey, out var extras) == true
            ? extras
            : null;
    }
}
