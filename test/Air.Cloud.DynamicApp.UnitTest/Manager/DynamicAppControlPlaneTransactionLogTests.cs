using Air.Cloud.DynamicApp.Contracts.Runtime;
using Air.Cloud.DynamicApp.Manager;

namespace Air.Cloud.DynamicApp.UnitTest.Manager;

/// <summary>
/// <para>zh-cn:控制面事务日志恢复测试。</para>
/// <para>en-us:Recovery tests for the control-plane transaction log.</para>
/// </summary>
public sealed class DynamicAppControlPlaneTransactionLogTests
{
    /// <summary>
    /// <para>zh-cn:未写入终态的事务在恢复时被标记为 Aborted，之后不会重复恢复。</para>
    /// <para>en-us:Transactions without a terminal record are marked Aborted during recovery and are not recovered again.</para>
    /// </summary>
    [Fact]
    public void Recover_should_abort_incomplete_transaction_once()
    {
        using var workspace = DynamicAppTestWorkspace.Create();
        var path = Path.Combine(workspace.RootPath, "control-plane.jsonl");
        var log = new FileDynamicAppControlPlaneTransactionLog(new FileDynamicAppControlPlaneTransactionLogOptions { FilePath = path });
        var transactionId = log.Begin("publish", "security-platform");

        var recovered = log.RecoverIncomplete();

        var record = Assert.Single(recovered);
        Assert.Equal(transactionId, record.TransactionId);
        Assert.Empty(log.RecoverIncomplete());
        Assert.Contains("Aborted", File.ReadAllText(path), StringComparison.OrdinalIgnoreCase);
    }
}
