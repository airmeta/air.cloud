﻿/*
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
using Air.Cloud.Core.Standard.Taxin.Model;

namespace Air.Cloud.Core.Standard.Taxin.Client
{
    /// <summary>
    /// <para>zh-cn:Taxin客户端标准</para>
    /// <para>en-us:Taxin client standard</para>
    /// </summary>
    public interface ITaxinClientStandard: ITaxinStandard
    {
        /// <summary>
        /// <para>zh-cn:推送数据包</para>
        /// <para>en-us:Push data package</para>
        /// </summary>
        public Task PushAsync();

        /// <summary>
        /// <para>zh-cn:拉取数据包</para>
        /// <para>en-us:Pull data package</para>
        /// </summary>
        /// <returns>
        /// <para>zh-cn:数据包</para>
        /// <para>en-us:Data package</para>
        /// </returns>
        public Task PullAsync();

        /// <summary>
        /// <para>zh-cn:远程检查是否最新</para>
        /// <para>en-us:Remotely check if it's up to date</para>
        /// </summary>
        /// <returns></returns>
        public Task CheckAsync();

        /// <summary>
        /// <para>zh-cn:发起请求</para>
        /// <para>en-us:Send request</para>
        /// </summary>
        /// <typeparam name="TResult">
        /// <para>zh-cn:请求结果类型</para>
        /// <para>en-us:Result type</para>
        /// </typeparam>
        /// <param name="Route">
        /// <para>zh-cn:路由标识</para>
        /// <para>en-us:Route key</para>
        /// </param>
        /// <param name="Version">
        /// <para>zh-cn:版本号(包含起始版本号和最终版本号),为空则选择可用版本中第一个(注意: 这里的可用版本并不是按照版本顺序进行排序的)</para>
        /// <para>en-us:Version number (including starting version number and final version number), if empty, select the first available version (note: the available versions here are not sorted according to version order)</para>
        /// </param>
        /// <param name="Data">
        /// <para>zh-cn:请求数据</para>
        /// <para>en-us:Request Data</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:远程调用接口返回结果</para>
        /// <para>en-us:Remote call interface returns results</para>
        /// </returns>
        public Task<TResult> SendAsync<TResult>(string Route, object Data = null, Tuple<Version, Version> Version = null) where TResult : class;
    }
}
