/*
 * Copyright (c) 2024-2030 星曳数据
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 *
 * This file is provided under the Mozilla Public License Version 2.0,
 * and the "NO WARRANTY" clause of the MPL is hereby expressly
 * acknowledged.
 */
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
    /// <para>zh-cn:注入规范化响应</para>
    /// <para>en-us:Inject standardized response</para>
    /// </summary>
    /// <param name="mvcBuilder">
    ///  <para>zh-cn:Mvc构建器</para>
    ///  <para>en-us:Mvc builder</para>
    /// </param>
    /// <param name="configure">
    ///  <para>zh-cn:配置选项</para>
    ///  <para>en-us:Configuration options</para>
    /// </param>
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
}