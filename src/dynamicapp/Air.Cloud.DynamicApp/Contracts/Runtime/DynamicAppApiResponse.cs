namespace Air.Cloud.DynamicApp.Contracts.Runtime;

/// <summary>
/// <para>zh-cn:DynamicApp Manager API 的统一响应模型，用于管理端稳定解析成功、错误码、消息和数据负载。</para>
/// <para>en-us:Unified response model for DynamicApp Manager APIs so management clients can parse success, error code, message, and payload consistently.</para>
/// </summary>
/// <typeparam name="T">
/// <para>zh-cn:响应数据负载类型。</para>
/// <para>en-us:Response payload type.</para>
/// </typeparam>
public sealed class DynamicAppApiResponse<T>
{
    /// <summary>
    /// <para>zh-cn:请求是否成功；业务失败和鉴权失败都会返回 false。</para>
    /// <para>en-us:Whether the request succeeded; business failures and authorization failures both return false.</para>
    /// </summary>
    public bool Succeeded { get; init; }

    /// <summary>
    /// <para>zh-cn:可读消息，面向管理端展示和日志记录。</para>
    /// <para>en-us:Human-readable message for management UI display and logging.</para>
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:稳定错误码；成功响应为空字符串。</para>
    /// <para>en-us:Stable error code; successful responses use an empty string.</para>
    /// </summary>
    public string ErrorCode { get; init; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:业务数据负载，失败响应可为空。</para>
    /// <para>en-us:Business payload; failure responses may leave it null.</para>
    /// </summary>
    public T? Data { get; init; }

    /// <summary>
    /// <para>zh-cn:创建成功响应，并携带可选数据负载。</para>
    /// <para>en-us:Creates a successful response with an optional payload.</para>
    /// </summary>
    /// <param name="data">
    /// <para>zh-cn:响应数据负载。</para>
    /// <para>en-us:Response payload.</para>
    /// </param>
    /// <param name="message">
    /// <para>zh-cn:成功消息，未指定时使用默认消息。</para>
    /// <para>en-us:Success message; a default message is used when omitted.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:统一成功响应。</para>
    /// <para>en-us:A unified successful response.</para>
    /// </returns>
    public static DynamicAppApiResponse<T> Success(T? data = default, string message = "OK") => new()
    {
        Succeeded = true,
        Message = message,
        Data = data
    };

    /// <summary>
    /// <para>zh-cn:从控制面操作结果创建统一响应。</para>
    /// <para>en-us:Creates a unified response from a control-plane operation result.</para>
    /// </summary>
    /// <param name="result">
    /// <para>zh-cn:控制面操作结果。</para>
    /// <para>en-us:Control-plane operation result.</para>
    /// </param>
    /// <param name="data">
    /// <para>zh-cn:成功时返回的数据负载。</para>
    /// <para>en-us:Payload returned when the operation succeeded.</para>
    /// </param>
    /// <param name="errorCode">
    /// <para>zh-cn:失败时使用的稳定错误码。</para>
    /// <para>en-us:Stable error code used when the operation failed.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:统一 API 响应。</para>
    /// <para>en-us:A unified API response.</para>
    /// </returns>
    public static DynamicAppApiResponse<T> FromOperation(DynamicAppOperationResult result, T? data = default, string errorCode = "dynamicapp.operation_failed")
    {
        ArgumentNullException.ThrowIfNull(result);
        return result.Succeeded ? Success(data, result.Message) : Failure(result.Message, errorCode);
    }

    /// <summary>
    /// <para>zh-cn:创建失败响应，并携带稳定错误码。</para>
    /// <para>en-us:Creates a failed response with a stable error code.</para>
    /// </summary>
    /// <param name="message">
    /// <para>zh-cn:失败消息。</para>
    /// <para>en-us:Failure message.</para>
    /// </param>
    /// <param name="errorCode">
    /// <para>zh-cn:稳定错误码。</para>
    /// <para>en-us:Stable error code.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:统一失败响应。</para>
    /// <para>en-us:A unified failed response.</para>
    /// </returns>
    public static DynamicAppApiResponse<T> Failure(string message, string errorCode = "dynamicapp.error") => new()
    {
        Succeeded = false,
        Message = message,
        ErrorCode = errorCode
    };
}
