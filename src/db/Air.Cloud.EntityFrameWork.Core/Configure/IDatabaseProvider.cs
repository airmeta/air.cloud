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

namespace Air.Cloud.EntityFrameWork.Core.Configure;

/// <summary>
/// <para>zh-cn:数据库 Provider 的稳定身份描述，由具体 Provider 包实现。</para>
/// <para>en-us:Stable identity descriptor for a database Provider, implemented by the concrete Provider package.</para>
/// </summary>
public interface IDatabaseProvider
{
    /// <summary>
    /// <para>zh-cn:Provider 的稳定标识，仅由 Provider 实现维护。</para>
    /// <para>en-us:The stable Provider key maintained by the Provider implementation.</para>
    /// </summary>
    string ProviderKey { get; }
}
