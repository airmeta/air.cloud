using air.gateway.Configuration;
using air.gateway.Options;

using Microsoft.Extensions.Configuration;

namespace Air.Cloud.UnitTest.Servers.Gateway;

public sealed class GatewayOcelotConfigurationLoaderTests
{
    [Fact]
    public void ResolveSource_ShouldUseConsulByDefault()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>());

        var source = GatewayOcelotConfigurationLoader.ResolveSource(configuration);

        Assert.Equal(GatewayOcelotConfigurationSource.Consul, source);
    }

    [Fact]
    public void ResolveSource_ShouldIgnoreCase()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["GatewaySettings:OcelotConfigurationSource"] = "file"
        });

        var source = GatewayOcelotConfigurationLoader.ResolveSource(configuration);

        Assert.Equal(GatewayOcelotConfigurationSource.File, source);
    }

    [Fact]
    public void ResolveSource_ShouldRejectUnsupportedValue()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["GatewaySettings:OcelotConfigurationSource"] = "Database"
        });

        var exception = Assert.Throws<InvalidOperationException>(() => GatewayOcelotConfigurationLoader.ResolveSource(configuration));

        Assert.Contains("GatewaySettings:OcelotConfigurationSource", exception.Message);
        Assert.Contains("Database", exception.Message);
    }

    [Fact]
    public void Load_ShouldUseLocalConfigurationWhenSourceIsFile()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["GatewaySettings:OcelotConfigurationSource"] = "File",
            ["Routes:0:UpstreamPathTemplate"] = "/v1/identity/{everything}",
            ["Routes:0:DownstreamPathTemplate"] = "/v1/identity/{everything}",
            ["Routes:0:DownstreamScheme"] = "http",
            ["Routes:0:DownstreamHostAndPorts:0:Host"] = "127.0.0.1",
            ["Routes:0:DownstreamHostAndPorts:0:Port"] = "5108",
            ["GlobalConfiguration:BaseUrl"] = "http://127.0.0.1:5100"
        });

        var ocelotConfiguration = GatewayOcelotConfigurationLoader.Load(configuration);

        Assert.Equal("/v1/identity/{everything}", ocelotConfiguration["Routes:0:UpstreamPathTemplate"]);
        Assert.Equal("127.0.0.1", ocelotConfiguration["Routes:0:DownstreamHostAndPorts:0:Host"]);
        Assert.Equal("http://127.0.0.1:5100", ocelotConfiguration["GlobalConfiguration:BaseUrl"]);
    }

    private static IConfiguration BuildConfiguration(Dictionary<string, string?> values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }
}
