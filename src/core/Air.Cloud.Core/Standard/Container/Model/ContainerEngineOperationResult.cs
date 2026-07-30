namespace Air.Cloud.Core.Standard.Container.Model;

/// <summary>
/// <para>zh-cn:容器引擎操作结果，表示启动、停止等生命周期操作是否成功以及平台返回的状态说明。</para>
/// <para>en-us:Container engine operation result indicating whether lifecycle operations such as start and stop succeeded and carrying platform status details.</para>
/// </summary>
public sealed class ContainerEngineOperationResult
{
    private ContainerEngineOperationResult(bool succeeded, string message)
    {
        Succeeded = succeeded;
        Message = message;
    }

    /// <summary>
    /// <para>zh-cn:操作是否成功。</para>
    /// <para>en-us:Whether the operation succeeded.</para>
    /// </summary>
    public bool Succeeded { get; }

    /// <summary>
    /// <para>zh-cn:操作结果说明。</para>
    /// <para>en-us:The operation result message.</para>
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// <para>zh-cn:创建成功结果。</para>
    /// <para>en-us:Creates a successful result.</para>
    /// </summary>
    /// <param name="message">
    /// <para>zh-cn:成功说明。</para>
    /// <para>en-us:The success message.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:成功结果。</para>
    /// <para>en-us:The successful result.</para>
    /// </returns>
    public static ContainerEngineOperationResult Success(string message) => new(true, message);

    /// <summary>
    /// <para>zh-cn:创建失败结果。</para>
    /// <para>en-us:Creates a failed result.</para>
    /// </summary>
    /// <param name="message">
    /// <para>zh-cn:失败说明。</para>
    /// <para>en-us:The failure message.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:失败结果。</para>
    /// <para>en-us:The failed result.</para>
    /// </returns>
    public static ContainerEngineOperationResult Failure(string message) => new(false, message);
}
