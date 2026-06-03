using Air.Cloud.Core.Plugins.APIProbe;
using Air.Cloud.Plugins.APICatalog.Extensions;
using Air.Cloud.Plugins.APICatalog.Providers;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
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

    [Fact]
    public async Task ApiExplorer_provider_should_consume_standard_endpoint_metadata()
    {
        var apiDescriptions = new[]
        {
            CreateApiDescription("/later", "GET", new APIProbeEndpointMetadata
            {
                GroupName = "Dynamic",
                Tag = "LaterTag",
                Summary = "later summary",
                Description = "later description",
                Order = 20
            }),
            CreateApiDescription("/first", "POST", new APIProbeEndpointMetadata
            {
                GroupName = "Dynamic",
                Tag = "FirstTag",
                Summary = "first summary",
                Description = "first description",
                Order = 1
            })
        };
        var provider = new ApiExplorerAPIProbeProvider(new FakeApiDescriptionGroupCollectionProvider(apiDescriptions));

        var result = await provider.GetDocumentAsync(new APIProbeQuery());

        Assert.Equal(new[] { "Dynamic" }, result.Groups);
        Assert.Equal(new[] { "/first", "/later" }, result.Endpoints.Select(endpoint => endpoint.Path));
        var endpoint = result.Endpoints.First();
        Assert.Equal("Dynamic", endpoint.Group);
        Assert.Equal("FirstTag", endpoint.Tag);
        Assert.Equal("first summary", endpoint.Summary);
        Assert.Equal("first description", endpoint.Description);
        Assert.Equal(1, endpoint.Order);
    }

    [Fact]
    public async Task ApiExplorer_provider_should_filter_by_standard_metadata_group_name()
    {
        var apiDescriptions = new[]
        {
            CreateApiDescription("/dynamic", "GET", new APIProbeEndpointMetadata
            {
                GroupName = "Dynamic",
                Tag = "DynamicTag"
            }),
            CreateApiDescription("/admin", "GET", new APIProbeEndpointMetadata
            {
                GroupName = "Admin",
                Tag = "AdminTag"
            })
        };
        var provider = new ApiExplorerAPIProbeProvider(new FakeApiDescriptionGroupCollectionProvider(apiDescriptions));

        var result = await provider.GetDocumentAsync(new APIProbeQuery { Group = "Admin" });

        var endpoint = Assert.Single(result.Endpoints);
        Assert.Equal("Admin", endpoint.Group);
        Assert.Equal("AdminTag", endpoint.Tag);
        Assert.Equal("/admin", endpoint.Path);
        Assert.Equal(new[] { "Admin" }, result.Groups);
    }

    [Fact]
    public async Task ApiExplorer_provider_should_fallback_when_standard_metadata_is_missing()
    {
        var apiDescription = new ApiDescription
        {
            GroupName = "FallbackGroup",
            HttpMethod = "GET",
            RelativePath = "fallback",
            ActionDescriptor = new ActionDescriptor
            {
                DisplayName = "Fallback endpoint",
                EndpointMetadata = new List<object>()
            }
        };
        var provider = new ApiExplorerAPIProbeProvider(new FakeApiDescriptionGroupCollectionProvider(new[] { apiDescription }));

        var result = await provider.GetDocumentAsync(new APIProbeQuery());

        var endpoint = Assert.Single(result.Endpoints);
        Assert.Equal("FallbackGroup", endpoint.Group);
        Assert.Equal("FallbackGroup", endpoint.Tag);
        Assert.Equal(0, endpoint.Order);
        Assert.Equal("/fallback", endpoint.Path);
    }

    [Fact]
    public async Task ApiExplorer_provider_should_keep_zero_order_as_default()
    {
        var apiDescriptions = new[]
        {
            CreateApiDescription("/zero", "GET", new APIProbeEndpointMetadata
            {
                GroupName = "Dynamic",
                Tag = "ZeroTag"
            }),
            CreateApiDescription("/negative", "GET", new APIProbeEndpointMetadata
            {
                GroupName = "Dynamic",
                Tag = "NegativeTag",
                Order = -1
            })
        };
        var provider = new ApiExplorerAPIProbeProvider(new FakeApiDescriptionGroupCollectionProvider(apiDescriptions));

        var result = await provider.GetDocumentAsync(new APIProbeQuery());

        Assert.Equal(new[] { "/negative", "/zero" }, result.Endpoints.Select(endpoint => endpoint.Path));
        Assert.Equal(-1, result.Endpoints[0].Order);
        Assert.Equal(0, result.Endpoints[1].Order);
    }

    private static ApiDescription CreateApiDescription(string path, string method, APIProbeEndpointMetadata metadata)
    {
        return new ApiDescription
        {
            GroupName = "Fallback",
            HttpMethod = method,
            RelativePath = path.TrimStart('/'),
            ActionDescriptor = new ActionDescriptor
            {
                DisplayName = $"{method} {path}",
                EndpointMetadata = new List<object> { metadata }
            }
        };
    }

    private sealed class FakeApiDescriptionGroupCollectionProvider : IApiDescriptionGroupCollectionProvider
    {
        public FakeApiDescriptionGroupCollectionProvider(IReadOnlyList<ApiDescription> apiDescriptions)
        {
            ApiDescriptionGroups = new ApiDescriptionGroupCollection(
                new[]
                {
                    new ApiDescriptionGroup("Fallback", apiDescriptions)
                },
                1);
        }

        public ApiDescriptionGroupCollection ApiDescriptionGroups { get; }
    }
}
