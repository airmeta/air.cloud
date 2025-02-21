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
using Air.Cloud.Core.App.Options;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

using System.Reflection;

namespace Air.Cloud.Core.Extensions
{
    /// <summary>
    /// 应用中间件拓展类
    /// </summary>
    [IgnoreScanning]
    public static class AppApplicationBuilderExtensions
    {
        /// <summary>
        /// 注入基础中间件
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseAppInject(this IApplicationBuilder app)
        {
            return app;
        }
        /// <summary>
        /// 添加应用中间件
        /// </summary>
        /// <param name="app">应用构建器</param>
        /// <param name="configure">应用配置</param>
        /// <returns>应用构建器</returns>
        internal static IApplicationBuilder UseApp(this IApplicationBuilder app, Action<IApplicationBuilder> configure = null)
        {
            // 调用自定义服务
            configure?.Invoke(app);
            return app;
        }

        /// <summary>
        /// 添加选项配置
        /// </summary>
        /// <typeparam name="TOptions">选项类型</typeparam>
        /// <param name="services">服务集合</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddConfigurableOptions<TOptions>(this IServiceCollection services)
            where TOptions : class, IConfigurableOptions
        {
            var optionsType = typeof(TOptions);
            // 获取配置路径
            var ConfigurableOptions = optionsType.GetCustomAttribute<ConfigurationInfoAttribute>();
            var optionsConfiguration = AppConfiguration.Configuration.GetSection(ConfigurableOptions.ConfigurationName);

            // 配置选项监听
            if (typeof(IConfigurableOptionsListener<TOptions>).IsAssignableFrom(optionsType))
            {
                var onListenerMethod = optionsType.GetMethod(nameof(IConfigurableOptionsListener<TOptions>.OnListener));
                if (onListenerMethod != null)
                {
                    AppConfiguration.AddChangeReloadFunction(ConfigurableOptions.ConfigurationName, () =>
                    {
                        var options = optionsConfiguration.Get<TOptions>();
                        if (options != null) onListenerMethod.Invoke(options, new object[] { options, optionsConfiguration });
                    });
                }
            }
            AppConfiguration.StartListenChangeReloadFunction();
            var optionsConfigure = services.AddOptions<TOptions>()
                  .Bind(optionsConfiguration, options =>
                  {
                      options.BindNonPublicProperties = true; // 绑定私有变量
                  })
                  .ValidateDataAnnotations();

            // 配置复杂验证后后期配置
            var validateInterface = optionsType.GetInterfaces()
                .FirstOrDefault(u => u.IsGenericType && typeof(IConfigurableOptions).IsAssignableFrom(u.GetGenericTypeDefinition()));
            if (validateInterface != null)
            {
                var genericArguments = validateInterface.GenericTypeArguments;

                // 配置复杂验证
                if (genericArguments.Length > 1)
                {
                    services.TryAddEnumerable(ServiceDescriptor.Singleton(typeof(IValidateOptions<TOptions>), genericArguments.Last()));
                }
                var postConfigureMethod = optionsType.GetMethod(nameof(IConfigurableOptions<TOptions>.PostConfigure));
                if (postConfigureMethod != null)
                {
                    optionsConfigure.PostConfigure(options => postConfigureMethod.Invoke(options, new object[] { options, optionsConfiguration }));
                }
            }

            return services;
        }
    }
}
