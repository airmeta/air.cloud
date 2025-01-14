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
namespace Air.Cloud.Core.Standard.Container.Model
{
    /// <summary>
    /// <para>zh-cn:容器实例基础信息</para>
    /// <para>en-us:Container instance inforamtion</para>
    /// </summary>
    /// <remarks>
    /// This itance is used to store the basic information of the container
    /// You must use this interface to implement the container information
    /// </remarks>
    public interface IContainerInstance
    {
        /// <summary>
        /// <para>zh-cn:端口号</para>
        /// <para>en-us: Port</para>
        /// </summary>
        public ushort? Port { get; set; }
        /// <summary>
        /// <para>zh-cn:ip地址</para>
        /// <para>en-us:container ipaddress</para>
        /// </summary>
        public string IPAddress { get; set; }
    }
}
