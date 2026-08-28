namespace Air.Cloud.Modules.ReverseTunnel.Common.Http;

internal static class ReverseTunnelHttpHeaders
{
    public const string GatewayAccessTokenHeaderName = "X-AirTunnel-Gateway-Token";

    public const string GatewayTimeoutSecondsHeaderName = "X-AirTunnel-Timeout-Seconds";

    public static readonly HashSet<string> HopByHopHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Connection",
        "Keep-Alive",
        "Proxy-Authenticate",
        "Proxy-Authorization",
        "TE",
        "Trailer",
        "Transfer-Encoding",
        "Upgrade",
        "Host",
        "Content-Length"
    };
}
