using Air.Cloud.Core;
using Air.Cloud.Core.App;
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
    /// <typeparam name="TDockerContainerInstance">
    /// <para>zh-cn:Docker 容器实例类型。</para>
    /// <para>en-us:The Docker container instance type.</para>
    /// </typeparam>
    public class DockerContainer<TDockerContainerInstance> :IHostContainer<TDockerContainerInstance> where TDockerContainerInstance: class,IContainerInstance,new()
    {
        /// <summary>
        /// <para>zh-cn:当前宿主机 IP 地址，用于标识 Docker 容器实例所在的主机。</para>
        /// <para>en-us:Current host IP address used to identify the host that owns the Docker container instances.</para>
        /// </summary>
        public IPAddress HostIpAddress { get; set ; }

        /// <summary>
        /// <para>zh-cn:宿主机操作系统平台，用于区分 Docker 运行环境。</para>
        /// <para>en-us:Host operating system platform used to distinguish the Docker runtime environment.</para>
        /// </summary>
        public OSPlatform OSPlatform { get; set; }

        /// <summary>
        /// <para>zh-cn:Docker 容器实例集合，保存本次读取到的容器运行实例。</para>
        /// <para>en-us:Collection of Docker container instances discovered during the current load operation.</para>
        /// </summary>
        public ConcurrentBag<TDockerContainerInstance> Containers { get; set; }

        /// <summary>
        /// <para>zh-cn:容器信息读取时间，用于判断当前缓存数据的新鲜度。</para>
        /// <para>en-us:Container information read time used to determine the freshness of the cached data.</para>
        /// </summary>
        public DateTime ReadTime { get; set; }

        /// <summary>
        /// <para>zh-cn:从容器标准服务中加载 Docker 容器实例，并刷新读取时间。</para>
        /// <para>en-us:Loads Docker container instances from the container standard service and refreshes the read time.</para>
        /// </summary>
        /// <returns>
        /// <para>zh-cn:表示异步加载操作的任务。</para>
        /// <para>en-us:A task that represents the asynchronous load operation.</para>
        /// </returns>
        public async Task  Load()
        {
            ConcurrentBag<TDockerContainerInstance> Containers= await AppRealization.Container.QueryAsync<TDockerContainerInstance>();
            this.Containers = Containers;
            this.HostIpAddress = null;
            this.ReadTime = DateTime.Now;
        }

        /// <summary>
        /// <para>zh-cn:将当前 Docker 容器信息序列化为 JSON 字符串。</para>
        /// <para>en-us:Serializes the current Docker container information to a JSON string.</para>
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
        /// <para>zh-cn:从 JSON 字符串反序列化 Docker 容器信息。</para>
        /// <para>en-us:Deserializes Docker container information from a JSON string.</para>
        /// </summary>
        /// <param name="Json">
        /// <para>zh-cn:Docker 容器信息 JSON 字符串。</para>
        /// <para>en-us:The JSON string that contains Docker container information.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:反序列化后的 Docker 容器信息实例。</para>
        /// <para>en-us:The deserialized Docker container information instance.</para>
        /// </returns>
        public static DockerContainer<TDockerContainerInstance> DeSerialize(string Json)
        {
            return JsonConvert.DeserializeObject<DockerContainer<TDockerContainerInstance>>(Json);

        }
    }
   
}
