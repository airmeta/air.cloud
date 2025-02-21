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
using Air.Cloud.Core.Standard.Container;
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
        public IPAddress HostIpAddress { get; set; }

        public OSPlatform OSPlatform { get; set; }

        public ConcurrentBag<TIISContainerInstance> Containers { get; set; }

        public DateTime ReadTime { get; set; } = DateTime.Now;

        public async Task Load()
        {
            //这里将会调用IISContainerDependency.QueryAsync方法 该方法是与现在的代码进行解耦 确保后续替换成任意的IIS容器实现都不会出现无法读取的情况
            ConcurrentBag<TIISContainerInstance> Containers = await AppRealization.Container.QueryAsync<TIISContainerInstance>();
            this.Containers = Containers;
            this.HostIpAddress = null;
            this.ReadTime = DateTime.Now;
        }

        public string Serialize()
        {
            return JsonConvert.SerializeObject(this);
        }
        public static IISContainer<TIISContainerInstance> DeSerialize(string Json)
        {
            return JsonConvert.DeserializeObject<IISContainer<TIISContainerInstance>>(Json);

        }
    }
}
