using System.Security.Cryptography;
using System.Text;

namespace Air.Cloud.Modules.ReverseTunnel.Server;

/// <summary>
/// <para>zh-cn:Enrollment 凭据文件名工具。文件名基于 AgentId 的 SHA-256 摘要生成，避免把 AgentId 中的特殊字符直接用于路径。</para>
/// <para>en-us:Enrollment credential file-name helper. File names are generated from the SHA-256 digest of AgentId so special characters in AgentId are not used directly in paths.</para>
/// </summary>
public static class ReverseTunnelEnrollmentCredentialNames
{
    /// <summary>
    /// <para>zh-cn:根据 AgentId 和扩展名生成一次性凭据文件名。AgentId 会先裁剪空白并转换为小写不变区域字符串。</para>
    /// <para>en-us:Builds a one-time credential file name from AgentId and extension. AgentId is trimmed and converted to lowercase invariant text before hashing.</para>
    /// </summary>
    /// <param name="agentId">
    /// <para>zh-cn:Agent 稳定身份。</para>
    /// <para>en-us:Stable Agent identity.</para>
    /// </param>
    /// <param name="extension">
    /// <para>zh-cn:文件扩展名；为空时使用 .token。</para>
    /// <para>en-us:File extension; .token is used when it is empty.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:可安全拼接到凭据目录下的文件名。</para>
    /// <para>en-us:File name safe to combine under the credential directory.</para>
    /// </returns>
    /// <exception cref="ArgumentException">
    /// <para>zh-cn:AgentId 为空。</para>
    /// <para>en-us:AgentId is empty.</para>
    /// </exception>
    public static string CreateFileName(string agentId, string? extension)
    {
        if (string.IsNullOrWhiteSpace(agentId))
        {
            throw new ArgumentException("AgentId is required.", nameof(agentId));
        }

        var normalizedExtension = string.IsNullOrWhiteSpace(extension) ? ".token" : extension.Trim();
        if (!normalizedExtension.StartsWith('.'))
        {
            normalizedExtension = $".{normalizedExtension}";
        }

        var normalizedAgentId = agentId.Trim().ToLowerInvariant();
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(normalizedAgentId));
        return $"{Convert.ToHexString(hash).ToLowerInvariant()}{normalizedExtension}";
    }
}
