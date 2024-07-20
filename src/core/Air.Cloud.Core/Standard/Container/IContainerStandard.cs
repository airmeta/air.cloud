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
using Air.Cloud.Core.Standard.Container.Model;

using System.Collections.Concurrent;

namespace Air.Cloud.Core.Standard.Container
{
    /// <summary>
    /// <para>zh-cn:容器约定</para>
    /// <para>en-us:Container standard</para>
    /// </summary>
    /// <remarks>
    /// <para>zh-cn:当前容器标准截至24年只包含查询,启动与停止三个功能 后续功能将在后续版本中上线,如果你有相关需求,可以接续相关开发</para>
    /// <para>en-us:As of 2024, the current container standard only includes three functions: query, start and stop. Subsequent functions will be launched in subsequent versions. If you have relevant needs, you can continue related development. We welcome you to join</para>
    /// </remarks>
    public interface IContainerStandard 
    {
        /// <summary>
        /// 查询容器
        /// </summary>
        /// <returns></returns>
        public Task<ConcurrentBag<TContainerInstance>> QueryAsync<TContainerInstance>() where TContainerInstance : IContainerInstance, new();

        /// <summary>
        /// 启动容器
        /// </summary>
        /// <returns></returns>
        public Task<TContainerInstance> StartAsync<TContainerInstance>(TContainerInstance Information) where TContainerInstance : IContainerInstance, new();

        /// <summary>
        /// 停止容器
        /// </summary>
        /// <param name="Information"></param>
        /// <returns></returns>
        public Task<TContainerInstance> StopAsync<TContainerInstance>(TContainerInstance Information) where TContainerInstance : IContainerInstance, new();

    }
}
