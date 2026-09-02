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
namespace Air.Cloud.EntityFrameWork.Core.Helpers;

/// <summary>
/// <para>zh-cn:仓储异常辅助方法。</para>
/// <para>en-us:Repository exception helpers.</para>
/// </summary>
internal static class RepositoryExceptionHelpers
{
    /// <summary>
    /// <para>zh-cn:创建实体未找到异常，供 EF Core 仓储查询方法统一使用。</para>
    /// <para>en-us:Creates the standard not-found exception used by EF Core repository query methods.</para>
    /// </summary>
    /// <returns>
    /// <para>zh-cn:实体未找到异常实例。</para>
    /// <para>en-us:A not-found exception instance.</para>
    /// </returns>
    internal static InvalidOperationException DataNotFoundException()
    {
        return new InvalidOperationException("Entity not found.");
    }
}
