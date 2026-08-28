using Air.Cloud.Modules.ReverseTunnel.Agent;
using Air.Cloud.Modules.ReverseTunnel.Common.Options;
using Air.Cloud.Modules.ReverseTunnel.Common.Runtime;

using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;

namespace Air.Cloud.UnitTest.Modules.ReverseTunnel;

/// <summary>
/// <para>zh-cn:Agent 运行期配置快照与本地持久化测试。</para>
/// <para>en-us:Tests for Agent runtime configuration snapshots and local persistence.</para>
/// </summary>
public sealed class ReverseTunnelAgentConfigurationSnapshotTests
{
    /// <summary>
    /// <para>zh-cn:验证快照序列化后可以恢复并通过哈希校验。</para>
    /// <para>en-us:Verifies that a serialized snapshot can be restored and passes hash validation.</para>
    /// </summary>
    [Fact]
    public void Snapshot_should_round_trip_and_verify_hash()
    {
        var options = new ReverseTunnelOptions
        {
            Enabled = true,
            TunnelMode = ReverseTunnelMode.Agent,
            AgentId = "agent-alpha",
            LocalServices =
            [
                new ReverseTunnelLocalServiceOptions
                {
                    ServiceKey = "order-service",
                    LocalAddress = "http://127.0.0.1:5101"
                }
            ]
        };
        options.PostConfigure(options, new ConfigurationBuilder().Build());

        var snapshot = ReverseTunnelAgentConfigurationSnapshotSerializer.Create("agent-alpha", options, 7);
        var restored = ReverseTunnelAgentConfigurationSnapshotSerializer.Deserialize(
            System.Text.Encoding.UTF8.GetBytes(ReverseTunnelAgentConfigurationSnapshotSerializer.Serialize(snapshot)));

        Assert.Equal(7, restored.Version);
        Assert.Equal("agent-alpha", restored.AgentId);
        Assert.True(ReverseTunnelAgentConfigurationSnapshotSerializer.VerifyHash(restored));
        Assert.Equal("http://127.0.0.1:5101", restored.Configuration.LocalServices[0].LocalAddress);
    }

    /// <summary>
    /// <para>zh-cn:验证快照存储使用临时文件替换并能读取最后一次有效版本。</para>
    /// <para>en-us:Verifies that the snapshot store uses replacement writes and restores the last valid version.</para>
    /// </summary>
    [Fact]
    public async Task Mounted_store_should_save_and_load_last_version()
    {
        var directory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "air-cloud-reverse-tunnel-snapshot-tests", Guid.NewGuid().ToString("N")));
        try
        {
            var path = Path.Combine(directory.FullName, "agent.snapshot.json");
            var options = Options.Create(new ReverseTunnelOptions
            {
                AgentId = "agent-alpha",
                AgentCertificate = new ReverseTunnelAgentCertificateOptions
                {
                    CertificatePath = Path.Combine(directory.FullName, "agent.cer"),
                    ConfigurationSnapshotPath = path
                }
            });
            var store = new MountedReverseTunnelAgentConfigurationStore(options);
            var snapshot = ReverseTunnelAgentConfigurationSnapshotSerializer.Create("agent-alpha", new ReverseTunnelOptions
            {
                AgentId = "agent-alpha"
            }, 12);

            await store.SaveAsync(snapshot);
            var restored = await store.LoadAsync();

            Assert.Equal(12, store.CurrentVersion);
            Assert.NotNull(restored);
            Assert.Equal(12, restored!.Version);
            Assert.True(File.Exists(path));
        }
        finally
        {
            if (Directory.Exists(directory.FullName))
            {
                Directory.Delete(directory.FullName, true);
            }
        }
    }

    /// <summary>
    /// <para>zh-cn:验证缺少哈希或修改配置内容时快照校验失败。</para>
    /// <para>en-us:Verifies that a missing hash or modified configuration fails snapshot validation.</para>
    /// </summary>
    [Fact]
    public void Snapshot_should_reject_missing_or_tampered_hash()
    {
        var snapshot = ReverseTunnelAgentConfigurationSnapshotSerializer.Create(
            "agent-alpha",
            new ReverseTunnelOptions { AgentId = "agent-alpha" },
            1);

        snapshot.Sha256 = string.Empty;
        Assert.False(ReverseTunnelAgentConfigurationSnapshotSerializer.VerifyHash(snapshot));

        ReverseTunnelAgentConfigurationSnapshotSerializer.RefreshHash(snapshot);
        snapshot.Configuration.AgentConnectionCount++;
        Assert.False(ReverseTunnelAgentConfigurationSnapshotSerializer.VerifyHash(snapshot));
    }
}


