using Air.Cloud.Core.App;
using Air.Cloud.HostApp.Dependency;
using Air.Cloud.Plugins.Jwt.Options;
using Air.Cloud.Plugins.Jwt;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using System.Reflection;

namespace Air.Cloud.UnitTest
{
    /// <summary>
    /// <para>zh-cn:unittest 项目的启动配置类。</para>
    /// <para>en-us:Startup configuration class for the unittest project.</para>
    /// </summary>
    public class Startup
    {
        /// <summary>
        /// <para>zh-cn:配置测试宿主构建器并加载当前程序集中的宿主注入配置。</para>
        /// <para>en-us:Configures the test host builder and loads host injection settings from the current assembly.</para>
        /// </summary>
        /// <param name="hostBuilder">
        /// <para>zh-cn:用于构建测试宿主的主机构建器。</para>
        /// <para>en-us:The host builder used to construct the test host.</para>
        /// </param>
        public void ConfigureHost(IHostBuilder hostBuilder)
        {
            hostBuilder.HostInjectInFile(Assembly.GetAssembly(typeof(Startup)));
        }

        /// <summary>
        /// <para>zh-cn:注册 unittest 所需的服务与选项配置。</para>
        /// <para>en-us:Registers the services and option bindings required by the unittest project.</para>
        /// </summary>
        /// <param name="services">
        /// <para>zh-cn:服务注册集合。</para>
        /// <para>en-us:The service collection used for dependency registration.</para>
        /// </param>
        /// <param name="hostBuilderContext">
        /// <para>zh-cn:提供宿主构建上下文和配置访问能力。</para>
        /// <para>en-us:Provides host builder context and access to configuration sources.</para>
        /// </param>
        public void ConfigureServices(IServiceCollection services, HostBuilderContext hostBuilderContext)
        {
            services.AddOptions<JWTSettingsOptions>()
                .BindConfiguration("JWTSettings")
                .ValidateDataAnnotations();
        }

        /// <summary>
        /// <para>zh-cn:在测试宿主启动完成后执行额外初始化逻辑。</para>
        /// <para>en-us:Executes additional initialization logic after the test host has been built.</para>
        /// </summary>
        /// <param name="applicationServices">
        /// <para>zh-cn:测试宿主创建的服务提供程序。</para>
        /// <para>en-us:The service provider created by the test host.</para>
        /// </param>
        public void Configure(IServiceProvider applicationServices)
        {
        }
    }
}
