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

using Air.Cloud.EntityFrameWork.Core.Configure;

namespace Air.Cloud.EntityFrameWork.Kingbase.Configure;

/// <summary>
/// <para>zh-cn:Kingbase Provider 的稳定身份描述。</para>
/// <para>en-us:Stable identity descriptor for the Kingbase Provider.</para>
/// </summary>
public sealed class KingbaseDataBaseProvider : IDatabaseProvider
{
    /// <summary><para>zh-cn:Kingbase Provider 的稳定标识。</para><para>en-us:The stable identifier of the Kingbase Provider.</para></summary>
    public string ProviderKey => "Kingbase";
}
