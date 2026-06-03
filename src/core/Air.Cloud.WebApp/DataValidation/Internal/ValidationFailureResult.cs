using Microsoft.AspNetCore.Http;

namespace Air.Cloud.WebApp.DataValidation.Internal;

/// <summary>
/// <para>zh-cn:数据验证失败返回结果，提供给前端稳定解析的错误结构。</para>
/// <para>en-us:Data validation failure response result that provides a stable error structure for frontend parsing.</para>
/// </summary>
[IgnoreScanning]
public sealed class ValidationFailureResult
{
    /// <summary>
    /// <para>zh-cn:HTTP 状态码，默认使用 400 表示请求参数验证失败。</para>
    /// <para>en-us:HTTP status code; defaults to 400 for request parameter validation failures.</para>
    /// </summary>
    public int StatusCode { get; set; } = StatusCodes.Status400BadRequest;

    /// <summary>
    /// <para>zh-cn:业务错误码；当验证失败来自业务异常时会携带业务侧传入的错误码。</para>
    /// <para>en-us:Business error code; populated when the validation failure comes from a business exception.</para>
    /// </summary>
    public object ErrorCode { get; set; }

    /// <summary>
    /// <para>zh-cn:可直接展示给用户的摘要消息，前端可以优先使用该字段做 Toast 或表单顶部提示。</para>
    /// <para>en-us:User-facing summary message; frontend can prefer this field for toast messages or form-level prompts.</para>
    /// </summary>
    public string Message { get; set; } = "请求参数验证失败";

    /// <summary>
    /// <para>zh-cn:按字段分组的验证错误，Key 是字段名，Value 是该字段对应的错误消息列表。</para>
    /// <para>en-us:Field-grouped validation errors where the key is the field name and the value contains messages for that field.</para>
    /// </summary>
    public Dictionary<string, string[]> Fields { get; set; } = new();

    /// <summary>
    /// <para>zh-cn:扁平化错误消息列表，用于不关心字段归属或全局业务验证失败的展示场景。</para>
    /// <para>en-us:Flattened error message list for scenarios that do not need field ownership or global business validation failures.</para>
    /// </summary>
    public string[] Errors { get; set; } = Array.Empty<string>();
}
