using Air.Cloud.Core;
using Air.Cloud.Core.App;
using Air.Cloud.Modules.Consul.Model;
using Air.Cloud.Modules.Consul.Resolver;
using Air.Cloud.Modules.Consul.Service;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using System.Diagnostics;
using System.Reflection;

namespace Air.Cloud.Modules.Consul.Extensions
{
    public static class ConsulRegistrationExtensions
    {
        /// <summary>
        /// 接入注册中心
        /// </summary>
        /// <param name="app"></param>
        /// <param name="assembly"></param>
        /// <remarks>
        /// IIS模式目前只需要一个注册中心地址即可,当前服务地址信息为即时发现
        /// Docker模式目前需要配置注册中心地址以及当前服务的地址信息
        /// </remarks>
        /// <returns></returns>
        public static IApplicationBuilder UseConsul(this IApplicationBuilder app, Assembly assembly = null)
        {
            var serviceOptions =AppCore.Configuration.GetConfig<ConsulServiceOptions>();
            //开发环境剔除此参数
            if (AppEnvironment.IsDevelopment) return app;
            AppRuntimeOptions? info = AppCore.GetOptions<AppRuntimeOptions>();
            serviceOptions = ConsulServiceOptions.GetConsulServiceOptions(info, serviceOptions);
            #region 初始化一些参数 如果可以自动获取 将忽略配置文件
            // 服务ID为当前应用程序唯一标识
            serviceOptions.ServiceId = serviceOptions.ServiceId ?? AppRealization.PID.Get();
            //获取当前程序的注册名
            serviceOptions.ServiceName = app.GetCurrentProjectConsulServiceName(serviceOptions.ServiceName, assembly);
            #endregion
            var ConsulHelpers = new ConsulOperatorHelper(serviceOptions.ConsulAddress);
            var result = ConsulHelpers.InitConsulRegistration(serviceOptions);
            if (!result.Item1) return app;
            #region 注册服务
            ConsulServerCenterDependency dependency = new ConsulServerCenterDependency();
            var r = dependency.Register(new ConsulServerCenterServiceRegisterOptions()
            {
                ServiceAddress = serviceOptions.ServiceAddress,
                ServiceName = serviceOptions.ServiceName,
                ServiceKey = serviceOptions.ServiceId,
                HealthCheckTimeStep = new TimeSpan(0, 0, 10),
                HealthCheckRoute = serviceOptions.HealthCheck,
                Timeout = new TimeSpan(0, 0, 5),
                DeregisterCriticalServiceAfter = new TimeSpan(0, 1, 0)
            }).Result;
            // 获取主机生命周期管理接口
            var lifetime = app.ApplicationServices.GetRequiredService<IHostApplicationLifetime>();
            // 应用程序终止时，注销服务
            lifetime.ApplicationStopping.Register(() =>
            {
                ConsulHelpers.GetConsulClient().Agent.ServiceDeregister(serviceOptions.ServiceId).Wait();
            });

            lifetime.ApplicationStarted.Register(() =>
            {
                //开始进行初次健康检查
                using (HttpClient client = new HttpClient())
                {
                    Uri uris = new Uri(new Uri(serviceOptions.ServiceAddress), serviceOptions.HealthCheck);
                    _ = client.GetAsync(uris).Result;
                }
            });
            #endregion
            app.UseHealthChecks(serviceOptions.HealthCheck);
            return app;
        }

        public static void AddConulService(this IServiceCollection services)
        {
            //开发环境剔除此参数
            if (AppEnvironment.IsDevelopment) return;
            services.AddHealthChecks();
        }
    }
}

