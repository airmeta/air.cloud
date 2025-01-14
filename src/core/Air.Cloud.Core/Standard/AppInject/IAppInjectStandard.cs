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
namespace Air.Cloud.Core.Standard.AppInject
{
    /// <summary>
    /// <para>zh-cn: 应用注入标准</para>
    /// <para> en-us: Application injection standard</para>
    /// </summary>
    public interface IAppInjectStandard:IStandard
    {
        /// <summary>
        /// zh-cn: 注入
        /// en-us: Configuration
        /// </summary>
        /// <typeparam name="T">
        /// <para>zh-cn: 注入后返回参数类型</para>
        /// <para>en-us: Parameter type returned after injection</para>
        /// </typeparam>
        /// <returns>
        /// <para>zh-cn: 注入后返回参数</para>
        /// <para>en-us: Parameter returned after injection </para>
        /// </returns>
        T Inject<T>(T t,params object[] parameter) where T : class;
    }
}
