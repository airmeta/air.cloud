namespace Air.Cloud.Core.Plugins.APIProbe;

/// <summary>
/// Provides a normalized API document that can be produced by different optional plugins.
/// </summary>
public interface IAPIProbeProvider
{
    /// <summary>
    /// Gets the provider name used for switching document implementations.
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Gets the normalized API document.
    /// </summary>
    /// <param name="query">Document query options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Normalized API document.</returns>
    Task<APIProbeResult> GetDocumentAsync(APIProbeQuery query, CancellationToken cancellationToken = default);
}
