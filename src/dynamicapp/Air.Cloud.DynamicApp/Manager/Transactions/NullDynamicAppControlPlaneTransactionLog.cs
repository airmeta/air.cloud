using Air.Cloud.DynamicApp.Contracts.Runtime;

namespace Air.Cloud.DynamicApp.Manager;

internal sealed class NullDynamicAppControlPlaneTransactionLog : IDynamicAppControlPlaneTransactionLog
{
    public static NullDynamicAppControlPlaneTransactionLog Instance { get; } = new();
    private NullDynamicAppControlPlaneTransactionLog() { }
    public string Begin(string operation, string scope) => Guid.NewGuid().ToString("N");
    public void Commit(string transactionId) { }
    public void Abort(string transactionId, string error) { }
    public IReadOnlyCollection<DynamicAppControlPlaneTransactionRecord> RecoverIncomplete() => Array.Empty<DynamicAppControlPlaneTransactionRecord>();
}
