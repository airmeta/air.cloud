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

namespace Air.Cloud.Core.Standard.Container
{
    /// <summary>
    /// 容器约定
    /// </summary>
    public interface IContainerStandard
    {
        /// <summary>
        /// 获取程序集运行信息
        /// </summary>
        /// <returns></returns>
        public IContainerRuntimeInformation GetRuntimeInformation();
        /// <summary>
        /// 泛型实现
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <returns></returns>
        public TResult GetRuntimeInformation<TResult>() where TResult : IContainerRuntimeInformation, new();
    }
}
