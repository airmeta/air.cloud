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
using Air.Cloud.Core;
using Air.Cloud.Core.Standard.Container;
using Air.Cloud.Core.Standard.Container.Model;
using Air.Cloud.Modules.IIS.Helper;

using Microsoft.Web.Administration;

namespace Air.Cloud.Modules.IIS.Dependencies
{
    /// <summary>
    /// <para>zh-cn: 基于IIS的容器标准实现</para>
    /// <para>en-us: Windows IIS container standard dependency</para>
    /// </summary>
    public class IISContainerDependency : IContainerEngineStandard
    {
        /// <summary>
        /// <para>zh-cn:查询当前 IIS 中的站点实例，并转换为指定的容器实例类型。</para>
        /// <para>en-us:Queries current IIS site instances and converts them to the specified container instance type.</para>
        /// </summary>
        /// <param name="includeStopped">
        /// <para>zh-cn:是否包含已停止站点；IIS 实现当前始终返回全部站点。</para>
        /// <para>en-us:Whether stopped sites are included; the IIS implementation currently always returns all sites.</para>
        /// </param>
        /// <param name="cancellationToken">
        /// <para>zh-cn:取消令牌。</para>
        /// <para>en-us:Cancellation token.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:包含 IIS 站点实例的集合。</para>
        /// <para>en-us:A collection that contains IIS site instances.</para>
        /// </returns>
        public Task<IReadOnlyCollection<ContainerEngineInstance>> ListAsync(bool includeStopped = true, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<ContainerEngineInstance>>(IISContainerHelper.ReadIISInstance()?.ToArray() ?? Array.Empty<ContainerEngineInstance>());
        }

        /// <inheritdoc />
        public Task<ContainerEngineInstance> InspectAsync(string containerNameOrId, CancellationToken cancellationToken = default)
        {
            using var serverManager = new ServerManager();
            var site = ResolveSite(serverManager, containerNameOrId);
            return Task.FromResult(IISContainerHelper.ToContainerEngineInstance(site));
        }

        /// <summary>
        /// <para>zh-cn:启动指定 IIS 容器实例对应的站点和应用程序池。</para>
        /// <para>en-us:Starts the site and application pool that correspond to the specified IIS container instance.</para>
        /// </summary>
        /// <param name="containerNameOrId">
        /// <para>zh-cn:需要启动的 IIS 站点名称或 ID。</para>
        /// <para>en-us:The IIS site name or id to start.</para>
        /// </param>
        /// <param name="cancellationToken">
        /// <para>zh-cn:取消令牌。</para>
        /// <para>en-us:Cancellation token.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:启动结果。</para>
        /// <para>en-us>The start result.</para>
        /// </returns>
        public Task<ContainerEngineOperationResult> StartAsync(string containerNameOrId, CancellationToken cancellationToken = default)
        {
            using var serverManager = new ServerManager();
            var site = ResolveSite(serverManager, containerNameOrId);
            var pool = serverManager.ApplicationPools[site.Applications.FirstOrDefault()?.ApplicationPoolName ?? site.Name] ?? serverManager.ApplicationPools[site.Name];
            if (pool != null && pool.State != ObjectState.Started)
            {
                pool.Start();
            }

            if (site.State != ObjectState.Started)
            {
                site.Start();
            }

            serverManager.CommitChanges();
            return Task.FromResult(ContainerEngineOperationResult.Success($"IIS site `{site.Name}` started."));
        }

        /// <summary>
        /// <para>zh-cn:停止指定 IIS 容器实例对应的站点和应用程序池。</para>
        /// <para>en-us:Stops the site and application pool that correspond to the specified IIS container instance.</para>
        /// </summary>
        /// <param name="containerNameOrId">
        /// <para>zh-cn:需要停止的 IIS 站点名称或 ID。</para>
        /// <para>en-us>The IIS site name or id to stop.</para>
        /// </param>
        /// <param name="cancellationToken">
        /// <para>zh-cn:取消令牌。</para>
        /// <para>en-us:Cancellation token.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:停止结果。</para>
        /// <para>en-us>The stop result.</para>
        /// </returns>
        public Task<ContainerEngineOperationResult> StopAsync(string containerNameOrId, CancellationToken cancellationToken = default)
        {
            using var serverManager = new ServerManager();
            var site = ResolveSite(serverManager, containerNameOrId);
            if (site.State != ObjectState.Stopped)
            {
                site.Stop();
            }

            var pool = serverManager.ApplicationPools[site.Applications.FirstOrDefault()?.ApplicationPoolName ?? site.Name] ?? serverManager.ApplicationPools[site.Name];
            if (pool != null && pool.State != ObjectState.Stopped)
            {
                pool.Stop();
            }

            serverManager.CommitChanges();
            return Task.FromResult(ContainerEngineOperationResult.Success($"IIS site `{site.Name}` stopped."));
        }

        private static Site ResolveSite(ServerManager serverManager, string containerNameOrId)
        {
            if (string.IsNullOrWhiteSpace(containerNameOrId))
            {
                throw new ArgumentException("IIS site name or id is required.", nameof(containerNameOrId));
            }

            var site = serverManager.Sites[containerNameOrId]
                ?? serverManager.Sites.FirstOrDefault(item => item.Id.ToString() == containerNameOrId);
            return site ?? throw new InvalidOperationException($"IIS site `{containerNameOrId}` was not found.");
        }
    }
}
