using System.Collections.Concurrent;

namespace Air.Cloud.Modules.ReverseTunnel.Server;

/// <summary>
/// <para>zh-cn:线程安全的内存证书状态注册表。该实现只适合单 Server 进程或测试环境，多 Server 部署需要替换为共享状态源。</para>
/// <para>en-us:Thread-safe in-memory certificate-status registry. This implementation is suitable only for a single Server process or tests; multi-Server deployments need a shared status source.</para>
/// </summary>
public sealed class InMemoryReverseTunnelCertificateStatusRegistry : IReverseTunnelCertificateStatusRegistry
{
    private readonly ConcurrentDictionary<string, byte> revokedCertificates = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// <para>zh-cn:记录一个已吊销证书。issuerThumbprint 或 serialNumber 为空时抛出异常，避免生成不可匹配的吊销键。</para>
    /// <para>en-us:Records a revoked certificate. The method throws when issuerThumbprint or serialNumber is empty to avoid creating a revocation key that cannot be matched later.</para>
    /// </summary>
    /// <param name="issuerThumbprint">
    /// <para>zh-cn:签发 CA 证书指纹。</para>
    /// <para>en-us:Issuer CA certificate thumbprint.</para>
    /// </param>
    /// <param name="serialNumber">
    /// <para>zh-cn:Agent 证书序列号。</para>
    /// <para>en-us:Agent certificate serial number.</para>
    /// </param>
    /// <exception cref="ArgumentException">
    /// <para>zh-cn:issuerThumbprint 或 serialNumber 为空。</para>
    /// <para>en-us:issuerThumbprint or serialNumber is empty.</para>
    /// </exception>
    public void Revoke(string issuerThumbprint, string serialNumber)
    {
        revokedCertificates[CreateKey(issuerThumbprint, serialNumber)] = 0;
    }

    /// <inheritdoc />
    public bool IsRevoked(string issuerThumbprint, string serialNumber)
    {
        return revokedCertificates.ContainsKey(CreateKey(issuerThumbprint, serialNumber));
    }

    private static string CreateKey(string issuerThumbprint, string serialNumber)
    {
        if (string.IsNullOrWhiteSpace(issuerThumbprint))
        {
            throw new ArgumentException("Issuer thumbprint is required.", nameof(issuerThumbprint));
        }

        if (string.IsNullOrWhiteSpace(serialNumber))
        {
            throw new ArgumentException("Certificate serial number is required.", nameof(serialNumber));
        }

        return $"{Normalize(issuerThumbprint)}:{Normalize(serialNumber)}";
    }

    private static string Normalize(string value)
    {
        return value.Replace(" ", string.Empty, StringComparison.Ordinal).Trim().ToUpperInvariant();
    }
}
