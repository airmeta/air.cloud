





//Furion

using Air.Cloud.Core.Attributes;
using Air.Cloud.WebApp.DynamicApiController.Internal;

using Microsoft.OpenApi.Models;

using Swashbuckle.AspNetCore.SwaggerGen;

namespace Air.Cloud.Plugins.SpecificationDocument.Filters;

/// <summary>
/// 标签文档排序拦截器
/// </summary>
[IgnoreScanning]
public class TagsOrderDocumentFilter : IDocumentFilter
{
    /// <summary>
    /// 配置拦截
    /// </summary>
    /// <param name="swaggerDoc"></param>
    /// <param name="context"></param>
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        swaggerDoc.Tags = swaggerDoc.Tags
                                    .OrderByDescending(u => GetTagOrder(u.Name))
                                    .ThenBy(u => u.Name)
                                    .ToArray();
    }

    /// <summary>
    /// 获取标签排序
    /// </summary>
    /// <param name="tag"></param>
    /// <returns></returns>
    private static int GetTagOrder(string tag)
    {
        var isExist = Penetrates.ControllerOrderCollection.TryGetValue(tag, out var order);
        return isExist ? order : default;
    }
}