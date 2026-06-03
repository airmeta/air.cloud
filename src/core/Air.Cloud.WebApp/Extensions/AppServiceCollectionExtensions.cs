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
    /// <para>zh-cn:添加 WebApp 核心能力，不包含统一返回结果。</para>
    /// <para>en-us:Adds WebApp core capabilities without unified response result handling.</para>
    /// </summary>
    /// <param name="mvcBuilder">
    /// <para>zh-cn:MVC 构建器，用于继续链式配置 MVC 相关能力。</para>
    /// <para>en-us:MVC builder used to continue chained MVC configuration.</para>
    /// </param>
    /// <param name="configure">
    /// <para>zh-cn:WebApp 核心能力配置，包括数据验证和友好异常配置。</para>
    /// <para>en-us:WebApp core configuration, including data validation and friendly exception options.</para>
    /// </param>
    /// <returns>IMvcBuilder</returns>
    public static IMvcBuilder AddWebAppCore(this IMvcBuilder mvcBuilder, Action<InjectServiceOptions> configure = null)
    {
        mvcBuilder.Services.AddWebAppCore(configure);

        return mvcBuilder;
    }

    /// <summary>
    /// <para>zh-cn:添加 WebApp 核心能力，不包含统一返回结果。</para>
    /// <para>en-us:Adds WebApp core capabilities without unified response result handling.</para>
    /// </summary>
    /// <param name="services">
    /// <para>zh-cn:服务集合，用于注册 WebApp 核心依赖。</para>
    /// <para>en-us:Service collection used to register WebApp core dependencies.</para>
    /// </param>
    /// <param name="configure">
    /// <para>zh-cn:WebApp 核心能力配置，包括数据验证和友好异常配置。</para>
    /// <para>en-us:WebApp core configuration, including data validation and friendly exception options.</para>
    /// </param>
    /// <returns>IServiceCollection</returns>
    public static IServiceCollection AddWebAppCore(this IServiceCollection services, Action<InjectServiceOptions> configure = null)
    {
        // 载入服务配置选项
        // Load service configuration options.
        var configureOptions = new InjectServiceOptions();
        configure?.Invoke(configureOptions);
      
        services.AddDynamicApiControllers()
                .AddDataValidation(configureOptions?.DataValidationConfigure)
                .AddFriendlyException(configureOptions?.FriendlyExceptionConfigure);

        return services;
    }

    /// <summary>
    /// <para>zh-cn:添加 WebApp 统一返回结果能力，不包含动态 API、数据验证和友好异常。</para>
    /// <para>en-us:Adds WebApp unified response result handling without dynamic APIs, data validation, or friendly exceptions.</para>
    /// </summary>
    /// <param name="mvcBuilder">
    /// <para>zh-cn:MVC 构建器，用于继续链式配置 MVC 相关能力。</para>
    /// <para>en-us:MVC builder used to continue chained MVC configuration.</para>
    /// </param>
    /// <returns>IMvcBuilder</returns>
    public static IMvcBuilder AddWebAppUnifyResult(this IMvcBuilder mvcBuilder)
    {
        mvcBuilder.Services.AddWebAppUnifyResult();
        return mvcBuilder;
    }

    /// <summary>
    /// <para>zh-cn:添加 WebApp 统一返回结果能力，不包含动态 API、数据验证和友好异常。</para>
    /// <para>en-us:Adds WebApp unified response result handling without dynamic APIs, data validation, or friendly exceptions.</para>
    /// </summary>
    /// <param name="services">
    /// <para>zh-cn:服务集合，用于注册统一返回结果依赖。</para>
    /// <para>en-us:Service collection used to register unified response result dependencies.</para>
    /// </param>
    /// <returns>IServiceCollection</returns>
    public static IServiceCollection AddWebAppUnifyResult(this IServiceCollection services)
    {
        services.AddUnifyResult();

        return services;
    }

    /// <summary>
    /// <para>zh-cn:添加 WebApp 统一返回结果能力，并使用指定的统一返回提供器。</para>
    /// <para>en-us:Adds WebApp unified response result handling with the specified unified result provider.</para>
    /// </summary>
    /// <typeparam name="TUnifyResultProvider">
    /// <para>zh-cn:统一返回结果提供器类型，必须实现 IUnifyResultProvider。</para>
    /// <para>en-us:Unified response result provider type, which must implement IUnifyResultProvider.</para>
    /// </typeparam>
    /// <param name="mvcBuilder">
    /// <para>zh-cn:MVC 构建器，用于继续链式配置 MVC 相关能力。</para>
    /// <para>en-us:MVC builder used to continue chained MVC configuration.</para>
    /// </param>
    /// <returns>IMvcBuilder</returns>
    public static IMvcBuilder AddWebAppUnifyResult<TUnifyResultProvider>(this IMvcBuilder mvcBuilder)
        where TUnifyResultProvider : class, IUnifyResultProvider
    {
        mvcBuilder.Services.AddWebAppUnifyResult<TUnifyResultProvider>();

        return mvcBuilder;
    }

    /// <summary>
    /// <para>zh-cn:添加 WebApp 统一返回结果能力，并使用指定的统一返回提供器。</para>
    /// <para>en-us:Adds WebApp unified response result handling with the specified unified result provider.</para>
    /// </summary>
    /// <typeparam name="TUnifyResultProvider">
    /// <para>zh-cn:统一返回结果提供器类型，必须实现 IUnifyResultProvider。</para>
    /// <para>en-us:Unified response result provider type, which must implement IUnifyResultProvider.</para>
    /// </typeparam>
    /// <param name="services">
    /// <para>zh-cn:服务集合，用于注册统一返回结果依赖。</para>
    /// <para>en-us:Service collection used to register unified response result dependencies.</para>
    /// </param>
    /// <returns>IServiceCollection</returns>
    public static IServiceCollection AddWebAppUnifyResult<TUnifyResultProvider>(this IServiceCollection services)
        where TUnifyResultProvider : class, IUnifyResultProvider
    {
        services.AddUnifyResult<TUnifyResultProvider>();

        return services;
    }

    /// <summary>
    /// <para>zh-cn:添加完整 WebApp 能力，包含核心能力和默认统一返回结果。</para>
    /// <para>en-us:Adds full WebApp capabilities, including core capabilities and default unified response result handling.</para>
    /// </summary>
    /// <param name="mvcBuilder">
    /// <para>zh-cn:MVC 构建器，用于继续链式配置 MVC 相关能力。</para>
    /// <para>en-us:MVC builder used to continue chained MVC configuration.</para>
    /// </param>
    /// <param name="configure">
    /// <para>zh-cn:WebApp 核心能力配置，包括数据验证和友好异常配置。</para>
    /// <para>en-us:WebApp core configuration, including data validation and friendly exception options.</para>
    /// </param>
    /// <returns>IMvcBuilder</returns>
    public static IMvcBuilder AddWebApp(this IMvcBuilder mvcBuilder, Action<InjectServiceOptions> configure = null)
    {
        mvcBuilder.Services.AddWebApp(configure);

        return mvcBuilder;
    }

    /// <summary>
    /// <para>zh-cn:添加完整 WebApp 能力，包含核心能力和默认统一返回结果。</para>
    /// <para>en-us:Adds full WebApp capabilities, including core capabilities and default unified response result handling.</para>
    /// </summary>
    /// <param name="services">
    /// <para>zh-cn:服务集合，用于注册完整 WebApp 依赖。</para>
    /// <para>en-us:Service collection used to register full WebApp dependencies.</para>
    /// </param>
    /// <param name="configure">
    /// <para>zh-cn:WebApp 核心能力配置，包括数据验证和友好异常配置。</para>
    /// <para>en-us:WebApp core configuration, including data validation and friendly exception options.</para>
    /// </param>
    /// <returns>IServiceCollection</returns>
    public static IServiceCollection AddWebApp(this IServiceCollection services, Action<InjectServiceOptions> configure = null)
    {
        services.AddWebAppCore(configure)
                .AddWebAppUnifyResult();

        return services;
    }

    /// <summary>
    /// <para>zh-cn:添加完整 WebApp 能力，包含核心能力和指定统一返回提供器。</para>
    /// <para>en-us:Adds full WebApp capabilities, including core capabilities and the specified unified result provider.</para>
    /// </summary>
    /// <typeparam name="TUnifyResultProvider">
    /// <para>zh-cn:统一返回结果提供器类型，必须实现 IUnifyResultProvider。</para>
    /// <para>en-us:Unified response result provider type, which must implement IUnifyResultProvider.</para>
    /// </typeparam>
    /// <param name="mvcBuilder">
    /// <para>zh-cn:MVC 构建器，用于继续链式配置 MVC 相关能力。</para>
    /// <para>en-us:MVC builder used to continue chained MVC configuration.</para>
    /// </param>
    /// <param name="configure">
    /// <para>zh-cn:WebApp 核心能力配置，包括数据验证和友好异常配置。</para>
    /// <para>en-us:WebApp core configuration, including data validation and friendly exception options.</para>
    /// </param>
    /// <returns>IMvcBuilder</returns>
    public static IMvcBuilder AddWebApp<TUnifyResultProvider>(this IMvcBuilder mvcBuilder, Action<InjectServiceOptions> configure = null)
        where TUnifyResultProvider : class, IUnifyResultProvider
    {
        mvcBuilder.Services.AddWebApp<TUnifyResultProvider>(configure);

        return mvcBuilder;
    }

    /// <summary>
    /// <para>zh-cn:添加完整 WebApp 能力，包含核心能力和指定统一返回提供器。</para>
    /// <para>en-us:Adds full WebApp capabilities, including core capabilities and the specified unified result provider.</para>
    /// </summary>
    /// <typeparam name="TUnifyResultProvider">
    /// <para>zh-cn:统一返回结果提供器类型，必须实现 IUnifyResultProvider。</para>
    /// <para>en-us:Unified response result provider type, which must implement IUnifyResultProvider.</para>
    /// </typeparam>
    /// <param name="services">
    /// <para>zh-cn:服务集合，用于注册完整 WebApp 依赖。</para>
    /// <para>en-us:Service collection used to register full WebApp dependencies.</para>
    /// </param>
    /// <param name="configure">
    /// <para>zh-cn:WebApp 核心能力配置，包括数据验证和友好异常配置。</para>
    /// <para>en-us:WebApp core configuration, including data validation and friendly exception options.</para>
    /// </param>
    /// <returns>IServiceCollection</returns>
    public static IServiceCollection AddWebApp<TUnifyResultProvider>(this IServiceCollection services, Action<InjectServiceOptions> configure = null)
        where TUnifyResultProvider : class, IUnifyResultProvider
    {
        services.AddWebAppCore(configure)
                .AddWebAppUnifyResult<TUnifyResultProvider>();

        return services;
    }

}
