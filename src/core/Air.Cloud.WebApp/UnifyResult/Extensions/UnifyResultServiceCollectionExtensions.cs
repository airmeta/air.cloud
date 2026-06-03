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
using Air.Cloud.Core.Extensions;
using Air.Cloud.WebApp.Extensions;
using Air.Cloud.WebApp.UnifyResult;
using Air.Cloud.WebApp.UnifyResult.Attributes;
using Air.Cloud.WebApp.UnifyResult.Conventions;
using Air.Cloud.WebApp.UnifyResult.Filters;
using Air.Cloud.WebApp.UnifyResult.Options;
using Air.Cloud.WebApp.UnifyResult.Providers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using System.Reflection;

namespace Air.Cloud.WebApp.UnifyResult.Extensions;

/// <summary>
/// 规范化结果服务拓展
/// </summary>
[IgnoreScanning]
public static class UnifyResultServiceCollectionExtensions
{
    /// <summary>
    /// <para>zh-cn:添加默认规范化结果服务。</para>
    /// <para>en-us:Adds the default unified result service.</para>
    /// </summary>
    /// <param name="mvcBuilder"></param>
    /// <returns></returns>
    public static IMvcBuilder AddUnifyResult(this IMvcBuilder mvcBuilder)
    {
        mvcBuilder.Services.AddUnifyResult<RESTfulResultProvider>();

        return mvcBuilder;
    }

    /// <summary>
    /// <para>zh-cn:添加默认规范化结果服务。</para>
    /// <para>en-us:Adds the default unified result service.</para>
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddUnifyResult(this IServiceCollection services)
    {
        services.AddUnifyResult<RESTfulResultProvider>();

        return services;
    }

    /// <summary>
    /// <para>zh-cn:添加指定提供器的规范化结果服务。</para>
    /// <para>en-us:Adds unified result service with the specified provider.</para>
    /// </summary>
    /// <typeparam name="TUnifyResultProvider"></typeparam>
    /// <param name="mvcBuilder"></param>
    /// <returns></returns>
    public static IMvcBuilder AddUnifyResult<TUnifyResultProvider>(this IMvcBuilder mvcBuilder)
        where TUnifyResultProvider : class, IUnifyResultProvider
    {
        mvcBuilder.Services.AddUnifyResult<TUnifyResultProvider>();

        return mvcBuilder;
    }

    /// <summary>
    /// <para>zh-cn:添加指定提供器的规范化结果服务。</para>
    /// <para>en-us:Adds unified result service with the specified provider.</para>
    /// </summary>
    /// <typeparam name="TUnifyResultProvider"></typeparam>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddUnifyResult<TUnifyResultProvider>(this IServiceCollection services)
        where TUnifyResultProvider : class, IUnifyResultProvider
    {
        // 添加配置
        // Add configuration support.
        services.AddConfigurableOptions<UnifyResultSettingsOptions>();

        // 获取规范化提供器模型
        // Resolve the unified result model from the provider metadata.
        var resultModelType = ResolveUnifyResultModelType<TUnifyResultProvider>();

        services.Configure<UnifyResultRuntimeOptions>(options =>
        {
            options.Enabled = true;
            options.ResultModelType = resultModelType;
        });

        // 添加规范化提供器
        // Register the unified result provider.
        services.AddTransient<IUnifyResultProvider, TUnifyResultProvider>();

        // 添加成功规范化结果筛选器
        // Add the successful response unified result filter.
        services.AddMvcFilter<SucceededUnifyResultFilter>();

        services.AddTransient<IPostConfigureOptions<MvcOptions>, UnifyResultMvcOptionsSetup>();

        return services;
    }

    private static Type ResolveUnifyResultModelType<TUnifyResultProvider>()
        where TUnifyResultProvider : class, IUnifyResultProvider
    {
        var providerType = typeof(TUnifyResultProvider);
        var unifyModelAttribute = providerType.GetCustomAttribute<UnifyModelAttribute>();
        if (unifyModelAttribute?.ModelType == null)
        {
            throw new InvalidOperationException(
                $"{providerType.FullName} must be marked with {nameof(UnifyModelAttribute)} and provide a valid model type.");
        }

        var modelType = unifyModelAttribute.ModelType;
        if (!modelType.IsGenericTypeDefinition || modelType.GetGenericArguments().Length != 1)
        {
            throw new InvalidOperationException(
                $"{providerType.FullName} {nameof(UnifyModelAttribute)} model type {modelType.FullName} must be an open generic type definition with exactly one generic parameter, such as RESTfulResult<>.");
        }

        return modelType;
    }
}
