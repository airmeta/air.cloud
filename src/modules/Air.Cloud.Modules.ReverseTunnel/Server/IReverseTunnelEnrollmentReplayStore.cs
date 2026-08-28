namespace Air.Cloud.Modules.ReverseTunnel.Server;

/// <summary>
/// <para>zh-cn:首次 Enrollment GUID 的重放保护存储。实现必须原子地判断并占用 GUID，并在窗口到期后允许清理。</para>
/// <para>en-us:Replay-protection store for initial enrollment GUIDs. Implementations must atomically check and claim a GUID and may remove entries after the replay window expires.</para>
/// </summary>
public interface IReverseTunnelEnrollmentReplayStore
{
    /// <summary>
    /// <para>zh-cn:尝试占用本次 Enrollment 的 GUID。相同 GUID 在 replay 窗口内只能成功一次。</para>
    /// <para>en-us:Attempts to claim an enrollment GUID. The same GUID can succeed only once inside the replay window.</para>
    /// </summary>
    /// <param name="agentId"><para>zh-cn:请求证书的 AgentId。</para><para>en-us:AgentId requesting a certificate.</para></param>
    /// <param name="guid"><para>zh-cn:已解密的随机 GUID。</para><para>en-us:Decrypted random GUID.</para></param>
    /// <param name="utcNow"><para>zh-cn:当前 UTC 时间。</para><para>en-us:Current UTC time.</para></param>
    /// <param name="replayWindow"><para>zh-cn:重放保护窗口。</para><para>en-us:Replay-protection window.</para></param>
    /// <returns><para>zh-cn:首次占用返回 true，重复或无效输入返回 false。</para><para>en-us:True for the first claim, false for a duplicate or invalid input.</para></returns>
    bool TryClaim(string agentId, Guid guid, DateTimeOffset utcNow, TimeSpan replayWindow);
}
