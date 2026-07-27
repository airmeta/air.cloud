using Microsoft.Extensions.Options;

namespace Air.Cloud.Modules.ReverseTunnel.Common.Options;

/// <summary>
/// <para>zh-cn:反向隧道选项验证器。当前只在模块启用且进程包含 Server 角色时强制校验 CA 挂载路径和密码环境变量，避免默认禁用或 Agent-only 配置受到 Server 密钥要求影响。</para>
/// <para>en-us:Options validator for reverse tunnel settings. It enforces CA mounted paths and the password environment variable only when the module is enabled and the process includes the Server role, so disabled or Agent-only configurations are not affected by Server key requirements.</para>
/// </summary>
public sealed class ReverseTunnelOptionsValidation : IValidateOptions<ReverseTunnelOptions>
{
    /// <summary>
    /// <para>zh-cn:验证反向隧道配置。该方法不读取数据库或普通配置中的密钥材料，只检查挂载文件是否存在以及密码环境变量是否有值。</para>
    /// <para>en-us:Validates reverse tunnel configuration. The method does not read key material from a database or ordinary configuration; it checks only that mounted files exist and the password environment variable has a value.</para>
    /// </summary>
    /// <param name="name">
    /// <para>zh-cn:选项实例名称，当前模块使用默认实例。</para>
    /// <para>en-us:Options instance name; the module currently uses the default instance.</para>
    /// </param>
    /// <param name="options">
    /// <para>zh-cn:需要验证的反向隧道选项。</para>
    /// <para>en-us:Reverse tunnel options to validate.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:验证成功或包含确定性错误信息的失败结果。</para>
    /// <para>en-us:A success result or a failure result with deterministic error messages.</para>
    /// </returns>
    public ValidateOptionsResult Validate(string? name, ReverseTunnelOptions options)
    {
        if (options is null)
        {
            return ValidateOptionsResult.Fail("ReverseTunnelSettings is required.");
        }

        if (!options.Enabled)
        {
            return ValidateOptionsResult.Success;
        }

        var failures = new List<string>();
        if (!Enum.IsDefined(options.SessionSelectionStrategy))
        {
            failures.Add("ReverseTunnelSettings:SessionSelectionStrategy must be one of NewestActive, OldestActive, or RoundRobin.");
        }

        if (options.Roles.HasFlag(ReverseTunnelRole.Agent))
        {
            ValidateAgentOptions(options, failures);
        }

        if (options.Roles.HasFlag(ReverseTunnelRole.Server))
        {
            ValidateServerOptions(options, failures);
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static void ValidateServerOptions(ReverseTunnelOptions options, ICollection<string> failures)
    {
        var certificateAuthority = options.CertificateAuthority ?? new ReverseTunnelCertificateAuthorityOptions();

        ValidateRequiredMountedFile(certificateAuthority.CertificatePath, "ReverseTunnelSettings:CertificateAuthority:CertificatePath", failures);

        if (string.IsNullOrWhiteSpace(certificateAuthority.CertificatePasswordEnvironmentVariable))
        {
            failures.Add("ReverseTunnelSettings:CertificateAuthority:CertificatePasswordEnvironmentVariable is required when the Server role is enabled.");
        }
        else if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(certificateAuthority.CertificatePasswordEnvironmentVariable)))
        {
            failures.Add($"Environment variable '{certificateAuthority.CertificatePasswordEnvironmentVariable}' must contain the mounted CA PFX password.");
        }

        if (!string.IsNullOrWhiteSpace(certificateAuthority.TrustedIssuerPath))
        {
            ValidateRequiredMountedFile(certificateAuthority.TrustedIssuerPath, "ReverseTunnelSettings:CertificateAuthority:TrustedIssuerPath", failures);
        }

        var enrollment = options.Enrollment ?? new ReverseTunnelEnrollmentOptions();
        if (enrollment.Enabled)
        {
            if (string.IsNullOrWhiteSpace(enrollment.CredentialDirectory))
            {
                failures.Add("ReverseTunnelSettings:Enrollment:CredentialDirectory is required when Server enrollment is enabled.");
            }
            else if (!Directory.Exists(enrollment.CredentialDirectory))
            {
                failures.Add("ReverseTunnelSettings:Enrollment:CredentialDirectory must point to an existing mounted directory.");
            }
        }

        ValidateAgentAuthorizations(options.AgentAuthorizations, failures);
    }

    private static void ValidateAgentAuthorizations(IEnumerable<ReverseTunnelAgentAuthorizationOptions>? authorizations, ICollection<string> failures)
    {
        if (authorizations is null)
        {
            return;
        }

        var agentIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var index = 0;
        foreach (var authorization in authorizations)
        {
            if (authorization is null)
            {
                index++;
                continue;
            }

            var agentId = authorization.AgentId?.Trim();
            if (string.IsNullOrWhiteSpace(agentId))
            {
                failures.Add($"ReverseTunnelSettings:AgentAuthorizations:{index}:AgentId is required when an authorization entry is configured.");
            }
            else if (!agentIds.Add(agentId))
            {
                failures.Add($"ReverseTunnelSettings:AgentAuthorizations contains duplicate AgentId '{agentId}'.");
            }

            var allowedServiceKeys = authorization.AllowedServiceKeys?
                .Where(key => !string.IsNullOrWhiteSpace(key))
                .Select(key => key.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray() ?? [];
            if (allowedServiceKeys.Length == 0)
            {
                failures.Add($"ReverseTunnelSettings:AgentAuthorizations:{index}:AllowedServiceKeys must contain at least one ServiceKey when an authorization entry is configured.");
            }

            index++;
        }
    }

    private static void ValidateAgentOptions(ReverseTunnelOptions options, ICollection<string> failures)
    {
        if (string.IsNullOrWhiteSpace(options.AgentId))
        {
            failures.Add("ReverseTunnelSettings:AgentId is required when the Agent role is enabled.");
        }

        if (string.IsNullOrWhiteSpace(options.ServerAddress))
        {
            failures.Add("ReverseTunnelSettings:ServerAddress is required when the Agent role is enabled.");
        }
        else if (!Uri.TryCreate(options.ServerAddress, UriKind.Absolute, out var serverAddress) ||
            (serverAddress.Scheme != Uri.UriSchemeHttp && serverAddress.Scheme != Uri.UriSchemeHttps))
        {
            failures.Add("ReverseTunnelSettings:ServerAddress must be an absolute HTTP or HTTPS URI when the Agent role is enabled.");
        }

        var agentCertificate = options.AgentCertificate ?? new ReverseTunnelAgentCertificateOptions();
        ValidateRequiredPathDirectory(agentCertificate.CertificatePath, "ReverseTunnelSettings:AgentCertificate:CertificatePath", failures);
        ValidateRequiredPathDirectory(agentCertificate.PrivateKeyPath, "ReverseTunnelSettings:AgentCertificate:PrivateKeyPath", failures);
        ValidateRequiredPathDirectory(agentCertificate.TrustedIssuerPath, "ReverseTunnelSettings:AgentCertificate:TrustedIssuerPath", failures);

        if (!string.IsNullOrWhiteSpace(agentCertificate.EnrollmentCredentialPath) && !File.Exists(agentCertificate.EnrollmentCredentialPath))
        {
            failures.Add("ReverseTunnelSettings:AgentCertificate:EnrollmentCredentialPath must point to an existing mounted file when configured.");
        }
    }

    private static void ValidateRequiredMountedFile(string path, string configurationKey, ICollection<string> failures)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            failures.Add($"{configurationKey} is required when the Server role is enabled.");
            return;
        }

        if (!File.Exists(path))
        {
            failures.Add($"{configurationKey} must point to an existing mounted file.");
        }
    }

    private static void ValidateRequiredPathDirectory(string path, string configurationKey, ICollection<string> failures)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            failures.Add($"{configurationKey} is required when the Agent role is enabled.");
            return;
        }

        var directory = Path.GetDirectoryName(path);
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
        {
            failures.Add($"{configurationKey} must be located in an existing mounted directory when the Agent role is enabled.");
        }
    }
}
