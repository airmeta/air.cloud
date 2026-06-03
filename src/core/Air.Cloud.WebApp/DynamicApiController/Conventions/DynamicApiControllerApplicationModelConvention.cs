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
using Air.Cloud.Core.Modules.DynamicApp.Internal;
using Air.Cloud.WebApp.DynamicApiController.Attributes;
using Air.Cloud.WebApp.DynamicApiController.Internal;
using Air.Cloud.WebApp.DynamicApiController.Options;
using Air.Cloud.WebApp.UnifyResult.Options;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.Extensions.Options;

using System.Reflection;

namespace Air.Cloud.WebApp.DynamicApiController.Conventions;

/// <summary>
/// 动态接口控制器应用模型转换器
/// </summary>
internal sealed class DynamicApiControllerApplicationModelConvention : IApplicationModelConvention
{
    /// <summary>
    /// 动态接口控制器配置实例
    /// </summary>
    private readonly DynamicApiControllerSettingsOptions _dynamicApiControllerSettings;

    /// <summary>
    /// 统一返回运行时配置。
    /// </summary>
    private readonly UnifyResultRuntimeOptions _unifyResultRuntimeOptions;

    /// <summary>
    /// 当前动态 API 约定实例使用的请求动词映射。
    /// </summary>
    private readonly IReadOnlyDictionary<string, string> _verbToHttpMethods;

    private readonly DynamicApiNameResolver _nameResolver;
    private readonly DynamicApiHttpMethodResolver _httpMethodResolver;
    private readonly DynamicApiParameterBinder _parameterBinder;
    private readonly DynamicApiRouteBuilder _routeBuilder;
    private readonly DynamicApiUnifyMetadataContributor _unifyMetadataContributor;
    private readonly DynamicApiProbeMetadataContributor _apiProbeMetadataContributor;

    /// <summary>
    /// 构造函数
    /// </summary>
    public DynamicApiControllerApplicationModelConvention(
        IOptions<DynamicApiControllerSettingsOptions> dynamicApiControllerSettings,
        IOptions<UnifyResultRuntimeOptions> unifyResultRuntimeOptions,
        DynamicApiVerbMap verbMap,
        DynamicApiNameResolver nameResolver,
        DynamicApiHttpMethodResolver httpMethodResolver,
        DynamicApiParameterBinder parameterBinder,
        DynamicApiRouteBuilder routeBuilder,
        DynamicApiUnifyMetadataContributor unifyMetadataContributor,
        DynamicApiProbeMetadataContributor apiProbeMetadataContributor)
        : this(
            dynamicApiControllerSettings.Value,
            unifyResultRuntimeOptions.Value,
            verbMap,
            nameResolver,
            httpMethodResolver,
            parameterBinder,
            routeBuilder,
            unifyMetadataContributor,
            apiProbeMetadataContributor)
    {
    }

    internal DynamicApiControllerApplicationModelConvention(
        DynamicApiControllerSettingsOptions dynamicApiControllerSettings,
        UnifyResultRuntimeOptions unifyResultRuntimeOptions)
        : this(
            dynamicApiControllerSettings,
            unifyResultRuntimeOptions,
            new DynamicApiVerbMap(dynamicApiControllerSettings),
            null,
            null,
            new DynamicApiParameterBinder(dynamicApiControllerSettings),
            new DynamicApiRouteBuilder(dynamicApiControllerSettings),
            new DynamicApiUnifyMetadataContributor(unifyResultRuntimeOptions),
            new DynamicApiProbeMetadataContributor())
    {
    }

    private DynamicApiControllerApplicationModelConvention(
        DynamicApiControllerSettingsOptions dynamicApiControllerSettings,
        UnifyResultRuntimeOptions unifyResultRuntimeOptions,
        DynamicApiVerbMap verbMap,
        DynamicApiNameResolver nameResolver,
        DynamicApiHttpMethodResolver httpMethodResolver,
        DynamicApiParameterBinder parameterBinder,
        DynamicApiRouteBuilder routeBuilder,
        DynamicApiUnifyMetadataContributor unifyMetadataContributor,
        DynamicApiProbeMetadataContributor apiProbeMetadataContributor)
    {
        _dynamicApiControllerSettings = dynamicApiControllerSettings;
        _unifyResultRuntimeOptions = unifyResultRuntimeOptions;
        _verbToHttpMethods = verbMap.VerbToHttpMethods;
        _nameResolver = nameResolver ?? new DynamicApiNameResolver(dynamicApiControllerSettings, verbMap);
        _httpMethodResolver = httpMethodResolver ?? new DynamicApiHttpMethodResolver(dynamicApiControllerSettings, verbMap);
        _parameterBinder = parameterBinder;
        _routeBuilder = routeBuilder;
        _unifyMetadataContributor = unifyMetadataContributor;
        _apiProbeMetadataContributor = apiProbeMetadataContributor;
    }

    /// <summary>
    /// 配置应用模型信息
    /// </summary>
    /// <param name="application">引用模型</param>
    public void Apply(ApplicationModel application)
    {
        var controllers = application.Controllers.Where(u => Penetrates.IsApiController(u.ControllerType));
        foreach (var controller in controllers)
        {
            var controllerType = controller.ControllerType;

            // 判断是否处理 Mvc控制器
            if (typeof(ControllerBase).IsAssignableFrom(controllerType))
            {
                if (!_dynamicApiControllerSettings.SupportedMvcController.Value || controller.ApiExplorer?.IsVisible == false)
                {
                    // 控制器默认处理规范化结果
                    if (_unifyResultRuntimeOptions.Enabled)
                    {
                        foreach (var action in controller.Actions)
                        {
                            // 配置动作方法规范化特性
                            _unifyMetadataContributor.Configure(action);
                        }
                    }

                    continue;
                }
            }

            var controllerApiDescriptionSettings = controllerType.IsDefined(typeof(ApiDescriptionSettingsAttribute), true) ? controllerType.GetCustomAttribute<ApiDescriptionSettingsAttribute>(true) : default;
            ConfigureController(controller, controllerApiDescriptionSettings);
        }
    }

    /// <summary>
    /// 配置控制器
    /// </summary>
    /// <param name="controller">控制器模型</param>
    /// <param name="controllerApiDescriptionSettings">接口描述配置</param>
    private void ConfigureController(ControllerModel controller, ApiDescriptionSettingsAttribute controllerApiDescriptionSettings)
    {
        // 配置区域
        ConfigureControllerArea(controller, controllerApiDescriptionSettings);

        // 配置控制器名称
        _nameResolver.ConfigureControllerName(controller, controllerApiDescriptionSettings);

        // 配置控制器路由特性
        _routeBuilder.ConfigureControllerRoute(controller, controllerApiDescriptionSettings);

        var actions = controller.Actions;

        // 查找所有重复的方法签名
        var repeats = actions.GroupBy(u => new { u.ActionMethod.ReflectedType.Name, Signature = u.ActionMethod.ToString() })
                             .Where(u => u.Count() > 1)
                             .SelectMany(u => u.Where(u => u.ActionMethod.ReflectedType.Name != u.ActionMethod.DeclaringType.Name));

        // 2021年04月01日 https://docs.microsoft.com/en-US/aspnet/core/web-api/?view=aspnetcore-5.0#binding-source-parameter-inference
        // 判断是否贴有 [ApiController] 特性
        var hasApiControllerAttribute = controller.Attributes.Any(u => u.GetType() == typeof(ApiControllerAttribute));

        foreach (var action in actions)
        {
            // 跳过相同方法签名
            if (repeats.Contains(action))
            {
                action.ApiExplorer.IsVisible = false;
                continue;
            };

            var actionMethod = action.ActionMethod;
            var actionApiDescriptionSettings = actionMethod.IsDefined(typeof(ApiDescriptionSettingsAttribute), true) ? actionMethod.GetCustomAttribute<ApiDescriptionSettingsAttribute>(true) : default;
            ConfigureAction(action, actionApiDescriptionSettings, controllerApiDescriptionSettings, hasApiControllerAttribute);
        }
    }

    /// <summary>
    /// 配置控制器区域
    /// </summary>
    /// <param name="controller"></param>
    /// <param name="controllerApiDescriptionSettings"></param>
    private void ConfigureControllerArea(ControllerModel controller, ApiDescriptionSettingsAttribute controllerApiDescriptionSettings)
    {
        // 如果配置了区域，则跳过
        if (controller.RouteValues.ContainsKey("area")) return;

        // 如果没有配置区域，则跳过
        var area = controllerApiDescriptionSettings?.Area ?? _dynamicApiControllerSettings.DefaultArea;
        if (string.IsNullOrWhiteSpace(area)) return;

        controller.RouteValues["area"] = area;
    }

    /// <summary>
    /// 配置动作方法
    /// </summary>
    /// <param name="action">控制器模型</param>
    /// <param name="apiDescriptionSettings">接口描述配置</param>
    /// <param name="controllerApiDescriptionSettings">控制器接口描述配置</param>
    /// <param name="hasApiControllerAttribute">是否贴有 ApiController 特性</param>
    private void ConfigureAction(ActionModel action, ApiDescriptionSettingsAttribute apiDescriptionSettings, ApiDescriptionSettingsAttribute controllerApiDescriptionSettings, bool hasApiControllerAttribute)
    {
        // 配置动作方法接口可见性
        ConfigureActionApiExplorer(action);

        // 配置动作方法名称
        var nameResult = _nameResolver.ConfigureActionName(action, apiDescriptionSettings, controllerApiDescriptionSettings);

        // 配置动作方法请求谓词特性
        _httpMethodResolver.Configure(action);

        // 配置引用类型参数
        _parameterBinder.ConfigureClassTypeParameter(action);

        // 配置动作方法路由特性
        _routeBuilder.ConfigureActionRoute(action, apiDescriptionSettings, controllerApiDescriptionSettings, nameResult, hasApiControllerAttribute);

        // 配置 APIProbe 标准元数据
        _apiProbeMetadataContributor.Configure(action, apiDescriptionSettings, controllerApiDescriptionSettings);

        // 配置动作方法规范化特性
        if (_unifyResultRuntimeOptions.Enabled) _unifyMetadataContributor.Configure(action);
    }

    /// <summary>
    /// 配置动作方法接口可见性
    /// </summary>
    /// <param name="action">动作方法模型</param>
    private static void ConfigureActionApiExplorer(ActionModel action)
    {
        if (!action.ApiExplorer.IsVisible.HasValue) action.ApiExplorer.IsVisible = true;
    }
}
