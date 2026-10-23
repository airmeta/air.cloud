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

using Air.Cloud.EntityFrameWork.Core.Contexts.Attributes;

namespace Air.Cloud.EntityFrameWork.Core.Configure;

/// <summary>
/// <para>zh-cn:默认数据库 Provider 配置器解析器。</para>
/// <para>en-us:Default database Provider configurator resolver.</para>
/// </summary>
[IgnoreScanning]
public sealed class DatabaseConfigureResolver : IDatabaseConfigureResolver
{
    private readonly IReadOnlyDictionary<string, IDatabaseConfigure> _configures;
    private readonly IReadOnlyDictionary<Type, IDatabaseConfigure> _configuresByProviderType;
    private readonly IReadOnlyList<IDatabaseConfigure> _fallbackConfigures;

    /// <summary>
    /// <para>zh-cn:创建解析器并校验 Provider 注册描述的一致性。</para>
    /// <para>en-us:Creates the resolver and validates Provider registration metadata.</para>
    /// </summary>
    /// <param name="configures"><para>zh-cn:已注册的数据库配置器集合。</para><para>en-us:Registered database configurators.</para></param>
    /// <param name="descriptors"><para>zh-cn:配置器注册描述集合。</para><para>en-us:Configurator registration descriptors.</para></param>
    /// <param name="providers"><para>zh-cn:已注册的 Provider 身份集合，用于校验类型注册。</para><para>en-us:Registered Provider identities used to validate type-based registrations.</para></param>
    public DatabaseConfigureResolver(
        IEnumerable<IDatabaseConfigure> configures,
        IEnumerable<IDatabaseConfigureDescriptor> descriptors,
        IEnumerable<IDatabaseProvider>? providers = null)
    {
        var configureList = configures?.ToList() ?? new List<IDatabaseConfigure>();
        var descriptorList = descriptors?.ToList() ?? new List<IDatabaseConfigureDescriptor>();
        var configureByType = configureList
            .GroupBy(u => u.GetType())
            .ToDictionary(u => u.Key, u => u.First());

        var registrations = new Dictionary<string, IDatabaseConfigure>(StringComparer.OrdinalIgnoreCase);
        var typeRegistrations = new Dictionary<Type, IDatabaseConfigure>();
        foreach (var descriptor in descriptorList)
        {
            if (!configureByType.TryGetValue(descriptor.ConfigureType, out var configure))
                throw new InvalidOperationException($"Database configure `{descriptor.ConfigureType.FullName}` is not registered.");

            if (descriptor.ProviderType != null)
            {
                if (!typeRegistrations.TryAdd(descriptor.ProviderType, configure))
                    throw new InvalidOperationException($"Database Provider type `{descriptor.ProviderType.FullName}` is registered more than once.");

                // Resolve the key from the Provider implementation for diagnostics and legacy string lookup.
                var provider = providers?.FirstOrDefault(u => u.GetType() == descriptor.ProviderType);
                var providerKey = provider?.ProviderKey;
                if (!string.IsNullOrWhiteSpace(providerKey) && !registrations.TryAdd(providerKey, configure))
                    throw new InvalidOperationException($"Database Provider key `{providerKey}` is registered more than once.");
            }
            else if (!registrations.TryAdd(descriptor.ProviderKey, configure))
            {
                throw new InvalidOperationException($"Database Provider key `{descriptor.ProviderKey}` is registered more than once.");
            }
        }

        _configures = registrations;
        _configuresByProviderType = typeRegistrations;
        _fallbackConfigures = descriptorList.Count == 0
            ? configureList
            : descriptorList
                .Select(u => configureByType[u.ConfigureType])
                .Distinct()
                .ToList();
    }

    /// <summary>
    /// <para>zh-cn:根据上下文定位器声明解析数据库配置器；未声明 Provider 时仅允许唯一候选。</para>
    /// <para>en-us:Resolves the database configurator from the context locator declaration; an undeclared Provider is allowed only when one candidate exists.</para>
    /// </summary>
    /// <param name="dbContextType"><para>zh-cn:待配置的数据库上下文类型。</para><para>en-us:The database context type to configure.</para></param>
    /// <param name="dbContextLocatorType"><para>zh-cn:用于选择 Provider 的上下文定位器类型。</para><para>en-us:The context locator type used to select the Provider.</para></param>
    /// <returns><para>zh-cn:匹配的数据库配置器。</para><para>en-us:The matching database configurator.</para></returns>
    /// <exception cref="InvalidOperationException"><para>zh-cn:Provider 未注册或多个 Provider 注册但定位器未声明 Provider 时抛出。</para><para>en-us:Thrown when the Provider is not registered or multiple Providers exist without a locator declaration.</para></exception>
    public IDatabaseConfigure Resolve(Type dbContextType, Type dbContextLocatorType)
    {
        if (dbContextType == null) throw new ArgumentNullException(nameof(dbContextType));
        if (dbContextLocatorType == null) throw new ArgumentNullException(nameof(dbContextLocatorType));

        var providerAttribute = dbContextLocatorType
            .GetCustomAttributes(typeof(DatabaseProviderAttribute), inherit: false)
            .OfType<DatabaseProviderAttribute>()
            .FirstOrDefault();

        if (providerAttribute?.ProviderType != null)
        {
            if (_configuresByProviderType.TryGetValue(providerAttribute.ProviderType, out var configure)) return configure;
            throw new InvalidOperationException($"Database Provider type `{providerAttribute.ProviderType.FullName}` for locator `{dbContextLocatorType.FullName}` is not registered.");
        }

        var providerKey = providerAttribute?.ProviderKey;
        if (!string.IsNullOrWhiteSpace(providerKey))
        {
            if (_configures.TryGetValue(providerKey, out var configure)) return configure;
            throw new InvalidOperationException($"Database Provider `{providerKey}` for locator `{dbContextLocatorType.FullName}` is not registered.");
        }

        if (_fallbackConfigures.Count == 1) return _fallbackConfigures[0];

        if (_fallbackConfigures.Count == 0)
            throw new InvalidOperationException($"No IDatabaseConfigure is registered for DbContext `{dbContextType.FullName}`.");

        throw new InvalidOperationException($"Multiple database Providers are registered, but locator `{dbContextLocatorType.FullName}` does not declare a Provider key.");
    }
}
