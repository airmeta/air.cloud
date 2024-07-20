/*
 * Copyright (c) 2024 星曳数据
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 *
 * This file is provided under the Mozilla Public License Version 2.0,
 * and the "NO WARRANTY" clause of the MPL is hereby expressly
 * acknowledged.
 */
using Air.Cloud.Core.Standard.Container.Model;

using Microsoft.VisualBasic;

using System.Collections.Concurrent;
using System.Net;
using System.Runtime.InteropServices;

namespace Air.Cloud.Core.Standard.Container
{
    /// <summary>
    /// <para>zh-cn:容器宿主机信息</para>
    /// <para>en-us:Host container information </para>
    /// </summary>
    /// <typeparam name="TContainerInstance"></typeparam>
    public  interface IHostContainer<TContainerInstance> where TContainerInstance : class, IContainerInstance, new()
    {

        /// <summary>
        /// <para>zh-cn:宿主机IP信息</para>
        /// <para>en-us: Host ip address</para>
        /// </summary>
        public IPAddress HostIpAddress { get; set; }
        
        /// <summary>
        /// <para>zh-cn:容器信息</para>
        /// <para>en-us:Container instances</para>
        /// </summary>
        public ConcurrentBag<TContainerInstance> Containers { get; set; }

        /// <summary>
        /// <para>zh-cn:宿主机IP信息</para>
        /// <para>en-us: Host ip address</para>
        /// </summary>
        public OSPlatform OSPlatform { get; set; }

        /// <summary>
        /// <para>zh-cn:读取时间</para>
        /// <para>en-us:Read container information time</para>
        /// </summary>
        public DateTime ReadTime { get; set; }

        /// <summary>
        /// <para>zh-cn:读取容器信息</para>
        /// <para>en-us:ReRead container</para>
        /// </summary>
        /// <returns></returns>
        public Task Load();
    }
}
