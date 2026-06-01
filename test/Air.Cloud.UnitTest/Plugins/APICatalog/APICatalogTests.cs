using Air.Cloud.Core.Plugins.APIProbe;
using Air.Cloud.Plugins.APICatalog.Extensions;
using Air.Cloud.Plugins.APICatalog.Providers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Air.Cloud.UnitTest.Plugins.APICatalog;

public class APICatalogTests
{
    [Fact]
    public void AddAPIProbe_should_register_ApiExplorer_provider_as_singleton()
    {
        var services = new ServiceCollection();

        services.AddAPIProbe();

        var descriptor = services
            .FirstOrDefault(sd => sd.ServiceType == typeof(IAPIProbeProvider)
                                  && sd.ImplementationType == typeof(ApiExplorerAPIProbeProvider));

        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Singleton, descriptor!.Lifetime);
    }

    [Fact]
    public void AddAPIProbe_should_be_safe_when_called_multiple_times()
    {
        var services = new ServiceCollection();

        services.AddAPIProbe();
        services.AddAPIProbe();

        var providerDescriptors = services
            .Where(sd => sd.ServiceType == typeof(IAPIProbeProvider)
                         && sd.ImplementationType == typeof(ApiExplorerAPIProbeProvider))
            .ToArray();

        Assert.Single(providerDescriptors);
        Assert.Contains(services, sd => sd.ServiceType == typeof(EndpointDataSource));
    }
}
