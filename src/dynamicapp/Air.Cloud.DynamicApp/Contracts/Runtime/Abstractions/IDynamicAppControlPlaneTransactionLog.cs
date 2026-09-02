namespace Air.Cloud.DynamicApp.Contracts.Runtime;

/// <summary>
/// <para>zh-cn:DynamicApp 控制面统一事务日志，用于记录多快照操作的准备、提交和异常终止。</para>
/// <para>en-us:Unified DynamicApp control-plane transaction log that records preparation, commit, and abort of operations spanning multiple snapshots.</para>
/// </summary>
public interface IDynamicAppControlPlaneTransactionLog
{
    /// <summary>
    /// <para>zh-cn:写入准备记录并返回事务标识。</para>
    /// <para>en-us:Writes a prepared record and returns the transaction identifier.</para>
    /// </summary>
    string Begin(string operation, string scope);

    /// <summary>
    /// <para>zh-cn:写入提交记录。</para>
    /// <para>en-us:Writes a committed record.</para>
    /// </summary>
    void Commit(string transactionId);

    /// <summary>
    /// <para>zh-cn:写入异常终止记录，保留错误原因供恢复和排障使用。</para>
    /// <para>en-us:Writes an aborted record while retaining the failure reason for recovery and diagnosis.</para>
    /// </summary>
    void Abort(string transactionId, string error);

    /// <summary>
    /// <para>zh-cn:扫描未完成事务并将其标记为异常终止；Host 启动时应调用该方法。</para>
    /// <para>en-us:Scans incomplete transactions and marks them aborted; Hosts should call this during startup.</para>
    /// </summary>
    IReadOnlyCollection<DynamicAppControlPlaneTransactionRecord> RecoverIncomplete();
}
