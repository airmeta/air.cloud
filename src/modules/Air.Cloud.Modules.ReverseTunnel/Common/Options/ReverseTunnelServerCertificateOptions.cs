namespace Air.Cloud.Modules.ReverseTunnel.Common.Options;

/// <summary>
/// <para>zh-cn:AirTunnel Server TLS 证书的可迁移身份配置。它只描述证书主题和 SAN，不保存证书、私钥或密码；证书生成脚本可据此为迁移后的主机重新签发 Server 证书。</para>
/// <para>en-us:Portable identity configuration for the AirTunnel Server TLS certificate. It describes only the subject and SAN values, never certificate bytes, private keys, or passwords; certificate-generation tooling can use it to reissue a Server certificate after migration.</para>
/// </summary>
public sealed class ReverseTunnelServerCertificateOptions
{
    /// <summary>
    /// <para>zh-cn:Server 证书的主题名称。通常使用当前 Server 主机 IP 或稳定 DNS 名称。</para>
    /// <para>en-us:Subject name for the Server certificate. This is usually the current Server host IP or a stable DNS name.</para>
    /// </summary>
    public string CommonName { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:证书的 Subject Alternative Name 列表。迁移时只需修改这里的 IP 或 DNS 名称并重新生成证书。</para>
    /// <para>en-us:Subject Alternative Name values for the certificate. During migration, update the IP or DNS names here and regenerate the certificate.</para>
    /// </summary>
    public List<string> SubjectAlternativeNames { get; set; } = [];

    internal void Normalize()
    {
        CommonName = CommonName?.Trim() ?? string.Empty;
        SubjectAlternativeNames = (SubjectAlternativeNames ?? [])
            .Select(value => value?.Trim() ?? string.Empty)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (SubjectAlternativeNames.Count == 0 && !string.IsNullOrWhiteSpace(CommonName))
        {
            SubjectAlternativeNames.Add(CommonName);
        }

        if (string.IsNullOrWhiteSpace(CommonName) && SubjectAlternativeNames.Count > 0)
        {
            CommonName = SubjectAlternativeNames[0];
        }
    }
}
