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
using Air.Cloud.Core.Standard.KVCenter;

namespace Air.Cloud.Modules.Nacos.Model
{
    /// <summary>
    /// <para>zh-cn:Nacos 配置中心键值信息，Key 对应 dataId，Value 对应配置内容。</para>
    /// <para>en-us>Nacos config-center key-value information where Key maps to dataId and Value maps to config content.</para>
    /// </summary>
    public class NacosKvCenterServiceInformation : IKVCenterServiceOptions
    {
        /// <inheritdoc/>
        public string Key { get; set; }

        /// <inheritdoc/>
        public string Value { get; set; }

        /// <summary>
        /// <para>zh-cn:Nacos 配置分组。</para>
        /// <para>en-us>The Nacos config group.</para>
        /// </summary>
        public string Group { get; set; }

        /// <summary>
        /// <para>zh-cn:配置格式类型，例如 json、yaml、text。</para>
        /// <para>en-us>The config format type, for example json, yaml, or text.</para>
        /// </summary>
        public string Type { get; set; }
    }
}
