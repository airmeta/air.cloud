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

namespace Air.Cloud.EntityFrameWork.Core.Configure;

/// <summary>
/// <para>zh-cn:数据库 Provider 配置器注册描述实现。</para>
/// <para>en-us:Implementation of database Provider configurator registration metadata.</para>
/// </summary>
public sealed class DatabaseConfigureDescriptor : IDatabaseConfigureDescriptor
{
    /// <summary>
    /// <para>zh-cn:创建数据库 Provider 配置器注册描述。</para>
    /// <para>en-us:Creates database Provider configurator registration metadata.</para>
    /// </summary>
    /// <param name="providerKey"><para>zh-cn:稳定的 Provider 标识。</para><para>en-us:The stable Provider key.</para></param>
    /// <param name="configureType"><para>zh-cn:配置器实现类型。</para><para>en-us:The configurator implementation type.</para></param>
    public DatabaseConfigureDescriptor(string providerKey, Type configureType)
    {
        if (string.IsNullOrWhiteSpace(providerKey)) throw new ArgumentException("Provider key cannot be empty.", nameof(providerKey));
        if (configureType == null) throw new ArgumentNullException(nameof(configureType));
        if (!typeof(IDatabaseConfigure).IsAssignableFrom(configureType)) throw new ArgumentException($"{configureType.FullName} is not an IDatabaseConfigure.", nameof(configureType));

        ProviderKey = providerKey.Trim();
        ConfigureType = configureType;
    }

    /// <summary>
    /// <para>zh-cn:创建由 Provider 类型标识的配置器注册描述。</para>
    /// <para>en-us:Creates configurator registration metadata identified by a Provider type.</para>
    /// </summary>
    /// <param name="providerType"><para>zh-cn:实现 IDatabaseProvider 的 Provider 类型。</para><para>en-us:The Provider type implementing IDatabaseProvider.</para></param>
    /// <param name="configureType"><para>zh-cn:配置器实现类型。</para><para>en-us:The configurator implementation type.</para></param>
    public DatabaseConfigureDescriptor(Type providerType, Type configureType)
    {
        if (providerType == null) throw new ArgumentNullException(nameof(providerType));
        if (!typeof(IDatabaseProvider).IsAssignableFrom(providerType)) throw new ArgumentException($"{providerType.FullName} is not an IDatabaseProvider.", nameof(providerType));
        if (configureType == null) throw new ArgumentNullException(nameof(configureType));
        if (!typeof(IDatabaseConfigure).IsAssignableFrom(configureType)) throw new ArgumentException($"{configureType.FullName} is not an IDatabaseConfigure.", nameof(configureType));

        ProviderType = providerType;
        ProviderKey = providerType.Name;
        ConfigureType = configureType;
    }

    /// <summary><para>zh-cn:注册描述中的稳定 Provider 标识。</para><para>en-us:The stable Provider identifier in the registration descriptor.</para></summary>
    public string ProviderKey { get; }

    /// <summary><para>zh-cn:用于标识 Provider 的实现类型；按字符串注册时为 null。</para><para>en-us:The Provider implementation type; null for string-based registrations.</para></summary>
    public Type? ProviderType { get; }

    /// <summary><para>zh-cn:数据库配置器实现类型。</para><para>en-us:The database configurator implementation type.</para></summary>
    public Type ConfigureType { get; }
}
