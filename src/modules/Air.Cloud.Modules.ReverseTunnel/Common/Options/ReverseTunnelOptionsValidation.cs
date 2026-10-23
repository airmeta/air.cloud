using Microsoft.Extensions.Options;
using Air.Cloud.Modules.ReverseTunnel.Common.Routing;

using System.Net;

namespace Air.Cloud.Modules.ReverseTunnel.Common.Options;

/// <summary>
/// <para>zh-cn:反向隧道选项验证器。当前只在模块启用且 TunnelMode=Server 时强制校验 CA 挂载路径和密码环境变量，避免 Agent 配置受到 Server 密钥要求影响。</para>
/// <para>en-us:Options validator for reverse tunnel settings. It enforces CA mounted paths and the password environment variable only when the module is enabled and TunnelMode=Server, so Agent configurations are not affected by Server key requirements.</para>
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
        if (!Enum.IsDefined(options.TunnelMode))
        {
            failures.Add("ReverseTunnelSettings:TunnelMode must be None, Server, or Agent.");
        }

        if (!Enum.IsDefined(options.SessionSelectionStrategy))
        {
            failures.Add("ReverseTunnelSettings:SessionSelectionStrategy must be one of NewestActive, OldestActive, or RoundRobin.");
        }

        if (options.TunnelMode == ReverseTunnelMode.Agent)
        {
            ValidateAgentOptions(options, failures);
        }

        if (options.TunnelMode == ReverseTunnelMode.Server)
        {
            ValidateServerOptions(options, failures);
        }

        ValidateManagementOptions(options.Management, failures);

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static void ValidateManagementOptions(ReverseTunnelManagementOptions? management, ICollection<string> failures)
    {
        if (management?.Enabled != true)
        {
            return;
        }

        if (!Uri.TryCreate(management.BindUrl, UriKind.Absolute, out var uri) || uri.Port <= 0)
        {
            failures.Add("ReverseTunnelSettings:Management:BindUrl must be an absolute URL with a port when management is enabled.");
        }

        if (!management.AllowAnonymousLoopback)
        {
            if (string.IsNullOrWhiteSpace(management.AccessTokenEnvironmentVariable))
            {
                failures.Add("ReverseTunnelSettings:Management:AccessTokenEnvironmentVariable is required when management is enabled.");
            }
            else if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(management.AccessTokenEnvironmentVariable)))
            {
                failures.Add($"Environment variable '{management.AccessTokenEnvironmentVariable}' must contain the AirTunnel management access token.");
            }
        }
    }

    private static void ValidateServerOptions(ReverseTunnelOptions options, ICollection<string> failures)
    {
        var certificateAuthority = options.CertificateAuthority ?? new ReverseTunnelCertificateAuthorityOptions();

        ValidateRequiredMountedFile(certificateAuthority.CertificatePath, "ReverseTunnelSettings:CertificateAuthority:CertificatePath", failures);

        if (string.IsNullOrWhiteSpace(certificateAuthority.CertificatePasswordEnvironmentVariable))
        {
            failures.Add("ReverseTunnelSettings:CertificateAuthority:CertificatePasswordEnvironmentVariable is required when Server mode is enabled.");
        }
        else if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(certificateAuthority.CertificatePasswordEnvironmentVariable)))
        {
            failures.Add($"Environment variable '{certificateAuthority.CertificatePasswordEnvironmentVariable}' must contain the mounted CA PFX password.");
        }

        if (!string.IsNullOrWhiteSpace(certificateAuthority.TrustedIssuerPath))
        {
            ValidateRequiredMountedFile(certificateAuthority.TrustedIssuerPath, "ReverseTunnelSettings:CertificateAuthority:TrustedIssuerPath", failures);
        }

        ValidateServerCertificate(options.ServerCertificate, failures);
        ValidateAgentAuthorizations(options.AgentAuthorizations, failures);
        ValidateTcpListeners(options.Tcp, failures);
        ValidateUdpListeners(options.Udp, failures);
    }

    private static void ValidateServerCertificate(
        ReverseTunnelServerCertificateOptions? certificate,
        ICollection<string> failures)
    {
        if (certificate is null ||
            (string.IsNullOrWhiteSpace(certificate.CommonName) &&
             (certificate.SubjectAlternativeNames is null || certificate.SubjectAlternativeNames.Count == 0)))
        {
            return;
        }

        if (!IsCertificateHostName(certificate.CommonName))
        {
            failures.Add("ReverseTunnelSettings:ServerCertificate:CommonName must be an IP address or DNS name.");
        }

        var names = certificate.SubjectAlternativeNames ?? [];
        if (names.Count == 0)
        {
            failures.Add("ReverseTunnelSettings:ServerCertificate:SubjectAlternativeNames must contain at least one IP address or DNS name.");
            return;
        }

        for (var index = 0; index < names.Count; index++)
        {
            if (!IsCertificateHostName(names[index]))
            {
                failures.Add($"ReverseTunnelSettings:ServerCertificate:SubjectAlternativeNames:{index} must be an IP address or DNS name.");
            }
        }
    }

    private static bool IsCertificateHostName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var trimmed = value.Trim();
        return IPAddress.TryParse(trimmed, out _) ||
            Uri.CheckHostName(trimmed) is UriHostNameType.Dns or UriHostNameType.IPv4 or UriHostNameType.IPv6;
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
            failures.Add("ReverseTunnelSettings:AgentId is required when Agent mode is enabled.");
        }

        if (string.IsNullOrWhiteSpace(options.ServerAddress))
        {
            failures.Add("ReverseTunnelSettings:ServerAddress is required when Agent mode is enabled.");
        }
        else if (!Uri.TryCreate(options.ServerAddress, UriKind.Absolute, out var serverAddress) ||
            (serverAddress.Scheme != Uri.UriSchemeHttp && serverAddress.Scheme != Uri.UriSchemeHttps))
        {
            failures.Add("ReverseTunnelSettings:ServerAddress must be an absolute HTTP or HTTPS URI when Agent mode is enabled.");
        }

        var agentCertificate = options.AgentCertificate ?? new ReverseTunnelAgentCertificateOptions();
        ValidateRequiredPathDirectory(agentCertificate.CertificatePath, "ReverseTunnelSettings:AgentCertificate:CertificatePath", failures);
        ValidateRequiredPathDirectory(agentCertificate.PrivateKeyPath, "ReverseTunnelSettings:AgentCertificate:PrivateKeyPath", failures);
        ValidateRequiredPathDirectory(agentCertificate.TrustedIssuerPath, "ReverseTunnelSettings:AgentCertificate:TrustedIssuerPath", failures);

        ValidateHttpLocalServices(options.LocalServices, failures);
        ValidateTcpLocalServices(options.Tcp, failures);
        ValidateUdpLocalServices(options.Udp, failures);
    }

    private static void ValidateHttpLocalServices(IEnumerable<ReverseTunnelLocalServiceOptions>? services, ICollection<string> failures)
    {
        if (services is null)
        {
            return;
        }

        var index = 0;
        foreach (var service in services)
        {
            if (service is null)
            {
                index++;
                continue;
            }

            if (string.IsNullOrWhiteSpace(service.PublicPath) ||
                string.IsNullOrEmpty(ReverseTunnelServiceKeyFactory.NormalizeHttpPublicPath(service.PublicPath)))
            {
                failures.Add($"ReverseTunnelSettings:LocalServices:{index}:PublicPath must start with '/' and cannot contain a query string or fragment.");
            }
            else if (!string.IsNullOrWhiteSpace(service.PublicPath) && !ReverseTunnelServiceKeyFactory.HasDerivedHttpServiceKey(service))
            {
                failures.Add($"ReverseTunnelSettings:LocalServices:{index}:ServiceKey must be generated from PublicPath.");
            }

            if (!string.IsNullOrEmpty(service.RewritePath) && string.IsNullOrEmpty(ReverseTunnelRouteTemplate.NormalizeRewritePath(service.RewritePath)))
            {
                failures.Add($"ReverseTunnelSettings:LocalServices:{index}:RewritePath must be a valid path template.");
            }

            if (string.IsNullOrWhiteSpace(service.Host))
            {
                failures.Add($"ReverseTunnelSettings:LocalServices:{index}:Host is required for an HTTP tunnel service.");
            }

            if (service.Port is < 1 or > 65_535)
            {
                failures.Add($"ReverseTunnelSettings:LocalServices:{index}:Port must be between 1 and 65535 for an HTTP tunnel service.");
            }

            if (!string.Equals(service.Protocol, "http", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(service.Protocol, "https", StringComparison.OrdinalIgnoreCase))
            {
                failures.Add($"ReverseTunnelSettings:LocalServices:{index}:Protocol must be http or https for an HTTP tunnel service.");
            }

            index++;
        }
    }

    private static void ValidateTcpListeners(ReverseTunnelTcpOptions? tcp, ICollection<string> failures)
    {
        if (tcp?.Enabled != true)
        {
            return;
        }

        for (var index = 0; index < tcp.Listeners.Count; index++)
        {
            var listener = tcp.Listeners[index];
            if (listener is null)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(listener.ServiceKey))
            {
                failures.Add($"ReverseTunnelSettings:Tcp:Listeners:{index}:ServiceKey is required when TCP tunneling is enabled.");
            }

            if (listener.ListenPort <= 0)
            {
                failures.Add($"ReverseTunnelSettings:Tcp:Listeners:{index}:ListenPort must be between 1 and 65535 when TCP tunneling is enabled.");
            }
        }
    }

    private static void ValidateUdpListeners(ReverseTunnelUdpOptions? udp, ICollection<string> failures)
    {
        if (udp?.Enabled != true)
        {
            return;
        }

        for (var index = 0; index < udp.Listeners.Count; index++)
        {
            var listener = udp.Listeners[index];
            if (listener is null)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(listener.ServiceKey))
            {
                failures.Add($"ReverseTunnelSettings:Udp:Listeners:{index}:ServiceKey is required when UDP tunneling is enabled.");
            }

            if (listener.ListenPort <= 0)
            {
                failures.Add($"ReverseTunnelSettings:Udp:Listeners:{index}:ListenPort must be between 1 and 65535 when UDP tunneling is enabled.");
            }
        }
    }

    private static void ValidateTcpLocalServices(ReverseTunnelTcpOptions? tcp, ICollection<string> failures)
    {
        if (tcp?.Enabled != true)
        {
            return;
        }

        for (var index = 0; index < tcp.LocalServices.Count; index++)
        {
            var service = tcp.LocalServices[index];
            if (service is null)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(service.ServiceKey) ||
                (service.ServerListenPort > 0 && !ReverseTunnelServiceKeyFactory.HasDerivedTcpServiceKey(service)))
            {
                failures.Add($"ReverseTunnelSettings:Tcp:LocalServices:{index}:ServiceKey must be generated from the Server listener endpoint when TCP tunneling is enabled.");
            }

            if (string.IsNullOrWhiteSpace(service.Host))
            {
                failures.Add($"ReverseTunnelSettings:Tcp:LocalServices:{index}:Host is required when TCP tunneling is enabled.");
            }

            if (service.Port <= 0)
            {
                failures.Add($"ReverseTunnelSettings:Tcp:LocalServices:{index}:Port must be between 1 and 65535 when TCP tunneling is enabled.");
            }

            if (service.ServerListenPort <= 0 && string.IsNullOrWhiteSpace(service.ServiceKey))
            {
                failures.Add($"ReverseTunnelSettings:Tcp:LocalServices:{index}:ServerListenPort must be between 1 and 65535 when TCP tunneling is enabled.");
            }
        }
    }

    private static void ValidateUdpLocalServices(ReverseTunnelUdpOptions? udp, ICollection<string> failures)
    {
        if (udp?.Enabled != true)
        {
            return;
        }

        for (var index = 0; index < udp.LocalServices.Count; index++)
        {
            var service = udp.LocalServices[index];
            if (service is null)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(service.ServiceKey) ||
                (service.ServerListenPort > 0 && !ReverseTunnelServiceKeyFactory.HasDerivedUdpServiceKey(service)))
            {
                failures.Add($"ReverseTunnelSettings:Udp:LocalServices:{index}:ServiceKey must be generated from the Server listener endpoint when UDP tunneling is enabled.");
            }

            if (string.IsNullOrWhiteSpace(service.Host))
            {
                failures.Add($"ReverseTunnelSettings:Udp:LocalServices:{index}:Host is required when UDP tunneling is enabled.");
            }

            if (service.Port <= 0)
            {
                failures.Add($"ReverseTunnelSettings:Udp:LocalServices:{index}:Port must be between 1 and 65535 when UDP tunneling is enabled.");
            }

            if (service.ServerListenPort <= 0 && string.IsNullOrWhiteSpace(service.ServiceKey))
            {
                failures.Add($"ReverseTunnelSettings:Udp:LocalServices:{index}:ServerListenPort must be between 1 and 65535 when UDP tunneling is enabled.");
            }
        }
    }

    private static void ValidateRequiredMountedFile(string path, string configurationKey, ICollection<string> failures)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            failures.Add($"{configurationKey} is required when Server mode is enabled.");
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
            failures.Add($"{configurationKey} is required when Agent mode is enabled.");
            return;
        }

        var directory = Path.GetDirectoryName(path);
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
        {
            failures.Add($"{configurationKey} must be located in an existing mounted directory when Agent mode is enabled.");
        }
    }
}
