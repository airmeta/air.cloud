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
using Air.Cloud.Core.App;
using Air.Cloud.Core.Extensions;
using Air.Cloud.Modules.Consul.Model;

namespace air.gateway.Modules.WhiteRouteLoader
{
    /// <summary>
    /// <para>zh-cn:网关白名单路由加载器。它维护当前进程的不可变白名单快照，首次请求可按需加载，Quartz 作业会定期从 Consul 刷新该快照。刷新失败不会清空最后一次成功的结果。</para>
    /// <para>en-us:Gateway whitelist-route loader. It maintains an immutable whitelist snapshot for the current process, loads on demand for the first request, and lets a Quartz job refresh the snapshot from Consul periodically. A refresh failure never clears the last successful result.</para>
    /// </summary>
    public class AirWhiteRouteLoader
    {
        private static readonly SemaphoreSlim RefreshLock = new(1, 1);
        private static readonly string[] EmptyRoutes = Array.Empty<string>();
        private static string[] whiteListRoutes = EmptyRoutes;
        private static int hasLoadedRoutes;
        private readonly Func<CancellationToken, Task<List<string>?>>? routeReader;

        /// <summary>
        /// <para>zh-cn:创建使用生产 Consul KV 中心读取白名单的加载器。</para>
        /// <para>en-us:Creates a loader that reads whitelist routes through the production Consul KV center.</para>
        /// </summary>
        public AirWhiteRouteLoader()
        {
        }

        internal AirWhiteRouteLoader(Func<CancellationToken, Task<List<string>?>> routeReader)
        {
            this.routeReader = routeReader ?? throw new ArgumentNullException(nameof(routeReader));
        }

        /// <summary>
        /// <para>zh-cn:系统请求白名单配置地址。服务名中的点会转换为 Consul KV 路径分隔符。</para>
        /// <para>en-us:System whitelist-route configuration address. Dots in the service name are converted to Consul KV path separators.</para>
        /// </summary>
        private const string WhiteRouteConfigUrl = "{0}/default_app_whiteroute_settings.json";

        /// <summary>
        /// <para>zh-cn:获取当前白名单快照。未完成首次加载时同步触发一次 Consul 读取；已有快照时不在请求线程访问 Consul。</para>
        /// <para>en-us:Gets the current whitelist snapshot. It triggers a Consul read only before the first load; once a snapshot exists, request threads do not access Consul.</para>
        /// </summary>
        /// <returns>
        /// <para>zh-cn:当前进程的白名单快照。</para>
        /// <para>en-us:The whitelist snapshot for the current process.</para>
        /// </returns>
        public Task<List<string>> GetWhiteRouteLoaderAsync()
        {
            return GetWhiteRouteLoaderAsync(CancellationToken.None);
        }

        /// <summary>
        /// <para>zh-cn:获取当前白名单快照，并允许调用方取消首次 Consul 加载。此重载供宿主作业和新调用方使用；无参重载保留给已发布的外置插件。</para>
        /// <para>en-us:Gets the current whitelist snapshot while allowing the caller to cancel the initial Consul load. This overload is for host jobs and new callers; the parameterless overload remains for published external plugins.</para>
        /// </summary>
        /// <param name="cancellationToken">
        /// <para>zh-cn:取消白名单首次加载的令牌。</para>
        /// <para>en-us:Token that cancels the initial whitelist load.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:当前进程的白名单快照。</para>
        /// <para>en-us:The whitelist snapshot for the current process.</para>
        /// </returns>
        public async Task<List<string>> GetWhiteRouteLoaderAsync(CancellationToken cancellationToken)
        {
            if (Volatile.Read(ref hasLoadedRoutes) == 0)
            {
                try
                {
                    await RefreshWhiteRouteLoaderAsync(cancellationToken);
                }
                catch
                {
                    // Initial Consul outages must not let the plugin bypass authorization.
                }
            }

            return Volatile.Read(ref whiteListRoutes).ToList();
        }

        /// <summary>
        /// <para>zh-cn:从 Consul 读取并原子替换白名单快照。该方法供 Quartz 刷新作业调用；发生异常时保留上一次成功快照并将异常交给调用方记录。</para>
        /// <para>en-us:Reads from Consul and atomically replaces the whitelist snapshot. Quartz refresh jobs call this method; on failure it preserves the last successful snapshot and lets the caller log the exception.</para>
        /// </summary>
        /// <param name="cancellationToken">
        /// <para>zh-cn:取消 Consul 读取的令牌。</para>
        /// <para>en-us:Token that cancels the Consul read.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:刷新后白名单中的有效路由数量。</para>
        /// <para>en-us:The number of valid whitelist routes after refresh.</para>
        /// </returns>
        public async Task<int> RefreshWhiteRouteLoaderAsync(CancellationToken cancellationToken = default)
        {
            await RefreshLock.WaitAsync(cancellationToken);
            try
            {
                var sourceRoutes = await ReadRoutesAsync(cancellationToken);
                if (sourceRoutes is null)
                {
                    var options = AppConfigurationLoader.InnerConfiguration.GetConfig<ConsulServiceOptions>();
                    if (string.IsNullOrWhiteSpace(options?.ConfigurationServiceName))
                    {
                        throw new InvalidOperationException("ConsulServiceOptions:ConfigurationServiceName is required for whitelist route refresh.");
                    }

                    var url = string.Format(WhiteRouteConfigUrl, options.ConfigurationServiceName.Replace(".", "/"));
                    var kvCenter = AppRealization.KVCenter
                        ?? throw new InvalidOperationException("Gateway whitelist route refresh requires a configured KV center.");
                    await kvCenter.AddOrUpdateAsync(url, AppRealization.JSON.Serialize(EmptyRoutes));
                    Volatile.Write(ref whiteListRoutes, EmptyRoutes);
                    Volatile.Write(ref hasLoadedRoutes, 1);
                    return 0;
                }

                var routes = sourceRoutes
                    .Where(route => !string.IsNullOrWhiteSpace(route))
                    .Select(route => route.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                Volatile.Write(ref whiteListRoutes, routes);
                Volatile.Write(ref hasLoadedRoutes, 1);
                return routes.Length;
            }
            finally
            {
                RefreshLock.Release();
            }
        }

        internal static void ResetForTesting()
        {
            Volatile.Write(ref whiteListRoutes, EmptyRoutes);
            Volatile.Write(ref hasLoadedRoutes, 0);
        }

        private async Task<List<string>?> ReadRoutesAsync(CancellationToken cancellationToken)
        {
            if (routeReader is not null)
            {
                return await routeReader(cancellationToken);
            }

            var options = AppConfigurationLoader.InnerConfiguration.GetConfig<ConsulServiceOptions>();
            if (string.IsNullOrWhiteSpace(options?.ConfigurationServiceName))
            {
                throw new InvalidOperationException("ConsulServiceOptions:ConfigurationServiceName is required for whitelist route refresh.");
            }

            var url = string.Format(WhiteRouteConfigUrl, options.ConfigurationServiceName.Replace(".", "/"));
            AppRealization.Output.Print("网关白名单路由信息读取", $"读取地址: {url},键值对中心是否为空:{AppRealization.KVCenter is null}");
            var kvCenter = AppRealization.KVCenter
                ?? throw new InvalidOperationException("Gateway whitelist route refresh requires a configured KV center.");
            var result = await kvCenter.GetAsync<ConsulKvCenterServiceInformation>(url);
            var value = result?.Value;
            return string.IsNullOrEmpty(value)
                ? null
                : AppRealization.JSON.Deserialize<List<string>>(value);
        }

    }
}
