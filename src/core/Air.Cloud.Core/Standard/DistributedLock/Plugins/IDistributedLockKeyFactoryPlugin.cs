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
using Air.Cloud.Core.Plugins;

namespace Air.Cloud.Core.Standard.DistributedLock.Plugins
{
    /// <summary>
    /// <para>zh-cn:分布式锁定键工厂插件接口</para>
    /// <para>en-us:Distributed Lock Key Factory Plugin Interface</para>
    /// </summary>
    public interface IDistributedLockKeyFactoryPlugin : IPlugin
    {
        /// <summary>
        /// <para>zh-cn:获取锁定键</para>
        /// <para>en-us:Get lock key</para>
        /// </summary>
        /// <param name="param">
        ///  <para>zh-cn:用于生成锁定键的参数,一般是请求参数,JSON格式</para>
        ///  <para>en-us:Parameters used to generate the lock key, usually request parameters, JSON format</para>
        /// </param>
        /// <returns>
        ///  <para>zh-cn:返回生成的锁定键</para>
        ///  <para>en-us:Returns the generated lock key</para>
        /// </returns>
        public string GetKey(string param = null);

    }
}
