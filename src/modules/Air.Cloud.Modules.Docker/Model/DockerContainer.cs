using Air.Cloud.Core;
using Air.Cloud.Core.App;
using Air.Cloud.Core.Standard.Container;
using Air.Cloud.Core.Standard.Container.Model;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System.Collections.Concurrent;
using System.Net;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Json;
using System.Text;

namespace Air.Cloud.Modules.Docker.Model
{
    /// <summary>
    /// <para>zh-cn:Docker 容器信息</para>
    /// <para>en-us:Docker container runtime information</para>
    /// </summary>
    public class DockerContainer<TDockerContainerInstance> :IHostContainer<TDockerContainerInstance> where TDockerContainerInstance: class,IContainerInstance,new()
    {
        public IPAddress HostIpAddress { get; set ; }

        public OSPlatform OSPlatform { get; set; }

        public ConcurrentBag<TDockerContainerInstance> Containers { get; set; }

        public DateTime ReadTime { get; set; }

        public async Task  Load()
        {
            ConcurrentBag<TDockerContainerInstance> Containers= await AppRealization.Container.QueryAsync<TDockerContainerInstance>();
            this.Containers = Containers;
            this.HostIpAddress = null;
            this.ReadTime = DateTime.Now;
        }

        public string Serialize()
        {
            return JsonConvert.SerializeObject(this);
        }
        public static DockerContainer<TDockerContainerInstance> DeSerialize(string Json)
        {
            return JsonConvert.DeserializeObject<DockerContainer<TDockerContainerInstance>>(Json);

        }
    }
   
}
