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

namespace Air.Cloud.EntityFrameWork.Core.Contexts.Attributes;

/// <summary>
/// <para>zh-cn:声明数据库上下文定位器使用的 Provider 标识。该标识只参与配置器解析，不包含连接字符串。</para>
/// <para>en-us:Declares the Provider key used by a database-context locator. The key participates only in configurator resolution and contains no connection string.</para>
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class DatabaseProviderAttribute : Attribute
{
    /// <summary>
    /// <para>zh-cn:创建 Provider 标识特性。</para>
    /// <para>en-us:Creates a Provider key attribute.</para>
    /// </summary>
    /// <param name="providerKey"><para>zh-cn:稳定的 Provider 标识。</para><para>en-us:The stable Provider key.</para></param>
    public DatabaseProviderAttribute(string providerKey)
    {
        if (string.IsNullOrWhiteSpace(providerKey)) throw new ArgumentException("Provider key cannot be empty.", nameof(providerKey));
        ProviderKey = providerKey.Trim();
    }

    /// <summary>
    /// <para>zh-cn:创建由 Provider 类型标识的 Provider 特性。</para>
    /// <para>en-us:Creates a Provider attribute identified by a Provider type.</para>
    /// </summary>
    /// <param name="providerType"><para>zh-cn:实现 IDatabaseProvider 的 Provider 类型。</para><para>en-us:The Provider type implementing IDatabaseProvider.</para></param>
    public DatabaseProviderAttribute(Type providerType)
    {
        ArgumentNullException.ThrowIfNull(providerType);
        if (!typeof(IDatabaseProvider).IsAssignableFrom(providerType))
            throw new ArgumentException($"{providerType.FullName} is not an IDatabaseProvider.", nameof(providerType));

        ProviderType = providerType;
    }

    /// <summary>
    /// <para>zh-cn:Provider 标识。</para>
    /// <para>en-us:The Provider key.</para>
    /// </summary>
    public string? ProviderKey { get; }

    /// <summary>
    /// <para>zh-cn:Provider 身份类型；新用法优先使用该属性。</para>
    /// <para>en-us:The Provider identity type; preferred by the new registration model.</para>
    /// </summary>
    public Type? ProviderType { get; }
}
