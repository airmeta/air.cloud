using Air.Cloud.Core.Standard.Container.Model;
using Air.Cloud.Core.Standard.Container;
using Air.Cloud.Core;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using Newtonsoft.Json;

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
