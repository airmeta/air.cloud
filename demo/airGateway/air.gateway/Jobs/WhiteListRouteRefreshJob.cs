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
using air.gateway.Modules.WhiteRouteLoader;

using Quartz;

namespace air.gateway.Jobs;

/// <summary>
/// <para>zh-cn:定期刷新网关白名单路由的 Quartz 作业。作业只更新宿主进程内快照，白名单插件在请求链路中读取该快照，不会把 Consul I/O 放到每个请求上。</para>
/// <para>en-us:Quartz job that periodically refreshes gateway whitelist routes. It only updates the host-process snapshot; whitelist plugins read that snapshot in the request path and do not perform Consul I/O for every request.</para>
/// </summary>
[DisallowConcurrentExecution]
public sealed class WhiteListRouteRefreshJob : IJob
{
    private readonly AirWhiteRouteLoader whiteRouteLoader;
    private readonly ILogger<WhiteListRouteRefreshJob> logger;

    /// <summary>
    /// <para>zh-cn:创建白名单刷新作业。</para>
    /// <para>en-us:Creates the whitelist refresh job.</para>
    /// </summary>
    /// <param name="whiteRouteLoader">
    /// <para>zh-cn:维护白名单快照并读取 Consul 的加载器。</para>
    /// <para>en-us:Loader that maintains the whitelist snapshot and reads Consul.</para>
    /// </param>
    /// <param name="logger">
    /// <para>zh-cn:用于记录刷新成功或失败的日志记录器。</para>
    /// <para>en-us:Logger used to record refresh success or failure.</para>
    /// </param>
    public WhiteListRouteRefreshJob(AirWhiteRouteLoader whiteRouteLoader, ILogger<WhiteListRouteRefreshJob> logger)
    {
        this.whiteRouteLoader = whiteRouteLoader ?? throw new ArgumentNullException(nameof(whiteRouteLoader));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// <para>zh-cn:执行一次白名单刷新。Consul 不可用或内容无效时仅记录警告，加载器会继续保留最后一次成功快照。</para>
    /// <para>en-us:Executes one whitelist refresh. When Consul is unavailable or content is invalid, it only logs a warning and the loader retains the last successful snapshot.</para>
    /// </summary>
    /// <param name="context">
    /// <para>zh-cn:Quartz 调度上下文，取消时会停止本次 Consul 读取。</para>
    /// <para>en-us:Quartz scheduling context whose cancellation stops this Consul read.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:表示刷新过程的任务。</para>
    /// <para>en-us:A task representing the refresh operation.</para>
    /// </returns>
    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            var routeCount = await whiteRouteLoader.RefreshWhiteRouteLoaderAsync(context.CancellationToken);
            logger.LogInformation("Gateway whitelist route snapshot refreshed with {RouteCount} route(s).", routeCount);
        }
        catch (Exception ex) when (!context.CancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Gateway whitelist route refresh failed; retaining the last successful snapshot.");
        }
    }
}
