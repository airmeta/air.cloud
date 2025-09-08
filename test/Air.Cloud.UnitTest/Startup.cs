using Air.Cloud.Core.App;
using Air.Cloud.HostApp.Dependency;
using Air.Cloud.Plugins.Jwt.Options;
using Air.Cloud.Plugins.Jwt;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using System.Reflection;

namespace Air.Cloud.UnitTest
{
    public class Startup
    {
        // 自定义 host 构建
        public void ConfigureHost(IHostBuilder hostBuilder)
        {
            hostBuilder.HostInjectInFile(Assembly.GetAssembly(typeof(Startup)));
        }

        public void ConfigureServices(IServiceCollection services, HostBuilderContext hostBuilderContext)
        {
            services.AddOptions<JWTSettingsOptions>()
                .BindConfiguration("JWTSettings")
                .ValidateDataAnnotations();
        }

        // 可以添加要用到的方法参数，会自动从注册的服务中获取服务实例，类似于 asp.net core 里 Configure 方法
        public void Configure(IServiceProvider applicationServices)
        {
        }
    }
}