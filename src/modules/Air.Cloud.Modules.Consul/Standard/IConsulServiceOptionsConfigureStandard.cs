using Air.Cloud.Core.Standard;
using Air.Cloud.Modules.Consul.Model;

namespace Air.Cloud.Modules.Consul.Standard
{

    /// <summary>
    /// <para>zh-cn:Consul服务选项配置标准</para>
    /// <para>en-us:Consul service options configure standard</para>
    /// </summary>
    public interface IConsulServiceOptionsConfigureStandard:IStandard
    {
        /// <summary>
        /// <para>zh-cn:配置Consul选项</para>
        /// </summary>
        /// <param name="options">配置项</param>
        /// <param name="action">配置动作</param>
        /// <returns></returns>
        public ConsulServiceOptions Configure(ConsulServiceOptions options, Action<ConsulServiceOptions> action=null);
    }
}
