using Air.Cloud.Modules.ReverseTunnel.Common.Options;
using Air.Cloud.Modules.ReverseTunnel.Common.Runtime;
using Air.Cloud.Modules.ReverseTunnel.Server;

using System.IO.Compression;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Air.Cloud.Modules.ReverseTunnel.Management;

/// <summary>
/// <para>zh-cn:基于已保存 Agent 配置和 Server CA 公钥生成部署 ZIP。导出过程明确排除 CA 私钥、Agent 私钥和 Agent 客户端证书。</para>
/// <para>en-us:Generates a deployment ZIP from a saved Agent configuration and the Server CA public certificate. The export explicitly excludes the CA private key, Agent private key, and Agent client certificate.</para>
/// </summary>
public sealed class ReverseTunnelAgentPackageExporter : IReverseTunnelAgentPackageExporter
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly IReverseTunnelDesiredAgentConfigurationStore desiredConfigurations;
    private readonly IReverseTunnelCertificateAuthority certificateAuthority;

    /// <summary>
    /// <para>zh-cn:创建 Agent 配置包导出器。</para>
    /// <para>en-us:Creates the Agent package exporter.</para>
    /// </summary>
    public ReverseTunnelAgentPackageExporter(
        IReverseTunnelDesiredAgentConfigurationStore desiredConfigurations,
        IReverseTunnelCertificateAuthority certificateAuthority)
    {
        this.desiredConfigurations = desiredConfigurations;
        this.certificateAuthority = certificateAuthority;
    }

    /// <inheritdoc />
    public Task<ReverseTunnelAgentPackage> CreateAsync(string agentId, CancellationToken cancellationToken = default)
        => CreateAsync(agentId, null, cancellationToken);

    /// <inheritdoc />
    public async Task<ReverseTunnelAgentPackage> CreateAsync(
        string agentId,
        ReverseTunnelAgentPackageExportRequest? request,
        CancellationToken cancellationToken = default)
    {
        var normalizedAgentId = agentId?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedAgentId))
        {
            throw new ArgumentException("AgentId is required.", nameof(agentId));
        }

        var snapshot = await desiredConfigurations.GetAsync(normalizedAgentId, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException("No desired configuration is stored for the Agent.");

        byte[] caCertificate;
        using (var issuer = certificateAuthority.LoadIssuerCertificate())
        {
            caCertificate = issuer.Export(X509ContentType.Cert);
        }

        var exportRequest = NormalizeRequest(request, snapshot.AgentId);
        var configuration = ReverseTunnelOptionsCloner.Serialize(snapshot.Configuration);
        var readme = CreateReadme(snapshot.AgentId);
        var compose = CreateCompose(exportRequest);
        await using var output = new MemoryStream();
        using (var archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            WriteEntry(archive, "config/00-airtunnel-agent.json", Encoding.UTF8.GetBytes(configuration));
            WriteEntry(archive, "data/certs/ca.crt", caCertificate);
            WriteEntry(archive, "docker-compose.yml", Encoding.UTF8.GetBytes(compose));
            WriteEntry(archive, "README.md", Encoding.UTF8.GetBytes(readme));
        }

        return new ReverseTunnelAgentPackage(output.ToArray(), $"agent-{SanitizeFileName(snapshot.AgentId)}.zip");
    }

    private static void WriteEntry(ZipArchive archive, string name, byte[] content)
    {
        var entry = archive.CreateEntry(name, CompressionLevel.Fastest);
        using var stream = entry.Open();
        stream.Write(content, 0, content.Length);
    }

    private static ReverseTunnelAgentPackageExportRequest NormalizeRequest(
        ReverseTunnelAgentPackageExportRequest? request,
        string agentId)
    {
        request ??= new ReverseTunnelAgentPackageExportRequest();
        var imageName = request.ImageName?.Trim() ?? string.Empty;
        var containerName = string.IsNullOrWhiteSpace(request.ContainerName)
            ? $"airtunnel-agent-{SanitizeDockerName(agentId)}"
            : request.ContainerName.Trim();

        if (!Regex.IsMatch(imageName, "^[a-z0-9][a-z0-9._/@:-]{0,254}$"))
        {
            throw new ArgumentException("ImageName must be a lowercase Docker image reference.", nameof(request));
        }

        if (!Regex.IsMatch(containerName, "^[a-z0-9][a-z0-9_.-]{0,127}$"))
        {
            throw new ArgumentException("ContainerName must be a lowercase Docker container name.", nameof(request));
        }

        var environment = request.RuntimeEnvironment ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (environment.Count == 0)
        {
            throw new ArgumentException("RuntimeEnvironment must contain at least one entry.", nameof(request));
        }

        foreach (var pair in environment)
        {
            if (!Regex.IsMatch(pair.Key ?? string.Empty, "^[A-Za-z_][A-Za-z0-9_]{0,127}$") ||
                pair.Value is null || pair.Value.Contains('\r') || pair.Value.Contains('\n'))
            {
                throw new ArgumentException("RuntimeEnvironment contains an invalid key or value.", nameof(request));
            }
        }

        return new ReverseTunnelAgentPackageExportRequest
        {
            ImageName = imageName,
            ContainerName = containerName,
            RuntimeEnvironment = new Dictionary<string, string>(environment, StringComparer.OrdinalIgnoreCase)
        };
    }

    private static string CreateCompose(ReverseTunnelAgentPackageExportRequest request)
    {
        var lines = new List<string>
        {
            "version: \"3.8\"",
            "",
            "services:",
            "  airtunnel-agent:",
            $"    image: {YamlScalar(request.ImageName)}",
            $"    container_name: {YamlScalar(request.ContainerName)}",
            "    restart: unless-stopped",
            "    environment:",
        };
        lines.AddRange(request.RuntimeEnvironment
            .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .Select(pair => $"      {pair.Key}: {YamlScalar(pair.Value)}"));
        lines.AddRange(new[]
        {
            "    volumes:",
            "      - ./config:/app/config:ro",
            "      - ./configs:/app/configs:ro",
            "      - ./data:/app/data",
            "      - ./logs:/app/logs"
        });
        return string.Join(Environment.NewLine, lines);
    }

    private static string YamlScalar(string value)
        => $"'{value.Replace("'", "''", StringComparison.Ordinal)}'";

    private static string CreateReadme(string agentId)
        => string.Join(Environment.NewLine, new[]
        {
            $"# AirTunnel Agent: {agentId}",
            "",
            "1. Put the image referenced by `docker-compose.yml` on the Agent host.",
            "2. Keep `data/certs/ca.crt` from this package. Do not replace it with a CA private key.",
            "3. Review `config/00-airtunnel-agent.json`, then run `docker compose -f docker-compose.yml up -d`.",
            "4. On first start the Agent creates its own `agent.key`, generates a CSR, and enrolls with the configured Server.",
            "",
            "The package intentionally does not contain `agent.key`, `agent.crt`, `ca.key`, or `ca.pfx`."
        });

    private static string SanitizeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            builder.Append(invalid.Contains(character) ? '_' : character);
        }

        return builder.Length == 0 ? "agent" : builder.ToString();
    }

    private static string SanitizeDockerName(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var character in value.ToLowerInvariant())
        {
            builder.Append(char.IsLetterOrDigit(character) || character is '_' or '.' or '-' ? character : '-');
        }

        return builder.Length == 0 ? "agent" : builder.ToString().Trim('-');
    }
}
