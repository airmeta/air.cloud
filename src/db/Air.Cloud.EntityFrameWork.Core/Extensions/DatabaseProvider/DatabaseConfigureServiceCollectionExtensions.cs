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

using Air.Cloud.EntityFrameWork.Core.Configure;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Air.Cloud.EntityFrameWork.Core.Extensions.DatabaseProvider;

/// <summary>
/// <para>zh-cn:数据库 Provider 配置器注册扩展。</para>
/// <para>en-us:Database Provider configurator registration extensions.</para>
/// </summary>
[IgnoreScanning]
public static class DatabaseConfigureServiceCollectionExtensions
{
    /// <summary>
    /// <para>zh-cn:按 Provider 类型注册数据库配置器；Provider 标识由 Provider 实现自身提供。</para>
    /// <para>en-us:Registers a database configurator by Provider type; the Provider identity is supplied by the Provider implementation.</para>
    /// </summary>
    /// <typeparam name="TConfigure"><para>zh-cn:数据库配置器类型。</para><para>en-us:The database configurator type.</para></typeparam>
    /// <typeparam name="TProvider"><para>zh-cn:数据库 Provider 身份类型。</para><para>en-us:The database Provider identity type.</para></typeparam>
    /// <param name="services"><para>zh-cn:服务集合。</para><para>en-us:The service collection.</para></param>
    /// <returns><para>zh-cn:已注册配置器的服务集合。</para><para>en-us:The service collection with the configurator registered.</para></returns>
    public static IServiceCollection AddDatabaseConfigure<TConfigure, TProvider>(this IServiceCollection services)
        where TConfigure : class, IDatabaseConfigure
        where TProvider : class, IDatabaseProvider
    {
        if (services == null) throw new ArgumentNullException(nameof(services));

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDatabaseConfigure, TConfigure>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDatabaseProvider, TProvider>());

        var descriptorExists = services.Any(u =>
            u.ServiceType == typeof(IDatabaseConfigureDescriptor)
            && u.ImplementationInstance is IDatabaseConfigureDescriptor descriptor
            && descriptor.ProviderType == typeof(TProvider)
            && descriptor.ConfigureType == typeof(TConfigure));
        if (!descriptorExists)
        {
            services.AddSingleton<IDatabaseConfigureDescriptor>(
                new DatabaseConfigureDescriptor(typeof(TProvider), typeof(TConfigure)));
        }

        services.TryAddSingleton<IDatabaseConfigureResolver, DatabaseConfigureResolver>();
        return services;
    }

    /// <summary>
    /// <para>zh-cn:注册带稳定 Provider 标识的数据库配置器，并自动加入全局解析器。</para>
    /// <para>en-us:Registers a database configurator with a stable Provider key and adds it to the global resolver.</para>
    /// </summary>
    /// <typeparam name="TConfigure"><para>zh-cn:数据库配置器类型。</para><para>en-us:The database configurator type.</para></typeparam>
    /// <param name="services"><para>zh-cn:服务集合。</para><para>en-us:The service collection.</para></param>
    /// <param name="providerKey"><para>zh-cn:稳定的 Provider 标识。</para><para>en-us:The stable Provider key.</para></param>
    /// <returns><para>zh-cn:已注册配置器的服务集合。</para><para>en-us:The service collection with the configurator registered.</para></returns>
    public static IServiceCollection AddDatabaseConfigure<TConfigure>(this IServiceCollection services, string providerKey)
        where TConfigure : class, IDatabaseConfigure
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (string.IsNullOrWhiteSpace(providerKey)) throw new ArgumentException("Provider key cannot be empty.", nameof(providerKey));
        var normalizedProviderKey = providerKey.Trim();

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDatabaseConfigure, TConfigure>());
        // Descriptor instances carry distinct ProviderKey values, so they must not be
        // registered with TryAddEnumerable, which de-duplicates the shared descriptor type.
        var descriptorExists = services.Any(u =>
            u.ServiceType == typeof(IDatabaseConfigureDescriptor)
            && u.ImplementationInstance is IDatabaseConfigureDescriptor descriptor
            && string.Equals(descriptor.ProviderKey, normalizedProviderKey, StringComparison.OrdinalIgnoreCase)
            && descriptor.ConfigureType == typeof(TConfigure));
        if (!descriptorExists)
        {
            services.AddSingleton<IDatabaseConfigureDescriptor>(
                new DatabaseConfigureDescriptor(normalizedProviderKey, typeof(TConfigure)));
        }
        services.TryAddSingleton<IDatabaseConfigureResolver, DatabaseConfigureResolver>();
        return services;
    }
}
