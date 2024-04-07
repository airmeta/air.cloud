// Copyright (c) 2020-2022 百小僧, Baiqian Co.,Ltd.
// Furion is licensed under Mulan PSL v2.
// You can use this software according to the terms and conditions of the Mulan PSL v2.
// You may obtain a copy of Mulan PSL v2 at:
//             https://gitee.com/dotnetchina/Furion/blob/master/LICENSE
// THIS SOFTWARE IS PROVIDED ON AN "AS IS" BASIS, WITHOUT WARRANTIES OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO NON-INFRINGEMENT, MERCHANTABILITY OR FIT FOR A PARTICULAR PURPOSE.
// See the Mulan PSL v2 for more details.


using Air.Cloud.Core.App;
using Air.Cloud.Core.Standard;

namespace Air.Cloud.WebApp.Extensions;

/// <summary>
/// WebApplication 拓展
/// </summary>
public static class AppWebApplicationBuilderExtensions
{
    /// <summary>
    /// Web 应用注入
    /// </summary>
    /// <param name="webApplicationBuilder">Web应用构建器</param>
    /// <returns>IWebHostBuilder</returns>
    public static WebApplicationBuilder Inject(this WebApplicationBuilder webApplicationBuilder)
    {
        webApplicationBuilder = webApplicationBuilder.InjectWebApplicationBuilder();
        AppCore.ConfigureApplication(webApplicationBuilder.WebHost, webApplicationBuilder.Host);
        return webApplicationBuilder;
    }
}