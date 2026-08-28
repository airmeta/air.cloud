using Air.Cloud.Modules.ReverseTunnel.Common.Options;
using Air.Cloud.Modules.ReverseTunnel.Common.Enrollment;

using Microsoft.Extensions.Options;

using System.Security.Cryptography;
using System.Text;

namespace Air.Cloud.Modules.ReverseTunnel.Server;

/// <summary>
/// <para>zh-cn:基于挂载目录的一次性 Enrollment 凭据存储。凭据文件内容为 token 明文，成功使用后默认重命名为 used 文件。</para>
/// <para>en-us:Mounted-directory one-time enrollment credential store. Credential file content is the token text, and successful use renames the file to a used file by default.</para>
/// </summary>
public sealed class MountedReverseTunnelEnrollmentCredentialStore : IReverseTunnelEnrollmentCredentialStore
{
    private readonly IOptions<ReverseTunnelOptions> options;

    /// <summary>
    /// <para>zh-cn:创建基于挂载目录的 Enrollment 凭据存储。</para>
    /// <para>en-us:Creates a mounted-directory enrollment credential store.</para>
    /// </summary>
    /// <param name="options">
    /// <para>zh-cn:包含 Enrollment 挂载目录和文件扩展名的反向隧道选项。</para>
    /// <para>en-us:Reverse tunnel options containing the enrollment mounted directory and file extension.</para>
    /// </param>
    public MountedReverseTunnelEnrollmentCredentialStore(IOptions<ReverseTunnelOptions> options)
    {
        this.options = options;
    }

    /// <inheritdoc />
    public bool TryConsume(string agentId, string credential, out string? failureReason)
    {
        failureReason = null;
        var enrollment = options.Value.Enrollment;
        if (!enrollment.Enabled)
        {
            failureReason = "Reverse tunnel Agent enrollment is not enabled.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(agentId))
        {
            failureReason = "AgentId is required for enrollment.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(credential))
        {
            failureReason = "Enrollment credential is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(enrollment.CredentialDirectory) || !Directory.Exists(enrollment.CredentialDirectory))
        {
            failureReason = "Enrollment credential directory is not mounted.";
            return false;
        }

        var credentialPath = Path.Combine(
            enrollment.CredentialDirectory,
            ReverseTunnelEnrollmentCredentialNames.CreateFileName(agentId, enrollment.CredentialFileExtension));
        if (!File.Exists(credentialPath))
        {
            failureReason = "Enrollment credential was not found or has already been consumed.";
            return false;
        }

        var storedTokenText = File.ReadAllText(credentialPath, Encoding.UTF8).Trim();
        var providedTokenText = credential.Trim();
        try
        {
            var storedToken = ReverseTunnelEnrollmentToken.Parse(storedTokenText);
            var providedToken = ReverseTunnelEnrollmentToken.Parse(providedTokenText);

            if (!string.IsNullOrWhiteSpace(storedToken.AgentId) &&
                !string.Equals(storedToken.AgentId, agentId.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                failureReason = "Enrollment credential is bound to a different AgentId.";
                return false;
            }

            if (!string.IsNullOrWhiteSpace(providedToken.AgentId) &&
                !string.Equals(providedToken.AgentId, agentId.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                failureReason = "Enrollment credential is bound to a different AgentId.";
                return false;
            }

            if (storedToken.ExpiresAtUtc is { } storedExpiresAtUtc &&
                storedExpiresAtUtc <= DateTimeOffset.UtcNow)
            {
                failureReason = "Enrollment credential has expired.";
                return false;
            }

            if (!FixedTimeEquals(storedToken.Credential, providedToken.Credential))
            {
                failureReason = "Enrollment credential is invalid.";
                return false;
            }
        }
        catch (ArgumentException)
        {
            failureReason = "Enrollment credential is invalid.";
            return false;
        }

        if (!enrollment.ConsumeOnUse)
        {
            return true;
        }

        var usedPath = $"{credentialPath}.used-{DateTimeOffset.UtcNow.UtcTicks}";
        try
        {
            File.Move(credentialPath, usedPath);
            return true;
        }
        catch (IOException)
        {
            failureReason = "Enrollment credential was consumed by another request.";
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            failureReason = "Enrollment credential could not be consumed because the mounted directory is not writable.";
            return false;
        }
    }

    private static bool FixedTimeEquals(string expectedCredential, string providedCredential)
    {
        var expectedBytes = Encoding.UTF8.GetBytes(expectedCredential);
        var providedBytes = Encoding.UTF8.GetBytes(providedCredential);
        return expectedBytes.Length == providedBytes.Length
            && CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes);
    }
}
