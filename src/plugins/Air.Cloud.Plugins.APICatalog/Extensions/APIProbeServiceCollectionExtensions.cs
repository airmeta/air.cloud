using Air.Cloud.Core.Extensions;
using Air.Cloud.Plugins.APICatalog.Options;
using Air.Cloud.Plugins.APICatalog.Providers;

using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Air.Cloud.Plugins.APICatalog.Extensions;

[IgnoreScanning]
public static class APIProbeServiceCollectionExtensions
{
    public static IServiceCollection AddAPIProbe(this IServiceCollection services)
    {
        services.AddConfigurableOptions<APIProbeSettingsOptions>();
        services.AddRouting();
        services.AddEndpointsApiExplorer();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IAPIProbeProvider, ApiExplorerAPIProbeProvider>());
        return services;
    }
}
