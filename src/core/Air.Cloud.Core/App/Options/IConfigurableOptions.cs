/*
 * Copyright (c) 2024 星曳数据
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 *
 * This file is provided under the Mozilla Public License Version 2.0,
 * and the "NO WARRANTY" clause of the MPL is hereby expressly
 * acknowledged.
 */

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Air.Cloud.Core.App.Options;

/// <summary>
/// <para>zh-cn:应用选项依赖接口</para>
/// <para>en-us:Application option dependency interface</para>
/// </summary>
public partial interface IConfigurableOptions
{ }

/// <summary>
/// <para>zh-cn:应用选项依赖接口</para>
/// <para>en-us:Application option dependency interface</para>
/// </summary>
/// <typeparam name="TOptions">
/// <para>zh-cn:选项类型</para>
/// <para>en-us:Option type</para>
/// </typeparam>
public partial interface IConfigurableOptions<TOptions> : IConfigurableOptions
    where TOptions : class, IConfigurableOptions
{
    /// <summary>
    /// 选项后期配置
    /// </summary>
    /// <param name="options"></param>
    /// <param name="configuration"></param>
    void PostConfigure(TOptions options, IConfiguration configuration);
}

/// <summary>
/// <para>zh-cn:应用选项依赖接口</para>
/// <para>en-us:Application option dependency interface</para>
/// </summary>
/// <typeparam name="TOptions">
/// <para>zh-cn:选项类型</para>
/// <para>en-us:Option type</para>
/// </typeparam>
/// <typeparam name="TOptionsValidation">
/// <para>zh-cn:选项验证类型</para>
/// <para>en-us:Option validation type</para>
/// </typeparam>
public partial interface IConfigurableOptions<TOptions, TOptionsValidation> : IConfigurableOptions<TOptions>
    where TOptions : class, IConfigurableOptions
    where TOptionsValidation : class, IValidateOptions<TOptions>
{
}

/// <summary>
/// <para>zh-cn:应用选项依赖接口</para>
/// <para>en-us:Application option dependency interface</para>
/// </summary>
/// <typeparam name="TOptions">
/// <para>zh-cn:选项类型</para>
/// <para>en-us:Option type</para>
/// </typeparam>
public partial interface IConfigurableOptionsListener<TOptions> : IConfigurableOptions<TOptions>
    where TOptions : class, IConfigurableOptions
{
    /// <summary>
    /// <para>zh-cn:监听选项</para>
    /// <para>en-us:Listen to options</para>
    /// </summary>
    /// <param name="options">
    /// <para>zh-cn:选项</para>
    /// <para>en-us:Options</para>
    /// </param>
    /// <param name="configuration">
    /// <para>zh-cn:配置信息</para>
    /// <para>en-us:Configuration information</para>
    /// </param>
    void OnListener(TOptions options, IConfiguration configuration);
}