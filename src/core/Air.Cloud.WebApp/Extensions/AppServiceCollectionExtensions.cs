// Copyright (c) 2020-2022 百小僧, Baiqian Co.,Ltd.
// Furion is licensed under Mulan PSL v2.
// You can use this software according to the terms and conditions of the Mulan PSL v2.
// You may obtain a copy of Mulan PSL v2 at:
//             https://gitee.com/dotnetchina/Furion/blob/master/LICENSE
// THIS SOFTWARE IS PROVIDED ON AN "AS IS" BASIS, WITHOUT WARRANTIES OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO NON-INFRINGEMENT, MERCHANTABILITY OR FIT FOR A PARTICULAR PURPOSE.
// See the Mulan PSL v2 for more details.

using Air.Cloud.WebApp.DataValidation.Extensions;
using Air.Cloud.WebApp.DynamicApiController.Extensions;
using Air.Cloud.WebApp.Extensions.Options;
using Air.Cloud.WebApp.FriendlyException.Extensions;
using Air.Cloud.WebApp.UnifyResult.Extensions;
using Air.Cloud.WebApp.UnifyResult.Providers;

using Microsoft.Extensions.DependencyInjection;

namespace Air.Cloud.WebApp.Extensions;

/// <summary>
/// 应用服务集合拓展类（由框架内部调用）
/// </summary>
[IgnoreScanning]
public static class AppServiceCollectionExtensions
{
    /// <summary>
    /// Mvc 注入基础配置（带Swagger）
    /// </summary>
    /// <param name="mvcBuilder">Mvc构建器</param>
    /// <param name="configure"></param>
    /// <returns>IMvcBuilder</returns>
    public static IMvcBuilder AddInject(this IMvcBuilder mvcBuilder, Action<InjectServiceOptions> configure = null)
    {
        mvcBuilder.Services.AddInject(configure);

        return mvcBuilder;
    }

    /// <summary>
    /// 服务注入基础配置（带Swagger）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configure"></param>
    /// <returns>IMvcBuilder</returns>
    public static IServiceCollection AddInject(this IServiceCollection services, Action<InjectServiceOptions> configure = null)
    {
        // 载入服务配置选项
        var configureOptions = new InjectServiceOptions();
        configure?.Invoke(configureOptions);

        services.AddDynamicApiControllers()
                .AddDataValidation(configureOptions?.DataValidationConfigure)
                .AddFriendlyException(configureOptions?.FriendlyExceptionConfigure);

        return services;
    }

    /// <summary>
    /// MiniAPI 服务注入基础配置（带Swagger）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configure"></param>
    /// <returns>IMvcBuilder</returns>
    /// <remarks>https://docs.microsoft.com/zh-cn/aspnet/core/fundamentals/minimal-apis?view=aspnetcore-6.0</remarks>
    public static IServiceCollection AddInjectMini(this IServiceCollection services, Action<InjectServiceOptions> configure = null)
    {
        // 载入服务配置选项
        var configureOptions = new InjectServiceOptions();
        configure?.Invoke(configureOptions);

        services.AddEndpointsApiExplorer()
                .AddDataValidation(configureOptions?.DataValidationConfigure)
                .AddFriendlyException(configureOptions?.FriendlyExceptionConfigure);

        return services;
    }

    /// <summary>
    /// Mvc 注入基础配置
    /// </summary>
    /// <param name="mvcBuilder">Mvc构建器</param>
    /// <param name="configure"></param>
    /// <returns>IMvcBuilder</returns>
    public static IMvcBuilder AddInjectBase(this IMvcBuilder mvcBuilder, Action<InjectServiceOptions> configure = null)
    {
        mvcBuilder.Services.AddInjectBase(configure);

        return mvcBuilder;
    }

    /// <summary>
    /// Mvc 注入基础配置
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configure"></param>
    /// <returns>IMvcBuilder</returns>
    public static IServiceCollection AddInjectBase(this IServiceCollection services, Action<InjectServiceOptions> configure = null)
    {
        // 载入服务配置选项
        var configureOptions = new InjectServiceOptions();
        configure?.Invoke(configureOptions);

        services.AddDataValidation(configureOptions?.DataValidationConfigure)
                .AddFriendlyException(configureOptions?.FriendlyExceptionConfigure);

        return services;
    }

    /// <summary>
    /// Mvc 注入基础配置和规范化结果
    /// </summary>
    /// <param name="mvcBuilder"></param>
    /// <param name="configure"></param>
    /// <returns></returns>
    public static IMvcBuilder AddInjectWithUnifyResult(this IMvcBuilder mvcBuilder, Action<InjectServiceOptions> configure = null)
    {
        mvcBuilder.Services.AddInjectWithUnifyResult(configure);

        return mvcBuilder;
    }

    /// <summary>
    /// 注入基础配置和规范化结果
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configure"></param>
    /// <returns></returns>
    public static IServiceCollection AddInjectWithUnifyResult(this IServiceCollection services, Action<InjectServiceOptions> configure = null)
    {
        services.AddInject(configure)
                .AddUnifyResult();

        return services;
    }

    /// <summary>
    /// Mvc 注入基础配置和规范化结果
    /// </summary>
    /// <typeparam name="TUnifyResultProvider"></typeparam>
    /// <param name="mvcBuilder"></param>
    /// <param name="configure"></param>
    /// <returns></returns>
    public static IMvcBuilder AddInjectWithUnifyResult<TUnifyResultProvider>(this IMvcBuilder mvcBuilder, Action<InjectServiceOptions> configure = null)
        where TUnifyResultProvider : class, IUnifyResultProvider
    {
        mvcBuilder.Services.AddInjectWithUnifyResult<TUnifyResultProvider>(configure);

        return mvcBuilder;
    }

    /// <summary>
    /// Mvc 注入基础配置和规范化结果
    /// </summary>
    /// <typeparam name="TUnifyResultProvider"></typeparam>
    /// <param name="configure"></param>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddInjectWithUnifyResult<TUnifyResultProvider>(this IServiceCollection services, Action<InjectServiceOptions> configure = null)
        where TUnifyResultProvider : class, IUnifyResultProvider
    {
        services.AddInject(configure)
                .AddUnifyResult<TUnifyResultProvider>();

        return services;
    }

    /// <summary>
    /// 解决 .NET6 WebApplication 模式下二级虚拟目录错误问题
    /// </summary>
    /// <param name="app"></param>
    /// <returns></returns>
    public static IApplicationBuilder MapRouteControllers(this IApplicationBuilder app)
    {
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });

        return app;
    }

}