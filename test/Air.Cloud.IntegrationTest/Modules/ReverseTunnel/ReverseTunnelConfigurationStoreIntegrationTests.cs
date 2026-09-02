using Air.Cloud.Modules.ReverseTunnel.Common.Options;
using Air.Cloud.Modules.ReverseTunnel.Common.Runtime;
using Air.Cloud.Modules.ReverseTunnel.Management;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Air.Cloud.IntegrationTest.Modules.ReverseTunnel;

public sealed class ReverseTunnelConfigurationStoreIntegrationTests
{
    [Fact]
    public void Management_page_should_be_embedded_in_the_module()
    {
        Assert.Contains(
            "Air.Cloud.Modules.ReverseTunnel.Management.management.html",
            typeof(ReverseTunnelConfigurationStore).Assembly.GetManifestResourceNames());

        using var stream = typeof(ReverseTunnelConfigurationStore).Assembly.GetManifestResourceStream(
            "Air.Cloud.Modules.ReverseTunnel.Management.management.html");
        using var reader = new StreamReader(stream!);
        var page = reader.ReadToEnd();
        Assert.Contains("AirTunnel 管理", page, StringComparison.Ordinal);
        Assert.Contains("X-Air-ControlPlane-Ticket", page, StringComparison.Ordinal);
        Assert.Contains("window.location.search", page, StringComparison.Ordinal);
        Assert.Contains("/config/backups/", page, StringComparison.Ordinal);
        Assert.Contains("目标版本", page, StringComparison.Ordinal);
        Assert.DoesNotContain("[hidden]{display:none!important]", page, StringComparison.Ordinal);
        Assert.Contains("id=\"configForm\"", page, StringComparison.Ordinal);
        Assert.Contains("id=\"saveDialog\"", page, StringComparison.Ordinal);
        Assert.Contains("id=\"configMessage\" class=\"message\" role=\"status\"", page, StringComparison.Ordinal);
        Assert.Contains("id=\"agentManagementList\"", page, StringComparison.Ordinal);
        Assert.Contains("id=\"agentRuntimeDialog\"", page, StringComparison.Ordinal);
        Assert.Contains("id=\"agentRuntimeConfig\"", page, StringComparison.Ordinal);
        Assert.Contains("data-agent-runtime", page, StringComparison.Ordinal);
        Assert.Contains("agentRuntimeDetail\">运行配置", page, StringComparison.Ordinal);
        Assert.Contains("agentRuntimeLoading", page, StringComparison.Ordinal);
        Assert.Contains("function openAgentRuntimeDialog", page, StringComparison.Ordinal);
        Assert.Contains("await loadSelectedAgentConfiguration();openAgentExportDialog(createdAgentId)", page, StringComparison.Ordinal);
        Assert.Contains("id=\"createAgentDialog\"", page, StringComparison.Ordinal);
        Assert.Contains("function submitCreateAgent", page, StringComparison.Ordinal);
        Assert.Contains("serverDerivedConfigHint", page, StringComparison.Ordinal);
        Assert.Contains("onclick=\"confirmSaveDialog()\"", page, StringComparison.Ordinal);
        Assert.Contains("options:configModel,changeReason:normalizedReason", page, StringComparison.Ordinal);
        Assert.DoesNotContain("X-AirTunnel-Change-Reason", page, StringComparison.Ordinal);
        Assert.Contains("validatePending", page, StringComparison.Ordinal);
        Assert.Contains("savePending", page, StringComparison.Ordinal);
        Assert.DoesNotContain("<form method=\"dialog\"", page, StringComparison.Ordinal);
        Assert.Contains("restartConfigPaths", page, StringComparison.Ordinal);
        Assert.DoesNotContain("id=\"actor\"", page, StringComparison.Ordinal);
        Assert.DoesNotContain("id=\"changeReason\"", page, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Save_restore_and_audit_should_persist_split_configuration()
    {
        var root = Path.Combine(Path.GetTempPath(), "air-cloud-reverse-tunnel-store", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var initial = CreateOptions(root);
            var runtimeOptions = new ReverseTunnelRuntimeOptionsAccessor(Options.Create(initial));
            var store = new ReverseTunnelConfigurationStore(
                runtimeOptions,
                new TestHostEnvironment(root),
                [new ReverseTunnelOptionsValidation()]);

            var firstSave = await store.SaveAsync(initial, "operator-a", "initial-config");
            Assert.True(firstSave.Succeeded);
            var firstServerJson = await File.ReadAllTextAsync(Path.Combine(initial.Management.ConfigDirectory, "airtunnel.server.json"));
            Assert.DoesNotContain("\"routes\"", firstServerJson, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("\"localServices\"", firstServerJson, StringComparison.OrdinalIgnoreCase);
            Assert.Empty(runtimeOptions.Current.Routes);
            Assert.Empty(runtimeOptions.Current.LocalServices);

            var changed = ReverseTunnelOptionsClonerForTest.Clone(initial);
            changed.AgentConnectionCount = 4;
            var secondSave = await store.SaveAsync(changed, "operator-b", "scale-connections");

            Assert.True(secondSave.Succeeded);
            Assert.NotNull(secondSave.BackupDirectory);
            Assert.Contains("\"agentConnectionCount\": 1", await File.ReadAllTextAsync(Path.Combine(secondSave.BackupDirectory!, "airtunnel.server.json")));
            var backupJson = await File.ReadAllTextAsync(Path.Combine(secondSave.BackupDirectory!, "airtunnel.server.json"));
            var backupRoot = System.Text.Json.JsonDocument.Parse(backupJson).RootElement;
            var backupSettings = backupRoot.EnumerateObject()
                .First(property => string.Equals(property.Name, "ReverseTunnelSettings", StringComparison.OrdinalIgnoreCase))
                .Value;
            var backupOptions = System.Text.Json.JsonSerializer.Deserialize<ReverseTunnelOptions>(
                backupSettings.GetRawText(),
                new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web)
                {
                    Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
                });
            Assert.Equal(1, backupOptions!.AgentConnectionCount);
            Assert.Contains("agentConnectionCount", (await store.ListAuditAsync()).First().DifferenceSummary);

            var backupName = Path.GetFileName(secondSave.BackupDirectory);
            var restore = await store.RestoreAsync(backupName, "operator-c", "rollback");

            Assert.True(restore.Succeeded);
            Assert.Equal(1, runtimeOptions.Current.AgentConnectionCount);
            Assert.Equal(3, (await store.ListAuditAsync()).Count);
            Assert.Contains("rollback", (await store.ListAuditAsync()).First().Reason);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    private static ReverseTunnelOptions CreateOptions(string root)
    {
        var options = new ReverseTunnelOptions
        {
            Enabled = false,
            AgentConnectionCount = 1,
            Routes = [new ReverseTunnelRouteOptions { VirtualHost = "legacy.example", ServiceKey = "legacy" }],
            LocalServices = [new ReverseTunnelLocalServiceOptions { ServiceKey = "legacy", LocalAddress = "http://127.0.0.1:5101" }],
            Management = new ReverseTunnelManagementOptions
            {
                ConfigDirectory = Path.Combine(root, "configs"),
                BackupDirectory = Path.Combine(root, "backups"),
                AllowConfigWrite = true
            }
        };
        options.PostConfigure(options, new ConfigurationManager());
        return options;
    }

    private sealed class TestHostEnvironment(string root) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "Air.Cloud.IntegrationTest";
        public string ContentRootPath { get; set; } = root;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private static class ReverseTunnelOptionsClonerForTest
    {
        public static ReverseTunnelOptions Clone(ReverseTunnelOptions options)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(options);
            return System.Text.Json.JsonSerializer.Deserialize<ReverseTunnelOptions>(json)
                ?? new ReverseTunnelOptions();
        }
    }
}


