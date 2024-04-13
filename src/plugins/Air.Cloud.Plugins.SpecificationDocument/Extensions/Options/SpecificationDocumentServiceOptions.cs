





//Furion

using Swashbuckle.AspNetCore.SwaggerGen;

namespace Air.Cloud.Plugins.SpecificationDocument.Extensions.Options;

/// <summary>
/// 规范化文档服务配置选项
/// </summary>
public sealed class SpecificationDocumentServiceOptions
{
    /// <summary>
    /// Swagger 生成器配置
    /// </summary>
    public Action<SwaggerGenOptions> SwaggerGenConfigure { get; set; }
}