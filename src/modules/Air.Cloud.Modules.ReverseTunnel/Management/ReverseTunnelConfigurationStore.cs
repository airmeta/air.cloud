using Air.Cloud.Modules.ReverseTunnel.Common.Options;
using Air.Cloud.Modules.ReverseTunnel.Common.Runtime;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Air.Cloud.Modules.ReverseTunnel.Management;

/// <summary>
/// <para>zh-cn:基于本地文件系统的 AirTunnel 拆分配置存储。</para>
/// <para>en-us:File-system based AirTunnel split-configuration store.</para>
/// </summary>
public sealed class ReverseTunnelConfigurationStore : IReverseTunnelConfigurationStore
{
    private static readonly string[] SplitFileNames =
    [
        "airtunnel.server.json",
        "airtunnel.listeners.json",
        "airtunnel.authorizations.json",
        "airtunnel.limits.json",
        "airtunnel.diagnostics.json",
        "airtunnel.management.json"
    ];

    private readonly IReverseTunnelRuntimeOptionsAccessor runtimeOptions;
    private readonly IHostEnvironment hostEnvironment;
    private readonly IEnumerable<IValidateOptions<ReverseTunnelOptions>> validators;

    /// <summary>
    /// <para>zh-cn:创建拆分配置存储。</para>
    /// <para>en-us:Creates the split-configuration store.</para>
    /// </summary>
    /// <param name="runtimeOptions">zh-cn:运行期配置访问器。 en-us:The runtime-options accessor.</param>
    /// <param name="hostEnvironment">zh-cn:宿主环境，用于解析相对目录。 en-us:The host environment used to resolve relative directories.</param>
    /// <param name="validators">zh-cn:AirTunnel 配置校验器集合。 en-us:AirTunnel option validators.</param>
    public ReverseTunnelConfigurationStore(
        IReverseTunnelRuntimeOptionsAccessor runtimeOptions,
        IHostEnvironment hostEnvironment,
        IEnumerable<IValidateOptions<ReverseTunnelOptions>> validators)
    {
        this.runtimeOptions = runtimeOptions;
        this.hostEnvironment = hostEnvironment;
        this.validators = validators;
    }

    /// <inheritdoc />
    public Task<ReverseTunnelConfigurationView> ReadAsync(CancellationToken cancellationToken = default)
    {
        var management = runtimeOptions.Current.Management;
        var directory = ResolvePath(management.ConfigDirectory);
        var files = SplitFileNames.Select(name => Path.Combine(directory, name)).ToArray();
        return Task.FromResult(new ReverseTunnelConfigurationView
        {
            Options = NormalizeServerManagedOptions(runtimeOptions.Current),
            Files = files
        });
    }

    /// <inheritdoc />
    public Task<ReverseTunnelConfigurationValidationResult> ValidateAsync(ReverseTunnelOptions options, CancellationToken cancellationToken = default)
    {
        // HTTP Routes and top-level LocalServices are Agent-owned. Ignore values
        // sent by older clients so a Server save cannot recreate a second source
        // of truth for those mappings.
        var normalized = NormalizeServerManagedOptions(options);
        var failures = validators
            .Select(validator => validator.Validate(Options.DefaultName, normalized))
            .Where(result => result.Failed)
            .SelectMany(result => result.Failures ?? [])
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return Task.FromResult(new ReverseTunnelConfigurationValidationResult
        {
            Succeeded = failures.Length == 0,
            Failures = failures,
            Options = normalized
        });
    }

    /// <inheritdoc />
    public async Task<ReverseTunnelConfigurationSaveResult> SaveAsync(
        ReverseTunnelOptions options,
        string? actor = null,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var validation = await ValidateAsync(options, cancellationToken);
        if (!validation.Succeeded)
        {
            return new ReverseTunnelConfigurationSaveResult
            {
                Succeeded = false,
                Failures = validation.Failures,
                Options = validation.Options,
                Version = runtimeOptions.Version
            };
        }

        var management = validation.Options.Management;
        if (!management.AllowConfigWrite)
        {
            return new ReverseTunnelConfigurationSaveResult
            {
                Succeeded = false,
                Failures = ["ReverseTunnelSettings:Management:AllowConfigWrite is false."],
                Options = validation.Options,
                Version = runtimeOptions.Version
            };
        }

        var configDirectory = ResolvePath(management.ConfigDirectory);
        var version = Math.Max(runtimeOptions.Version + 1, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        var normalizedActor = string.IsNullOrWhiteSpace(actor) ? "management-api" : actor.Trim();
        var normalizedReason = string.IsNullOrWhiteSpace(reason) ? "configuration-update" : reason.Trim();
        var differenceSummary = BuildDifferenceSummary(runtimeOptions.Current, validation.Options);
        var backupDirectory = await BackupCurrentFilesAsync(configDirectory, management.BackupDirectory, version, normalizedActor, normalizedReason, cancellationToken);
        Directory.CreateDirectory(configDirectory);

        await WriteSplitFileAsync(configDirectory, "airtunnel.server.json", new
        {
            ReverseTunnelSettings = new
            {
                validation.Options.Enabled,
                validation.Options.TunnelMode,
                validation.Options.EnableTunnelProxy,
                validation.Options.SessionSelectionStrategy,
                validation.Options.AgentId,
                validation.Options.ServerAddress,
                validation.Options.AgentConnectionCount,
                validation.Options.ProtocolVersion,
                validation.Options.HeartbeatIntervalSeconds,
                validation.Options.SessionIdleTimeoutSeconds,
                validation.Options.Gateway,
                validation.Options.CertificateAuthority,
                validation.Options.Enrollment,
                validation.Options.AgentCertificate
            }
        }, cancellationToken);
        await WriteSplitFileAsync(configDirectory, "airtunnel.listeners.json", new
        {
            ReverseTunnelSettings = new
            {
                validation.Options.Tcp,
                validation.Options.Udp
            }
        }, cancellationToken);
        await WriteSplitFileAsync(configDirectory, "airtunnel.authorizations.json", new
        {
            ReverseTunnelSettings = new
            {
                validation.Options.AgentAuthorizations
            }
        }, cancellationToken);
        await WriteSplitFileAsync(configDirectory, "airtunnel.limits.json", new
        {
            ReverseTunnelSettings = new
            {
                validation.Options.MaxConcurrentStreams,
                validation.Options.MaxRequestBodyBytes,
                validation.Options.MaxResponseBodyBytes,
                validation.Options.MaxFramePayloadBytes,
                validation.Options.OutboundFrameQueueCapacity,
                validation.Options.AgentReconnectInitialDelaySeconds,
                validation.Options.AgentReconnectMaxDelaySeconds
            }
        }, cancellationToken);
        await WriteSplitFileAsync(configDirectory, "airtunnel.diagnostics.json", new
        {
            ReverseTunnelSettings = new
            {
                validation.Options.Diagnostics
            }
        }, cancellationToken);
        await WriteSplitFileAsync(configDirectory, "airtunnel.management.json", new
        {
            ReverseTunnelSettings = new
            {
                validation.Options.Management
            }
        }, cancellationToken);

        runtimeOptions.Update(validation.Options);
        await AppendAuditAsync(management.BackupDirectory, new ReverseTunnelConfigurationAuditInfo
        {
            Version = version,
            ChangedAtUtc = DateTimeOffset.UtcNow,
            Actor = normalizedActor,
            Reason = normalizedReason,
            BackupDirectory = backupDirectory,
            DifferenceSummary = differenceSummary
        }, cancellationToken);
        return new ReverseTunnelConfigurationSaveResult
        {
            Succeeded = true,
            Options = validation.Options,
            BackupDirectory = backupDirectory,
            Version = version
        };
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ReverseTunnelConfigurationBackupInfo>> ListBackupsAsync(CancellationToken cancellationToken = default)
    {
        var directory = ResolvePath(runtimeOptions.Current.Management.BackupDirectory);
        if (!Directory.Exists(directory))
        {
            return Task.FromResult<IReadOnlyList<ReverseTunnelConfigurationBackupInfo>>([]);
        }

        var backups = Directory.EnumerateDirectories(directory)
            .Select(path => new ReverseTunnelConfigurationBackupInfo
            {
                Name = Path.GetFileName(path),
                Path = path,
                CreatedAtUtc = Directory.GetCreationTimeUtc(path),
                Version = ReadBackupMetadata(path)?.Version ?? 0,
                Actor = ReadBackupMetadata(path)?.Actor ?? string.Empty,
                Reason = ReadBackupMetadata(path)?.Reason ?? string.Empty
            })
            .OrderByDescending(item => item.CreatedAtUtc)
            .ToArray();
        return Task.FromResult<IReadOnlyList<ReverseTunnelConfigurationBackupInfo>>(backups);
    }

    /// <summary>
    /// <para>zh-cn:从备份目录恢复配置并生成新的审计记录。</para>
    /// <para>en-us:Restores configuration from a backup directory and creates a new audit record.</para>
    /// </summary>
    public async Task<ReverseTunnelConfigurationSaveResult> RestoreAsync(
        string backupName,
        string? actor = null,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(backupName) ||
            backupName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
            !string.Equals(Path.GetFileName(backupName), backupName, StringComparison.Ordinal))
        {
            return new ReverseTunnelConfigurationSaveResult
            {
                Succeeded = false,
                Failures = ["A valid backup name is required."],
                Options = ReverseTunnelOptionsCloner.CloneAndNormalize(runtimeOptions.Current),
                Version = runtimeOptions.Version
            };
        }

        var backupDirectory = ResolvePath(Path.Combine(runtimeOptions.Current.Management.BackupDirectory, backupName.Trim()));
        if (!Directory.Exists(backupDirectory))
        {
            return new ReverseTunnelConfigurationSaveResult
            {
                Succeeded = false,
                Failures = ["The requested configuration backup was not found."],
                Options = ReverseTunnelOptionsCloner.CloneAndNormalize(runtimeOptions.Current),
                Version = runtimeOptions.Version
            };
        }

        var current = ReverseTunnelOptionsCloner.CloneAndNormalize(runtimeOptions.Current);
        var candidate = await ReadBackupOptionsAsync(backupDirectory, current, cancellationToken);
        return await SaveAsync(candidate, actor, string.IsNullOrWhiteSpace(reason) ? $"restore:{backupName.Trim()}" : reason, cancellationToken);
    }

    /// <summary>
    /// <para>zh-cn:读取本地配置审计记录。</para>
    /// <para>en-us:Reads local configuration audit records.</para>
    /// </summary>
    public async Task<IReadOnlyList<ReverseTunnelConfigurationAuditInfo>> ListAuditAsync(CancellationToken cancellationToken = default)
    {
        var path = ResolvePath(Path.Combine(runtimeOptions.Current.Management.BackupDirectory, "audit.json"));
        if (!File.Exists(path))
        {
            return [];
        }

        await using var stream = File.OpenRead(path);
        return await System.Text.Json.JsonSerializer.DeserializeAsync<List<ReverseTunnelConfigurationAuditInfo>>(stream, cancellationToken: cancellationToken)
            ?? [];
    }

    private async Task<string?> BackupCurrentFilesAsync(
        string configDirectory,
        string backupDirectorySetting,
        long version,
        string actor,
        string reason,
        CancellationToken cancellationToken)
    {
        if (!Directory.Exists(configDirectory))
        {
            return null;
        }

        var existingFiles = SplitFileNames
            .Select(name => Path.Combine(configDirectory, name))
            .Where(File.Exists)
            .ToArray();
        if (existingFiles.Length == 0)
        {
            return null;
        }

        var backupRoot = ResolvePath(backupDirectorySetting);
        var backupDirectory = Path.Combine(backupRoot, DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmssfff"));
        Directory.CreateDirectory(backupDirectory);
        foreach (var file in existingFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await using var source = File.OpenRead(file);
            await using var target = File.Create(Path.Combine(backupDirectory, Path.GetFileName(file)));
            await source.CopyToAsync(target, cancellationToken);
        }

        await File.WriteAllTextAsync(
            Path.Combine(backupDirectory, "metadata.json"),
            ReverseTunnelOptionsCloner.Serialize(new { version, actor, reason, createdAtUtc = DateTimeOffset.UtcNow }) + Environment.NewLine,
            cancellationToken);

        return backupDirectory;
    }

    private async Task AppendAuditAsync(string backupDirectorySetting, ReverseTunnelConfigurationAuditInfo audit, CancellationToken cancellationToken)
    {
        var path = ResolvePath(Path.Combine(backupDirectorySetting, "audit.json"));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var items = (await ListAuditAsync(cancellationToken)).ToList();
        items.Insert(0, audit);
        await File.WriteAllTextAsync(path, System.Text.Json.JsonSerializer.Serialize(items) + Environment.NewLine, cancellationToken);
    }

    private ReverseTunnelConfigurationBackupMetadata? ReadBackupMetadata(string directory)
    {
        var path = Path.Combine(directory, "metadata.json");
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<ReverseTunnelConfigurationBackupMetadata>(File.ReadAllText(path));
        }
        catch
        {
            return null;
        }
    }

    private static ReverseTunnelOptions NormalizeServerManagedOptions(ReverseTunnelOptions options)
    {
        var normalized = ReverseTunnelOptionsCloner.CloneAndNormalize(options);
        normalized.Routes = [];
        normalized.LocalServices = [];
        return normalized;
    }

    private async Task<ReverseTunnelOptions> ReadBackupOptionsAsync(string backupDirectory, ReverseTunnelOptions fallback, CancellationToken cancellationToken)
    {
        var candidate = NormalizeServerManagedOptions(fallback);
        foreach (var fileName in SplitFileNames)
        {
            var path = Path.Combine(backupDirectory, fileName);
            if (!File.Exists(path))
            {
                continue;
            }

            await using var stream = File.OpenRead(path);
            using var document = await System.Text.Json.JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var settingsProperty = document.RootElement.EnumerateObject()
                .FirstOrDefault(property => string.Equals(property.Name, "ReverseTunnelSettings", StringComparison.OrdinalIgnoreCase));
            if (settingsProperty.Equals(default(System.Text.Json.JsonProperty)))
            {
                continue;
            }

            var merged = System.Text.Json.JsonSerializer.Deserialize<ReverseTunnelOptions>(settingsProperty.Value.GetRawText(), new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web)
            {
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
            });
            if (merged is not null)
            {
                candidate = MergeOptions(candidate, merged, fileName);
            }
        }

        candidate.PostConfigure(candidate, new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build());
        return NormalizeServerManagedOptions(candidate);
    }

    private static ReverseTunnelOptions MergeOptions(ReverseTunnelOptions target, ReverseTunnelOptions source, string fileName)
    {
        if (fileName.Contains("server", StringComparison.OrdinalIgnoreCase))
        {
            target.Enabled = source.Enabled;
            target.TunnelMode = source.TunnelMode;
            target.EnableTunnelProxy = source.EnableTunnelProxy;
            target.AgentId = source.AgentId;
            target.ServerAddress = source.ServerAddress;
            target.AgentConnectionCount = source.AgentConnectionCount;
            target.ProtocolVersion = source.ProtocolVersion;
            target.HeartbeatIntervalSeconds = source.HeartbeatIntervalSeconds;
            target.SessionIdleTimeoutSeconds = source.SessionIdleTimeoutSeconds;
            target.Gateway = source.Gateway;
            target.CertificateAuthority = source.CertificateAuthority;
            target.Enrollment = source.Enrollment;
            target.AgentCertificate = source.AgentCertificate;
        }
        else if (fileName.Contains("listeners", StringComparison.OrdinalIgnoreCase))
        {
            target.Tcp = source.Tcp;
            target.Udp = source.Udp;
        }
        else if (fileName.Contains("authorizations", StringComparison.OrdinalIgnoreCase))
        {
            target.AgentAuthorizations = source.AgentAuthorizations;
        }
        else if (fileName.Contains("limits", StringComparison.OrdinalIgnoreCase))
        {
            target.MaxConcurrentStreams = source.MaxConcurrentStreams;
            target.MaxRequestBodyBytes = source.MaxRequestBodyBytes;
            target.MaxResponseBodyBytes = source.MaxResponseBodyBytes;
            target.MaxFramePayloadBytes = source.MaxFramePayloadBytes;
            target.OutboundFrameQueueCapacity = source.OutboundFrameQueueCapacity;
            target.AgentReconnectInitialDelaySeconds = source.AgentReconnectInitialDelaySeconds;
            target.AgentReconnectMaxDelaySeconds = source.AgentReconnectMaxDelaySeconds;
        }
        else if (fileName.Contains("diagnostics", StringComparison.OrdinalIgnoreCase))
        {
            target.Diagnostics = source.Diagnostics;
        }
        else if (fileName.Contains("management", StringComparison.OrdinalIgnoreCase))
        {
            target.Management = source.Management;
        }

        return target;
    }

    /// <summary>
    /// <para>zh-cn:比较保存前后的配置顶层字段，并生成用于审计记录的差异摘要。</para>
    /// <para>en-us:Compares top-level configuration fields before and after a save and builds an audit difference summary.</para>
    /// </summary>
    /// <param name="before">
    /// <para>zh-cn:保存前的运行期配置。</para>
    /// <para>en-us:The runtime configuration before the save.</para>
    /// </param>
    /// <param name="after">
    /// <para>zh-cn:保存后的标准化配置。</para>
    /// <para>en-us:The normalized configuration after the save.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:可供管理页面和审计文件展示的字段差异摘要。</para>
    /// <para>en-us:A field-level difference summary suitable for the management page and audit file.</para>
    /// </returns>
    private static string BuildDifferenceSummary(ReverseTunnelOptions before, ReverseTunnelOptions after)
    {
        using var beforeDocument = System.Text.Json.JsonDocument.Parse(ReverseTunnelOptionsCloner.Serialize(before));
        using var afterDocument = System.Text.Json.JsonDocument.Parse(ReverseTunnelOptionsCloner.Serialize(after));
        var beforeValues = beforeDocument.RootElement.EnumerateObject()
            .ToDictionary(property => property.Name, property => property.Value.GetRawText(), StringComparer.Ordinal);
        var afterValues = afterDocument.RootElement.EnumerateObject()
            .ToDictionary(property => property.Name, property => property.Value.GetRawText(), StringComparer.Ordinal);
        var changed = beforeValues.Keys
            .Union(afterValues.Keys, StringComparer.Ordinal)
            .Where(key => !beforeValues.TryGetValue(key, out var beforeValue)
                || !afterValues.TryGetValue(key, out var afterValue)
                || !string.Equals(beforeValue, afterValue, StringComparison.Ordinal))
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToArray();

        return changed.Length == 0
            ? "no effective changes"
            : $"changed: {string.Join(", ", changed)}";
    }

    private sealed record ReverseTunnelConfigurationBackupMetadata(long Version, string Actor, string Reason, DateTimeOffset CreatedAtUtc);

    private static async Task WriteSplitFileAsync(string directory, string fileName, object value, CancellationToken cancellationToken)
    {
        var path = Path.Combine(directory, fileName);
        var json = ReverseTunnelOptionsCloner.Serialize(value);
        await File.WriteAllTextAsync(path, json + Environment.NewLine, cancellationToken);
    }

    private string ResolvePath(string path)
    {
        if (Path.IsPathRooted(path))
        {
            return Path.GetFullPath(path);
        }

        return Path.GetFullPath(Path.Combine(hostEnvironment.ContentRootPath, path));
    }
}

