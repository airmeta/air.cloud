using System.Text.Json;
using System.Text.Json.Serialization;

using Air.Cloud.DynamicApp.Contracts.Runtime;

namespace Air.Cloud.DynamicApp.Manager;

/// <summary>
/// <para>zh-cn:基于 JSONL 的控制面事务日志；每个状态变更追加一条记录，启动时可识别未提交操作。</para>
/// <para>en-us:JSONL-based control-plane transaction log; each lifecycle mutation appends a record and startup can identify operations without a terminal commit.</para>
/// </summary>
public sealed class FileDynamicAppControlPlaneTransactionLog : IDynamicAppControlPlaneTransactionLog
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
    private readonly string _filePath;

    /// <summary>
    /// <para>zh-cn:创建事务日志并在启动时恢复未完成记录。</para>
    /// <para>en-us:Creates the transaction log and recovers incomplete records during startup.</para>
    /// </summary>
    public FileDynamicAppControlPlaneTransactionLog(FileDynamicAppControlPlaneTransactionLogOptions? options = null)
    {
        _filePath = ResolveFilePath(options?.FilePath);
        RecoverIncomplete();
    }

    /// <inheritdoc />
    public string Begin(string operation, string scope)
    {
        var record = new DynamicAppControlPlaneTransactionRecord
        {
            TransactionId = Guid.NewGuid().ToString("N"),
            Operation = operation,
            Scope = scope,
            Status = DynamicAppControlPlaneTransactionStatus.Prepared,
            StartedAtUtc = DateTimeOffset.UtcNow
        };
        Append(record);
        return record.TransactionId;
    }

    /// <inheritdoc />
    public void Commit(string transactionId) => Append(new DynamicAppControlPlaneTransactionRecord
    {
        TransactionId = transactionId,
        Status = DynamicAppControlPlaneTransactionStatus.Committed,
        CompletedAtUtc = DateTimeOffset.UtcNow
    });

    /// <inheritdoc />
    public void Abort(string transactionId, string error) => Append(new DynamicAppControlPlaneTransactionRecord
    {
        TransactionId = transactionId,
        Status = DynamicAppControlPlaneTransactionStatus.Aborted,
        CompletedAtUtc = DateTimeOffset.UtcNow,
        Error = error
    });

    /// <inheritdoc />
    public IReadOnlyCollection<DynamicAppControlPlaneTransactionRecord> RecoverIncomplete()
    {
        using var fileLock = DynamicAppFileSnapshotLock.Acquire(_filePath);
        var records = ReadRecordsUnsafe();
        var incomplete = records.GroupBy(item => item.TransactionId, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.Last())
            .Where(item => item.Status == DynamicAppControlPlaneTransactionStatus.Prepared)
            .ToArray();
        foreach (var record in incomplete)
        {
            AppendUnsafe(new DynamicAppControlPlaneTransactionRecord
            {
                TransactionId = record.TransactionId,
                Operation = record.Operation,
                Scope = record.Scope,
                Status = DynamicAppControlPlaneTransactionStatus.Aborted,
                StartedAtUtc = record.StartedAtUtc,
                CompletedAtUtc = DateTimeOffset.UtcNow,
                Error = "Recovered incomplete control-plane transaction during Host startup."
            });
        }

        return incomplete;
    }

    private void Append(DynamicAppControlPlaneTransactionRecord record)
    {
        using var fileLock = DynamicAppFileSnapshotLock.Acquire(_filePath);
        AppendUnsafe(record);
    }

    private void AppendUnsafe(DynamicAppControlPlaneTransactionRecord record)
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
        using var stream = new FileStream(_filePath, FileMode.Append, FileAccess.Write, FileShare.Read,
            bufferSize: 4096, options: FileOptions.WriteThrough);
        using var writer = new StreamWriter(stream);
        writer.WriteLine(JsonSerializer.Serialize(record, JsonOptions));
        writer.Flush();
        stream.Flush(flushToDisk: true);
    }

    private List<DynamicAppControlPlaneTransactionRecord> ReadRecordsUnsafe()
    {
        if (!File.Exists(_filePath)) return new List<DynamicAppControlPlaneTransactionRecord>();
        var records = new List<DynamicAppControlPlaneTransactionRecord>();
        try
        {
            foreach (var line in File.ReadLines(_filePath))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var record = JsonSerializer.Deserialize<DynamicAppControlPlaneTransactionRecord>(line, JsonOptions);
                if (record != null) records.Add(record);
            }
        }
        catch (IOException) { }
        catch (JsonException) { }
        return records;
    }

    private static string ResolveFilePath(string? filePath)
        => string.IsNullOrWhiteSpace(filePath)
            ? Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "data", "dynamicapp-control-plane-transactions.jsonl"))
            : Path.IsPathFullyQualified(filePath) ? Path.GetFullPath(filePath) : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, filePath));
}

/// <summary>
/// <para>zh-cn:文件型控制面事务日志配置。</para>
/// <para>en-us:Options for the file-based control-plane transaction log.</para>
/// </summary>
public sealed class FileDynamicAppControlPlaneTransactionLogOptions
{
    /// <summary>
    /// <para>zh-cn:日志文件路径；为空时写入 data/dynamicapp-control-plane-transactions.jsonl。</para>
    /// <para>en-us:Log file path; defaults to data/dynamicapp-control-plane-transactions.jsonl.</para>
    /// </summary>
    public string? FilePath { get; set; }
}
