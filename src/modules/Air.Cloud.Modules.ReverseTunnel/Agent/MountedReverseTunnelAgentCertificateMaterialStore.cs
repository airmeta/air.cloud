using Air.Cloud.Modules.ReverseTunnel.Common.Options;

using Microsoft.Extensions.Options;

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Air.Cloud.Modules.ReverseTunnel.Agent;

/// <summary>
/// <para>zh-cn:基于本地挂载文件的 Agent 证书材料存储。该实现只把私钥和证书写到指定路径，不把敏感材料写入配置、数据库或 Consul。</para>
/// <para>en-us:Agent certificate material store backed by local mounted files. This implementation writes private keys and certificates only to configured paths, never to configuration, databases, or Consul.</para>
/// </summary>
public sealed class MountedReverseTunnelAgentCertificateMaterialStore : IReverseTunnelAgentCertificateMaterialStore
{
    private readonly IOptions<ReverseTunnelOptions> options;

    /// <summary>
    /// <para>zh-cn:创建基于挂载文件路径的 Agent 证书材料存储。</para>
    /// <para>en-us:Creates an Agent certificate material store backed by mounted file paths.</para>
    /// </summary>
    /// <param name="options">
    /// <para>zh-cn:包含 Agent 证书、私钥和可信签发者路径的反向隧道配置。</para>
    /// <para>en-us:Reverse tunnel options containing Agent certificate, private-key, and trusted-issuer paths.</para>
    /// </param>
    public MountedReverseTunnelAgentCertificateMaterialStore(IOptions<ReverseTunnelOptions> options)
    {
        this.options = options;
    }

    /// <inheritdoc />
    public byte[] CreateCertificateSigningRequest(string agentId)
    {
        if (string.IsNullOrWhiteSpace(agentId))
        {
            throw new ArgumentException("AgentId is required.", nameof(agentId));
        }

        using var rsa = LoadOrCreatePrivateKey();
        var subject = new X500DistinguishedName($"CN={EscapeDistinguishedNameValue(agentId.Trim())}");
        var request = new CertificateRequest(subject, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return request.CreateSigningRequest();
    }

    /// <inheritdoc />
    public void SaveIssuedCertificate(byte[] certificate, byte[] issuerCertificate)
    {
        if (certificate is null || certificate.Length == 0)
        {
            throw new ArgumentException("Agent certificate is required.", nameof(certificate));
        }

        var agentCertificate = options.Value.AgentCertificate;
        AtomicWriteBytes(agentCertificate.CertificatePath, certificate);

        if (issuerCertificate is { Length: > 0 })
        {
            AtomicWriteBytes(agentCertificate.TrustedIssuerPath, issuerCertificate);
        }
    }

    /// <inheritdoc />
    public X509Certificate2 LoadClientCertificate()
    {
        using var certificate = LoadCertificate();
        using var rsa = LoadPrivateKey();
        using var certificateWithPrivateKey = certificate.CopyWithPrivateKey(rsa);
        var pfx = certificateWithPrivateKey.Export(X509ContentType.Pkcs12);
        return X509CertificateLoader.LoadPkcs12(
            pfx,
            password: null,
            X509KeyStorageFlags.UserKeySet | X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);
    }

    /// <inheritdoc />
    public X509Certificate2 LoadCertificate()
    {
        var path = options.Value.AgentCertificate.CertificatePath;
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            throw new FileNotFoundException("Agent certificate file was not found.", path);
        }

        return X509CertificateLoader.LoadCertificateFromFile(path);
    }

    /// <inheritdoc />
    public bool ShouldRenew(X509Certificate2 certificate, int renewalWindowDays, DateTimeOffset utcNow)
    {
        if (certificate is null)
        {
            throw new ArgumentNullException(nameof(certificate));
        }

        var windowStartUtc = new DateTimeOffset(certificate.NotAfter.ToUniversalTime(), TimeSpan.Zero)
            .AddDays(-Math.Max(1, renewalWindowDays));
        return utcNow >= windowStartUtc;
    }

    /// <inheritdoc />
    public X509Certificate2 LoadTrustedIssuerCertificate()
    {
        var path = options.Value.AgentCertificate.TrustedIssuerPath;
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new InvalidOperationException("ReverseTunnelSettings:AgentCertificate:TrustedIssuerPath is required for Agent enrollment.");
        }

        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Agent trusted issuer certificate was not found.", path);
        }

        return X509CertificateLoader.LoadCertificateFromFile(path);
    }

    private RSA LoadOrCreatePrivateKey()
    {
        var path = options.Value.AgentCertificate.PrivateKeyPath;
        if (File.Exists(path))
        {
            return LoadPrivateKey();
        }

        EnsureParentDirectory(path, "ReverseTunnelSettings:AgentCertificate:PrivateKeyPath");
        var rsa = RSA.Create();
        rsa.KeySize = options.Value.AgentCertificate.PrivateKeySizeBits;
        AtomicWriteText(path, rsa.ExportPkcs8PrivateKeyPem());
        return rsa;
    }

    private RSA LoadPrivateKey()
    {
        var path = options.Value.AgentCertificate.PrivateKeyPath;
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            throw new FileNotFoundException("Agent private key file was not found.", path);
        }

        var rsa = RSA.Create();
        rsa.ImportFromPem(File.ReadAllText(path));
        return rsa;
    }

    private static void AtomicWriteBytes(string path, byte[] content)
    {
        EnsureParentDirectory(path, path);
        var tempPath = CreateTempPath(path);
        try
        {
            File.WriteAllBytes(tempPath, content);
            File.Move(tempPath, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    private static void AtomicWriteText(string path, string content)
    {
        EnsureParentDirectory(path, path);
        var tempPath = CreateTempPath(path);
        try
        {
            File.WriteAllText(tempPath, content);
            File.Move(tempPath, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    private static string CreateTempPath(string path)
    {
        return Path.Combine(Path.GetDirectoryName(path)!, $"{Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp");
    }

    private static void EnsureParentDirectory(string path, string configurationKey)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new InvalidOperationException($"{configurationKey} is required.");
        }

        var directory = Path.GetDirectoryName(path);
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
        {
            throw new DirectoryNotFoundException($"{configurationKey} must be located in an existing mounted directory.");
        }
    }

    private static string EscapeDistinguishedNameValue(string value)
    {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace(",", "\\,", StringComparison.Ordinal)
            .Replace("+", "\\+", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal)
            .Replace("<", "\\<", StringComparison.Ordinal)
            .Replace(">", "\\>", StringComparison.Ordinal)
            .Replace(";", "\\;", StringComparison.Ordinal);
    }
}
