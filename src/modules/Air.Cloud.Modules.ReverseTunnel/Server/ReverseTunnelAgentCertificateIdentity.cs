using System.Formats.Asn1;
using System.Security.Cryptography.X509Certificates;

namespace Air.Cloud.Modules.ReverseTunnel.Server;

/// <summary>
/// <para>zh-cn:Agent 证书身份解析工具。优先从 Subject Alternative Name 读取 DNS 或 URI 身份，缺失时回退到 Subject CN。</para>
/// <para>en-us:Agent certificate-identity parser. It prefers DNS or URI identities from Subject Alternative Name and falls back to Subject CN when SAN is absent.</para>
/// </summary>
public static class ReverseTunnelAgentCertificateIdentity
{
    private const string SubjectAlternativeNameOid = "2.5.29.17";
    private const string AgentUriPrefix = "air-cloud-reverse-tunnel://agent/";

    /// <summary>
    /// <para>zh-cn:从客户端证书中解析 AgentId。SAN 中的 DNS 名称直接作为 AgentId；SAN URI 使用 air-cloud-reverse-tunnel://agent/{agentId} 格式；没有 SAN 时使用 Subject CN。</para>
    /// <para>en-us:Parses AgentId from a client certificate. A DNS name in SAN is used directly as AgentId; a SAN URI uses the air-cloud-reverse-tunnel://agent/{agentId} format; when SAN is absent, Subject CN is used.</para>
    /// </summary>
    /// <param name="certificate">
    /// <para>zh-cn:需要解析身份的客户端证书。</para>
    /// <para>en-us:Client certificate from which to parse identity.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:解析出的 AgentId，无法解析时返回空字符串。</para>
    /// <para>en-us:Parsed AgentId, or an empty string when no identity can be parsed.</para>
    /// </returns>
    public static string GetAgentId(X509Certificate2 certificate)
    {
        foreach (var agentId in ReadSubjectAlternativeNames(certificate))
        {
            if (!string.IsNullOrWhiteSpace(agentId))
            {
                return agentId.Trim();
            }
        }

        return certificate.GetNameInfo(X509NameType.SimpleName, false)?.Trim() ?? string.Empty;
    }

    private static IEnumerable<string> ReadSubjectAlternativeNames(X509Certificate2 certificate)
    {
        var extension = certificate.Extensions
            .OfType<X509Extension>()
            .FirstOrDefault(item => item.Oid?.Value == SubjectAlternativeNameOid);
        if (extension is null)
        {
            yield break;
        }

        var reader = new AsnReader(extension.RawData, AsnEncodingRules.DER);
        var sequence = reader.ReadSequence();
        while (sequence.HasData)
        {
            var tag = sequence.PeekTag();
            if (tag.HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 2)))
            {
                yield return sequence.ReadCharacterString(UniversalTagNumber.IA5String, tag);
            }
            else if (tag.HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 6)))
            {
                var uri = sequence.ReadCharacterString(UniversalTagNumber.IA5String, tag);
                if (uri.StartsWith(AgentUriPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    yield return Uri.UnescapeDataString(uri[AgentUriPrefix.Length..]);
                }
            }
            else
            {
                sequence.ReadEncodedValue();
            }
        }
    }
}
