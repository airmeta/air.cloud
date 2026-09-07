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
/// <para>zh-cn:根据数据库上下文和定位器解析对应 Provider 配置器。</para>
/// <para>en-us:Resolves the Provider configurator for a database context and locator.</para>
/// </summary>
public interface IDatabaseConfigureResolver
{
    /// <summary>
    /// <para>zh-cn:解析数据库配置器。多个 Provider 共存时必须通过 Locator 的 Provider 标识唯一确定实现。</para>
    /// <para>en-us:Resolves a database configurator. When multiple Providers coexist, the locator Provider key must identify one implementation uniquely.</para>
    /// </summary>
    /// <param name="dbContextType"><para>zh-cn:数据库上下文类型。</para><para>en-us:The database context type.</para></param>
    /// <param name="dbContextLocatorType"><para>zh-cn:数据库上下文定位器类型。</para><para>en-us:The database context locator type.</para></param>
    /// <returns><para>zh-cn:匹配的数据库配置器。</para><para>en-us:The matching database configurator.</para></returns>
    /// <exception cref="InvalidOperationException"><para>zh-cn:没有匹配实现或存在多个候选实现时抛出。</para><para>en-us:Thrown when no matching implementation exists or multiple candidates remain.</para></exception>
    IDatabaseConfigure Resolve(Type dbContextType, Type dbContextLocatorType);
}

/// <summary>
/// <para>zh-cn:数据库 Provider 配置器注册描述。</para>
/// <para>en-us:Registration metadata for a database Provider configurator.</para>
/// </summary>
public interface IDatabaseConfigureDescriptor
{
    /// <summary>
    /// <para>zh-cn:稳定的 Provider 标识。</para>
    /// <para>en-us:The stable Provider key.</para>
    /// </summary>
    string ProviderKey { get; }

    /// <summary>
    /// <para>zh-cn:Provider 身份类型；新注册方式通过该类型匹配配置器。</para>
    /// <para>en-us:The Provider identity type used by the type-based configurator registration.</para>
    /// </summary>
    Type? ProviderType { get; }

    /// <summary>
    /// <para>zh-cn:配置器实现类型。</para>
    /// <para>en-us:The configurator implementation type.</para>
    /// </summary>
    Type ConfigureType { get; }
}
