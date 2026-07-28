using Air.Cloud.DynamicApp.Contracts.Manifest;

namespace Air.Cloud.DynamicApp.Contracts.Runtime;

/// <summary>
/// <para>zh-cn:DynamicApp 清单校验器接口。</para>
/// <para>en-us:DynamicApp manifest validator interface.</para>
/// </summary>
public interface IDynamicAppManifestValidator
{
    /// <summary>
    /// <para>zh-cn:校验 DynamicApp 清单的结构、安全边界和运行必需字段。</para>
    /// <para>en-us:Validates the structure, security boundary, and required runtime fields of a DynamicApp manifest.</para>
    /// </summary>
    /// <param name="manifest">
    /// <para>zh-cn:待校验清单。</para>
    /// <para>en-us:The manifest to validate.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:清单校验结果。</para>
    /// <para>en-us:The manifest validation result.</para>
    /// </returns>
    DynamicAppManifestValidationResult Validate(DynamicAppManifest manifest);
}
