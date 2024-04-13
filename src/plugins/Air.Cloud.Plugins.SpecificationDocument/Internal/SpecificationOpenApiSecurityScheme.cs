





//Furion








//Furion

using Microsoft.OpenApi.Models;

namespace Air.Cloud.Plugins.SpecificationDocument.Internal;

/// <summary>
/// 规范化稳定安全配置
/// </summary>
public sealed class SpecificationOpenApiSecurityScheme : OpenApiSecurityScheme
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public SpecificationOpenApiSecurityScheme()
    {
    }

    /// <summary>
    /// 唯一Id
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// 安全需求
    /// </summary>
    public SpecificationOpenApiSecurityRequirementItem Requirement { get; set; }
}