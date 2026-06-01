namespace Air.Cloud.Core.Plugins.APIProbe;

/// <summary>
/// Query options for a normalized API document.
/// </summary>
public sealed class APIProbeQuery
{
    /// <summary>
    /// Gets or sets the requested document group.
    /// </summary>
    public string Group { get; set; }

    /// <summary>
    /// Gets or sets whether schema details should be included.
    /// </summary>
    public bool IncludeSchemas { get; set; } = true;
}

/// <summary>
/// Normalized API document result.
/// </summary>
public sealed class APIProbeResult
{
    /// <summary>
    /// Gets or sets the provider name.
    /// </summary>
    public string ProviderName { get; set; }

    /// <summary>
    /// Gets or sets the application name.
    /// </summary>
    public string ApplicationName { get; set; }

    /// <summary>
    /// Gets or sets the application version.
    /// </summary>
    public string ApplicationVersion { get; set; }

    /// <summary>
    /// Gets or sets the document generation time.
    /// </summary>
    public DateTimeOffset GeneratedAt { get; set; } = DateTimeOffset.Now;

    /// <summary>
    /// Gets or sets the document groups.
    /// </summary>
    public IList<string> Groups { get; set; } = new List<string>();

    /// <summary>
    /// Gets or sets the endpoint list.
    /// </summary>
    public IList<APIProbeEndpoint> Endpoints { get; set; } = new List<APIProbeEndpoint>();
}

/// <summary>
/// Normalized endpoint metadata.
/// </summary>
public sealed class APIProbeEndpoint
{
    public string Id { get; set; }

    public string Group { get; set; }

    public string Tag { get; set; }

    public string Name { get; set; }

    public string Method { get; set; }

    public string Path { get; set; }

    public string Summary { get; set; }

    public string Description { get; set; }

    public bool IsAllowAnonymous { get; set; }

    public bool RequiresAuthorization { get; set; }

    public IList<APIProbeAuthorizeData> AuthorizeDatas { get; set; } = new List<APIProbeAuthorizeData>();

    public APIProbeRequest Request { get; set; } = new APIProbeRequest();

    public IList<APIProbeResponse> Responses { get; set; } = new List<APIProbeResponse>();
}

/// <summary>
/// Normalized authorization metadata.
/// </summary>
public sealed class APIProbeAuthorizeData
{
    public string AuthenticationSchemes { get; set; }

    public string Policy { get; set; }

    public string Roles { get; set; }
}

/// <summary>
/// Normalized request metadata.
/// </summary>
public sealed class APIProbeRequest
{
    public IList<string> ContentTypes { get; set; } = new List<string>();

    public IList<APIProbeParameter> Parameters { get; set; } = new List<APIProbeParameter>();

    public APIProbeSchema Body { get; set; }
}

/// <summary>
/// Normalized response metadata.
/// </summary>
public sealed class APIProbeResponse
{
    public int StatusCode { get; set; }

    public string Description { get; set; }

    public IList<string> ContentTypes { get; set; } = new List<string>();

    public APIProbeSchema Body { get; set; }
}

/// <summary>
/// Normalized request parameter metadata.
/// </summary>
public sealed class APIProbeParameter
{
    public string Name { get; set; }

    public string Source { get; set; }

    public bool Required { get; set; }

    public string Description { get; set; }

    public object DefaultValue { get; set; }

    public APIProbeSchema Schema { get; set; }
}

/// <summary>
/// Normalized schema metadata.
/// </summary>
public sealed class APIProbeSchema
{
    public string Type { get; set; }

    public string Format { get; set; }

    public string TypeName { get; set; }

    public bool Nullable { get; set; }

    public bool IsArray { get; set; }

    public APIProbeSchema Items { get; set; }

    public IList<string> EnumValues { get; set; } = new List<string>();

    public IList<APIProbeParameter> Properties { get; set; } = new List<APIProbeParameter>();
}
