namespace Air.Cloud.DynamicApp.Contracts.Runtime;

/// <summary>
/// <para>zh-cn:DynamicApp 清单校验结果。</para>
/// <para>en-us:DynamicApp manifest validation result.</para>
/// </summary>
public sealed class DynamicAppManifestValidationResult
{
    /// <summary>
    /// <para>zh-cn:是否校验通过；当 Errors 为空时为 true。</para>
    /// <para>en-us:Whether validation passed; true when Errors is empty.</para>
    /// </summary>
    public bool IsValid => Errors.Count == 0;

    /// <summary>
    /// <para>zh-cn:校验错误集合。</para>
    /// <para>en-us:Validation error collection.</para>
    /// </summary>
    public List<string> Errors { get; } = new();
}
