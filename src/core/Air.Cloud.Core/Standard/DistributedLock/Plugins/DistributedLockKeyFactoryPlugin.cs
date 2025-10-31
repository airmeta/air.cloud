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
using Air.Cloud.Core.Plugins.Security.MD5;

namespace Air.Cloud.Core.Standard.DistributedLock.Plugins
{
    /// <summary>
    /// <para>zh-cn:默认分布式锁定键工厂插件</para>
    /// <para>en-us:Default Distributed Lock Key Factory Plugin</para>
    /// </summary>
    public class DistributedLockKeyFactoryPlugin : IDistributedLockKeyFactoryPlugin
    {
        /// <inheritdoc/>
        public string GetKey(string param = null)
        {
            return MD5Encryption.GetMd5By32(param);
        }
    }
}
