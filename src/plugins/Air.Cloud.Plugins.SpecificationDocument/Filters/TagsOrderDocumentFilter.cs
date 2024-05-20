// Copyright (c) 2020-2022 百小僧, Baiqian Co.,Ltd.
// Furion is licensed under Mulan PSL v2.
// You can use this software according to the terms and conditions of the Mulan PSL v2.
// You may obtain a copy of Mulan PSL v2 at:
//             https://gitee.com/dotnetchina/Furion/blob/master/LICENSE
// THIS SOFTWARE IS PROVIDED ON AN "AS IS" BASIS, WITHOUT WARRANTIES OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO NON-INFRINGEMENT, MERCHANTABILITY OR FIT FOR A PARTICULAR PURPOSE.
// See the Mulan PSL v2 for more details.

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