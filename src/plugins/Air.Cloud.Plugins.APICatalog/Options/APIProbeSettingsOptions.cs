using Microsoft.Extensions.Configuration;

namespace Air.Cloud.Plugins.APICatalog.Options;

[ConfigurationInfo("APIProbeSettings")]
public sealed class APIProbeSettingsOptions : IConfigurableOptions<APIProbeSettingsOptions>
{
    public bool? Enabled { get; set; }

    public bool? EnableAuthorized { get; set; }

    public bool? EnableInternalAccessToken { get; set; }

    public string RoutePrefix { get; set; }

    public string HeaderName { get; set; }

    public string QueryName { get; set; }

    public string[] AccessTokens { get; set; }

    public string DefaultProviderName { get; set; }

    public void PostConfigure(APIProbeSettingsOptions options, IConfiguration configuration)
    {
        options.Enabled ??= AppEnvironment.IsProduction ? false : true;
        options.EnableAuthorized ??= true;
        options.EnableInternalAccessToken ??= true;
        options.RoutePrefix ??= "api-probe";
        options.HeaderName ??= "X-Air-Document-Token";
        options.QueryName ??= "access_token";
        options.AccessTokens ??= Array.Empty<string>();
        options.DefaultProviderName ??= "ApiExplorer";
    }
}
