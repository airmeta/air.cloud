using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Air.Cloud.Modules.ReverseTunnel.Common.Enrollment;

/// <summary>
/// <para>zh-cn:AirTunnel Agent 首次注册所需的引导 Token。Token 包含一次性凭据、可选的 Server 地址和服务端 CA 公共证书，客户端可以在没有预置 ca.crt 时建立首个受信 TLS 连接。</para>
/// <para>en-us:Bootstrap token used by an AirTunnel Agent for first enrollment. The token contains a one-time credential, an optional Server address, and the Server CA public certificate so the client can establish its first trusted TLS connection without a pre-mounted ca.crt.</para>
/// </summary>
public sealed class ReverseTunnelEnrollmentToken
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// <para>zh-cn:当前 Token 格式版本。</para>
    /// <para>en-us:Current bootstrap token format version.</para>
    /// </summary>
    [JsonPropertyName("version")]
    public int Version { get; init; } = 1;

    /// <summary>
    /// <para>zh-cn:绑定到指定 Agent 的一次性注册凭据。</para>
    /// <para>en-us:One-time enrollment credential bound to the target Agent.</para>
    /// </summary>
    [JsonPropertyName("credential")]
    public string Credential { get; init; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:Token 绑定的 AgentId。</para>
    /// <para>en-us:AgentId bound to the token.</para>
    /// </summary>
    [JsonPropertyName("agentId")]
    public string AgentId { get; init; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:可选的 AirTunnel Server 地址。为空时仍使用客户端现有配置。</para>
    /// <para>en-us:Optional AirTunnel Server address. The Agent keeps its configured address when this value is empty.</para>
    /// </summary>
    [JsonPropertyName("serverAddress")]
    public string ServerAddress { get; init; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:服务端 CA 公共证书的 Base64 编码 DER 内容。</para>
    /// <para>en-us:Base64-encoded DER content of the Server CA public certificate.</para>
    /// </summary>
    [JsonPropertyName("trustedIssuerCertificate")]
    public string TrustedIssuerCertificate { get; init; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:Token 有效期结束时间，使用 UTC。</para>
    /// <para>en-us:UTC expiration time of the bootstrap token.</para>
    /// </summary>
    [JsonPropertyName("expiresAtUtc")]
    public DateTimeOffset? ExpiresAtUtc { get; init; }

    /// <summary>
    /// <para>zh-cn:把 Token 编码为可写入文件的一行 JSON。返回内容不包含私钥。</para>
    /// <para>en-us:Serializes the token as one-line JSON suitable for a mounted file. The output never contains a private key.</para>
    /// </summary>
    /// <returns>
    /// <para>zh-cn:Token JSON 文本。</para>
    /// <para>en-us:Token JSON text.</para>
    /// </returns>
    public string Serialize()
    {
        return JsonSerializer.Serialize(this, SerializerOptions);
    }

    /// <summary>
    /// <para>zh-cn:解析引导 Token。旧版本纯文本 Token 会作为兼容凭据解析，不提供 CA 引导信息。</para>
    /// <para>en-us:Parses a bootstrap token. Legacy plain-text tokens are parsed as compatible credentials without CA bootstrap material.</para>
    /// </summary>
    /// <param name="value">
    /// <para>zh-cn:从挂载文件读取的 Token 文本。</para>
    /// <para>en-us:Token text read from the mounted file.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:解析后的 Token。</para>
    /// <para>en-us:The parsed token.</para>
    /// </returns>
    /// <exception cref="ArgumentException">
    /// <para>zh-cn:Token 文本为空或 JSON Token 缺少凭据。</para>
    /// <para>en-us:The token is empty or a JSON token does not contain a credential.</para>
    /// </exception>
    public static ReverseTunnelEnrollmentToken Parse(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Enrollment token is required.", nameof(value));
        }

        var normalized = value.Trim();
        if (!normalized.StartsWith('{'))
        {
            return new ReverseTunnelEnrollmentToken
            {
                Credential = normalized
            };
        }

        try
        {
            var token = JsonSerializer.Deserialize<ReverseTunnelEnrollmentToken>(normalized, SerializerOptions);
            if (token is null || string.IsNullOrWhiteSpace(token.Credential))
            {
                throw new ArgumentException("Enrollment token JSON must contain a credential.", nameof(value));
            }

            return new ReverseTunnelEnrollmentToken
            {
                Version = token.Version,
                Credential = token.Credential.Trim(),
                AgentId = token.AgentId?.Trim() ?? string.Empty,
                ServerAddress = token.ServerAddress?.Trim() ?? string.Empty,
                TrustedIssuerCertificate = token.TrustedIssuerCertificate?.Trim() ?? string.Empty,
                ExpiresAtUtc = token.ExpiresAtUtc
            };
        }
        catch (JsonException exception)
        {
            throw new ArgumentException("Enrollment token JSON is invalid.", nameof(value), exception);
        }
    }

    /// <summary>
    /// <para>zh-cn:尝试加载 Token 中携带的服务端 CA 公共证书。无 CA 或内容无效时返回 null。</para>
    /// <para>en-us:Attempts to load the Server CA public certificate carried by the token. Returns null when the token has no CA or the encoded content is invalid.</para>
    /// </summary>
    /// <returns>
    /// <para>zh-cn:服务端 CA 证书，调用方负责释放；没有可用证书时返回 null。</para>
    /// <para>en-us:The Server CA certificate owned by the caller, or null when no usable certificate is present.</para>
    /// </returns>
    public X509Certificate2? LoadTrustedIssuerCertificate()
    {
        if (string.IsNullOrWhiteSpace(TrustedIssuerCertificate))
        {
            return null;
        }

        try
        {
            return X509CertificateLoader.LoadCertificate(Convert.FromBase64String(TrustedIssuerCertificate));
        }
        catch (FormatException exception)
        {
            throw new InvalidOperationException("Enrollment token trusted issuer certificate is not valid Base64.", exception);
        }
        catch (CryptographicException exception)
        {
            throw new InvalidOperationException("Enrollment token trusted issuer certificate is not a valid X.509 certificate.", exception);
        }
    }

    /// <summary>
    /// <para>zh-cn:创建新的服务端引导 Token。只接收 CA 公共证书，禁止传入包含私钥的证书内容。</para>
    /// <para>en-us:Creates a new Server bootstrap token. Only the CA public certificate is accepted; certificate content containing a private key must not be supplied.</para>
    /// </summary>
    /// <param name="agentId">
    /// <para>zh-cn:目标 AgentId。</para>
    /// <para>en-us:Target AgentId.</para>
    /// </param>
    /// <param name="credential">
    /// <para>zh-cn:一次性注册凭据。</para>
    /// <para>en-us:One-time enrollment credential.</para>
    /// </param>
    /// <param name="issuerCertificate">
    /// <para>zh-cn:服务端 CA 公共证书。</para>
    /// <para>en-us:Server CA public certificate.</para>
    /// </param>
    /// <param name="serverAddress">
    /// <para>zh-cn:可选的 Server 地址。</para>
    /// <para>en-us:Optional Server address.</para>
    /// </param>
    /// <param name="expiresAtUtc">
    /// <para>zh-cn:Token 过期时间。</para>
    /// <para>en-us:Token expiration time.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:可序列化的引导 Token。</para>
    /// <para>en-us:A serializable bootstrap token.</para>
    /// </returns>
    public static ReverseTunnelEnrollmentToken Create(
        string agentId,
        string credential,
        X509Certificate2 issuerCertificate,
        string? serverAddress = null,
        DateTimeOffset? expiresAtUtc = null)
    {
        if (string.IsNullOrWhiteSpace(agentId))
        {
            throw new ArgumentException("AgentId is required.", nameof(agentId));
        }

        if (string.IsNullOrWhiteSpace(credential))
        {
            throw new ArgumentException("Enrollment credential is required.", nameof(credential));
        }

        ArgumentNullException.ThrowIfNull(issuerCertificate);
        if (issuerCertificate.HasPrivateKey)
        {
            throw new ArgumentException("Enrollment token must contain a public CA certificate without a private key.", nameof(issuerCertificate));
        }

        return new ReverseTunnelEnrollmentToken
        {
            Version = 1,
            AgentId = agentId.Trim(),
            Credential = credential.Trim(),
            ServerAddress = serverAddress?.Trim() ?? string.Empty,
            TrustedIssuerCertificate = Convert.ToBase64String(issuerCertificate.Export(X509ContentType.Cert)),
            ExpiresAtUtc = expiresAtUtc
        };
    }
}
