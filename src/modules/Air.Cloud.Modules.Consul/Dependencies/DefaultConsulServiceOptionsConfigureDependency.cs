using Air.Cloud.Core;
using Air.Cloud.Core.App;
using Air.Cloud.Core.Enums;
using Air.Cloud.Core.Extensions;
using Air.Cloud.Modules.Consul.Model;
using Air.Cloud.Modules.Consul.Standard;

namespace Air.Cloud.Modules.Consul.Dependencies
{
    /// <summary>
    /// <para>zh-cn:Consul服务选项配置实现</para>
    /// <para>en-us:Consul service options configure dependency</para>
    /// </summary>
    internal class DefaultConsulServiceOptionsConfigureDependency : IConsulServiceOptionsConfigureStandard
    {
        public ConsulServiceOptions Configure(ConsulServiceOptions options,Action<ConsulServiceOptions> action = null)
        {
            action?.Invoke(options);
            if (options.ConsulAddress.IsNullOrEmpty())
            {
                Console.WriteLine("你需要配置Consul服务运行IP地址");
                throw new Exception("你需要配置Consul服务运行IP地址");
            }
            if (options.ServiceAddress.IsNullOrEmpty())
            {
                Console.WriteLine("你需要配置当前服务运行IP地址");
                throw new Exception("你需要配置当前服务运行IP地址");
            }
            if (options.ServiceName.IsNullOrEmpty()) options.ServiceName = GetDefaultServiceName(options.IsIgnoreServiceNameKey, options.IgnoreKey);
            return options;
        }

        /// <summary>
        /// <para>zh-cn:获取默认服务名称</para>
        /// <para>en-us:Get default service name</para>
        /// </summary>
        /// <param name="IsIgnoreServiceNameKey">
        /// <para>zh-cn:是否忽略启动项关键字</para>
        /// <para>en-us:is Ignore entry key</para>
        /// </param>
        /// <param name="IgnoreKey">
        /// <para>zh-cn:忽略启动项关键字</para>
        /// <para>en-us:Ignore entry key</para>
        /// </param>
        /// <returns></returns>
        public static string GetDefaultServiceName(bool IsIgnoreServiceNameKey = true,string IgnoreKey=null)
        {
            string ProjectName = AppConst.ApplicationName;
            if (AppEnvironment.IsTest) ProjectName = $"{ProjectName}.{AppConst.ENVIRONMENT_TEST_KEY}";
            if (AppConst.EnvironmentStatus.HasValue && AppConst.EnvironmentStatus.Value == EnvironmentEnums.Other)
            {
                //如果是其他环境，则追加环境名称
                string EnvironmentKey=  AppConfigurationLoader.InnerConfiguration[AppConst.ENVIRONMENT];
                ProjectName = $"{ProjectName}.{EnvironmentKey}";
            }
            return (IsIgnoreServiceNameKey && (!IgnoreKey.IsNullOrEmpty()))? ProjectName.Replace(IgnoreKey,string.Empty):ProjectName;
        }
    }
}
