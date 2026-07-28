using Air.Cloud.Modules.ReverseTunnel.Common.Options;
using Air.Cloud.Modules.ReverseTunnel.Extensions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddReverseTunnel();
builder.Services.Configure<ReverseTunnelOptions>(builder.Configuration.GetSection("ReverseTunnelSettings"));

await builder.Build().RunAsync();
