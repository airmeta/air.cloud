namespace Air.Cloud.DynamicApp.Contracts.Runtime;

/// <summary>
/// <para>zh-cn:DynamicApp 控制面操作结果。</para>
/// <para>en-us:DynamicApp control-plane operation result.</para>
/// </summary>
public sealed class DynamicAppOperationResult
{
    /// <summary>
    /// <para>zh-cn:操作是否成功。</para>
    /// <para>en-us:Whether the operation succeeded.</para>
    /// </summary>
    public bool Succeeded { get; init; }

    /// <summary>
    /// <para>zh-cn:操作消息，用于管理端显示或日志记录。</para>
    /// <para>en-us:Operation message for management display or logging.</para>
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:创建成功结果。</para>
    /// <para>en-us:Creates a successful result.</para>
    /// </summary>
    /// <param name="message">
    /// <para>zh-cn:成功消息。</para>
    /// <para>en-us:Success message.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:成功操作结果。</para>
    /// <para>en-us:A successful operation result.</para>
    /// </returns>
    public static DynamicAppOperationResult Success(string message) => new()
    {
        Succeeded = true,
        Message = message
    };

    /// <summary>
    /// <para>zh-cn:创建失败结果。</para>
    /// <para>en-us:Creates a failed result.</para>
    /// </summary>
    /// <param name="message">
    /// <para>zh-cn:失败消息。</para>
    /// <para>en-us:Failure message.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:失败操作结果。</para>
    /// <para>en-us:A failed operation result.</para>
    /// </returns>
    public static DynamicAppOperationResult Failure(string message) => new()
    {
        Succeeded = false,
        Message = message
    };
}
