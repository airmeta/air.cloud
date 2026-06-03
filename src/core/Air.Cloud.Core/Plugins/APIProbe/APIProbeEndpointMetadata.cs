namespace Air.Cloud.Core.Plugins.APIProbe;

/// <summary>
/// Represents endpoint metadata that can be consumed by APIProbe providers.
/// </summary>
public sealed class APIProbeEndpointMetadata
{
    /// <summary>
    /// Gets or sets the endpoint document group name.
    /// </summary>
    public string GroupName { get; set; }

    /// <summary>
    /// Gets or sets the endpoint document group collection.
    /// </summary>
    public string[] Groups { get; set; }

    /// <summary>
    /// Gets or sets the endpoint tag.
    /// </summary>
    public string Tag { get; set; }

    /// <summary>
    /// Gets or sets the endpoint summary.
    /// </summary>
    public string Summary { get; set; }

    /// <summary>
    /// Gets or sets the endpoint description.
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Gets or sets the endpoint ordering value.
    /// </summary>
    public int Order { get; set; }
}
