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
using Air.Cloud.Core.Standard.Container.Model;

using System.Collections.Concurrent;

namespace Air.Cloud.Modules.Docker.Internal
{
    /// <summary>
    /// <para>zh-cn:Docker Engine 容器实例缓存，用于在事件刷新前复用最近一次查询结果。</para>
    /// <para>en-us:Docker Engine container instance cache used to reuse the latest query result before an event refresh.</para>
    /// </summary>
    /// <typeparam name="TDockerContainerInstance">
    /// <para>zh-cn:Docker 容器实例类型。</para>
    /// <para>en-us:The Docker container instance type.</para>
    /// </typeparam>
    public static class DockerEngineInternalStore<TDockerContainerInstance> where TDockerContainerInstance : IContainerInstance, new()
    {
        /// <summary>
        /// <para>zh-cn:缓存的 Docker 容器实例集合。</para>
        /// <para>en-us:The cached Docker container instance collection.</para>
        /// </summary>
        public static ConcurrentBag<TDockerContainerInstance> DockerContainerInstances = null;
    }
}
