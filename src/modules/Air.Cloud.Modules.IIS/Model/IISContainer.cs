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
using Air.Cloud.Core;
using Air.Cloud.Core.Standard.Container.Model;

using Newtonsoft.Json;

using System.Collections.Concurrent;
using System.Net;
using System.Runtime.InteropServices;

namespace Air.Cloud.Modules.IIS.Model
{
    /// <summary>
    /// <para>zh-cn:IIS 容器信息</para>
    /// <para>en-us:IIS container information</para>
    /// </summary>
    /// <typeparam name="TIISContainerInstance"></typeparam>
    public class IISContainer<TIISContainerInstance> : IHostContainer<TIISContainerInstance> where TIISContainerInstance : class, IContainerInstance, new()
    {
        /// <summary>
        /// <para>zh-cn:当前宿主机 IP 地址，用于标识 IIS 容器实例所在的主机。</para>
        /// <para>en-us:Current host IP address used to identify the host that owns the IIS container instances.</para>
        /// </summary>
        public IPAddress HostIpAddress { get; set; }

        /// <summary>
        /// <para>zh-cn:宿主机操作系统平台，用于区分 Windows、Linux 等运行环境。</para>
        /// <para>en-us:Host operating system platform used to distinguish runtime environments such as Windows and Linux.</para>
        /// </summary>
        public OSPlatform OSPlatform { get; set; }

        /// <summary>
        /// <para>zh-cn:IIS 容器实例集合，保存本次读取到的所有 IIS 容器运行实例。</para>
        /// <para>en-us:Collection of IIS container instances discovered during the current load operation.</para>
        /// </summary>
        public ConcurrentBag<TIISContainerInstance> Containers { get; set; }

        /// <summary>
        /// <para>zh-cn:容器信息读取时间，用于判断当前缓存数据的新鲜度。</para>
        /// <para>en-us:Container information read time used to determine the freshness of the cached data.</para>
        /// </summary>
        public DateTime ReadTime { get; set; } = DateTime.Now;

        /// <summary>
        /// <para>zh-cn:从容器标准服务中加载 IIS 容器实例，并刷新读取时间。</para>
        /// <para>en-us:Loads IIS container instances from the container standard service and refreshes the read time.</para>
        /// </summary>
        /// <returns>
        /// <para>zh-cn:表示异步加载操作的任务。</para>
        /// <para>en-us:A task that represents the asynchronous load operation.</para>
        /// </returns>
        public async Task Load()
        {
            //这里将会调用IISContainerDependency.QueryAsync方法 该方法是与现在的代码进行解耦 确保后续替换成任意的IIS容器实现都不会出现无法读取的情况
            ConcurrentBag<TIISContainerInstance> Containers = await AppRealization.Container.QueryAsync<TIISContainerInstance>();
            this.Containers = Containers;
            this.HostIpAddress = null;
            this.ReadTime = DateTime.Now;
        }

        /// <summary>
        /// <para>zh-cn:将当前 IIS 容器信息序列化为 JSON 字符串。</para>
        /// <para>en-us:Serializes the current IIS container information to a JSON string.</para>
        /// </summary>
        /// <returns>
        /// <para>zh-cn:当前容器信息的 JSON 表示。</para>
        /// <para>en-us:The JSON representation of the current container information.</para>
        /// </returns>
        public string Serialize()
        {
            return JsonConvert.SerializeObject(this);
        }

        /// <summary>
        /// <para>zh-cn:从 JSON 字符串反序列化 IIS 容器信息。</para>
        /// <para>en-us:Deserializes IIS container information from a JSON string.</para>
        /// </summary>
        /// <param name="Json">
        /// <para>zh-cn:IIS 容器信息 JSON 字符串。</para>
        /// <para>en-us:The JSON string that contains IIS container information.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:反序列化后的 IIS 容器信息实例。</para>
        /// <para>en-us:The deserialized IIS container information instance.</para>
        /// </returns>
        public static IISContainer<TIISContainerInstance> DeSerialize(string Json)
        {
            return JsonConvert.DeserializeObject<IISContainer<TIISContainerInstance>>(Json);

        }
    }
}
