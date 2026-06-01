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
namespace Air.Cloud.Core.Standard.CICD.Model
{
    /// <summary>
    /// <para>zh-cn:定义容器运行状态信息标准。</para>
    /// <para>en-us:Defines the container runtime status information contract.</para>
    /// </summary>
    public interface IContainerStatus
    {
        /// <summary>
        /// <para>zh-cn:获取或设置容器唯一标识。</para>
        /// <para>en-us:Gets or sets the unique container identifier.</para>
        /// </summary>
        public string ContainerId { get; set; }

        /// <summary>
        /// <para>zh-cn:获取或设置容器是否处于正常运行状态。</para>
        /// <para>en-us:Gets or sets whether the container is in a normal running state.</para>
        /// </summary>
        public bool ContainerStatus { get; set; }
    }
}
