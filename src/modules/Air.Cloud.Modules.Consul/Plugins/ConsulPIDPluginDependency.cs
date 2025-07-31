using Air.Cloud.Core.App;
using Air.Cloud.Core.Extensions;
using Air.Cloud.Core.Plugins.PID;
using Air.Cloud.Core.Plugins.Security.MD5;
using Air.Cloud.Modules.Consul.Model;
using Air.Cloud.Modules.Consul.Service;

using Microsoft.Extensions.Configuration;

using System;

namespace Air.Cloud.Modules.Consul.Plugins
{
    /// <summary>
    /// <para>zh-cn:Consul PID 插件依赖</para>
    /// <para>en-us:Consul PID plugin dependency</para>
    /// </summary>
    /// <remarks>
    ///  <para>zh-cn:重写PID生成规则,确保在微服务环境下的PID不会生成相同PID的结果</para>
    ///  <para>en-us:Override the PID generation rules to ensure that the PID in a microservice environment does not generate the same PID result</para>
    /// </remarks>
    public class ConsulPIDPluginDependency : IPIDPlugin
    {
        private static object locker = new object();
        private static string SPID = null;

        /// <inheritdoc/>
        public string Set(string PID = null)
        {
            //此处放置同步执行的代码
            if (!File.Exists(IPIDPlugin.StartPath))
            {
                File.Create(IPIDPlugin.StartPath).Close();
            }
            var serviceOptions = AppConfigurationLoader.InnerConfiguration.GetConfig<ConsulServiceOptions>();
            if (serviceOptions == null)
            {
                throw new ArgumentNullException("Consul服务参数缺失,无法完成启动");
            }
            PID = PID ?? MD5Encryption.GetMd5By32(IPIDPlugin.StartPath + serviceOptions?.ServiceAddress);
            using (StreamWriter file = new StreamWriter(IPIDPlugin.StartPath))
            {
                file.Write(PID);
                file.Close();
            }
            SPID = PID;
            return PID;
        }
        /// <inheritdoc/>
        public string Get()
        {
            lock (locker)
            {
                if (SPID.IsNullOrEmpty())
                {
                    return Set();
                }
                else
                {
                    return SPID;
                }
            }
        }
    }
}
