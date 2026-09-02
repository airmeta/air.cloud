namespace Air.Cloud.DynamicApp.Contracts.Runtime;

/// <summary>
/// <para>zh-cn:控制面事务状态，用于记录跨文件生命周期操作的提交边界。</para>
/// <para>en-us:Control-plane transaction status used to record commit boundaries for lifecycle operations spanning multiple files.</para>
/// </summary>
public enum DynamicAppControlPlaneTransactionStatus
{
    /// <summary><para>zh-cn:事务已准备但尚未完成。</para><para>en-us:The transaction is prepared but has no terminal record.</para></summary>
    Prepared,
    /// <summary><para>zh-cn:事务已完成提交。</para><para>en-us:The transaction has been committed.</para></summary>
    Committed,
    /// <summary><para>zh-cn:事务已异常终止。</para><para>en-us:The transaction has been aborted.</para></summary>
    Aborted
}

/// <summary>
/// <para>zh-cn:控制面事务日志记录。</para>
/// <para>en-us:A control-plane transaction log record.</para>
/// </summary>
public sealed class DynamicAppControlPlaneTransactionRecord
{
    /// <summary><para>zh-cn:事务唯一标识。</para><para>en-us:Unique transaction identifier.</para></summary>
    public string TransactionId { get; set; } = string.Empty;
    /// <summary><para>zh-cn:操作名称。</para><para>en-us:Operation name.</para></summary>
    public string Operation { get; set; } = string.Empty;
    /// <summary><para>zh-cn:操作作用域。</para><para>en-us:Operation scope.</para></summary>
    public string Scope { get; set; } = string.Empty;
    /// <summary><para>zh-cn:事务状态。</para><para>en-us:Transaction status.</para></summary>
    public DynamicAppControlPlaneTransactionStatus Status { get; set; }
    /// <summary><para>zh-cn:开始时间。</para><para>en-us:Start time.</para></summary>
    public DateTimeOffset StartedAtUtc { get; set; }
    /// <summary><para>zh-cn:完成时间。</para><para>en-us:Completion time.</para></summary>
    public DateTimeOffset? CompletedAtUtc { get; set; }
    /// <summary><para>zh-cn:异常原因。</para><para>en-us:Failure reason.</para></summary>
    public string? Error { get; set; }
}
