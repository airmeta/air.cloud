using Air.Cloud.Core.App;
using Air.Cloud.Core.Standard.ClairvoyanceStandard;
using Air.Cloud.Modules.Clairvoyance.Dependencies;
using Air.Cloud.Modules.Clairvoyance.Options;

using Microsoft.Extensions.DependencyInjection;

using System.Net.Sockets;

namespace Air.Cloud.Modules.Clairvoyance.Extensions
{
    public static class ClairvoyanceExtensions
    {
        public static void WebInjectClairvoyanceServer(this IServiceCollection services, Action<Socket> ConstractorServerBootstrap)
        {
            services.AddOptions<ClairvoyanceServerOptions>()
               .BindConfiguration("NettySettings")
               .ValidateDataAnnotations()
               .PostConfigure(options =>
               { });
            var options = AppCore.GetOptions<ClairvoyanceServerOptions>();
            ClairvoyanceNettyServerDependency serverDependency = new ClairvoyanceNettyServerDependency();
            var Socket = serverDependency.Create<Socket>(options);
            ConstractorServerBootstrap.Invoke(Socket);
            _ = serverDependency.OnLineAsync();
            //注入服务端
            services.AddSingleton<IClairvoyanceServerStandard>(serverDependency);
        }
        public static void WebInjectClairvoyanceClient(this IServiceCollection services, Action<Socket> ConstractorServerBootstrap)
        {
            services.AddOptions<ClairvoyanceClientOptions>()
              .BindConfiguration("NettySettings")
              .ValidateDataAnnotations()
              .PostConfigure(options =>
              { });
            var options = AppCore.GetOptions<ClairvoyanceClientOptions>();
            IClairvoyanceClientStandard clientDependency = new ClairvoyanceNettyClientDependency();
            ConstractorServerBootstrap.Invoke(clientDependency.Create<Socket>(options));
            _ = clientDependency.OnLineAsync();
            //注入服务端
            services.AddSingleton(clientDependency);
        }
    }
}
