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
namespace Air.Cloud.Core.Standard.CICD.Config
{
    /// <summary>
    /// <para>zh-cn:定义容器配置信息标准。</para>
    /// <para>en-us:Defines the container configuration information contract.</para>
    /// </summary>
    public interface IContainerConfig
    {
        /// <summary>
        /// <para>zh-cn:获取或设置容器唯一标识。</para>
        /// <para>en-us:Gets or sets the unique container identifier.</para>
        /// </summary>
        public string ContainerId { get; set; }
    }
}
