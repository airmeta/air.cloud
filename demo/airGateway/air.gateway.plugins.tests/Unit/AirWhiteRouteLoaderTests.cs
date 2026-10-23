using air.gateway.Modules.WhiteRouteLoader;

namespace air.gateway.plugins.tests.Unit;

/// <summary>
/// <para>zh-cn:网关白名单路由快照刷新测试。</para>
/// <para>en-us:Tests for gateway whitelist-route snapshot refreshes.</para>
/// </summary>
public sealed class AirWhiteRouteLoaderTests
{
    /// <summary>
    /// <para>zh-cn:验证刷新会规范化路由并原子替换后续插件读取的进程级快照。</para>
    /// <para>en-us:Verifies that refresh normalizes routes and atomically replaces the process-wide snapshot read by subsequent plugins.</para>
    /// </summary>
    [Fact]
    public async Task RefreshWhiteRouteLoaderAsync_should_replace_the_shared_snapshot()
    {
        AirWhiteRouteLoader.ResetForTesting();
        var responses = new Queue<List<string>?>();
        responses.Enqueue(new List<string> { " /public ", "/PUBLIC", string.Empty });
        responses.Enqueue(new List<string> { "/private" });
        var loader = new AirWhiteRouteLoader(_ => Task.FromResult(responses.Dequeue()));

        var firstCount = await loader.RefreshWhiteRouteLoaderAsync();
        var firstRoutes = await new AirWhiteRouteLoader().GetWhiteRouteLoaderAsync();
        var secondCount = await loader.RefreshWhiteRouteLoaderAsync();
        var secondRoutes = await new AirWhiteRouteLoader().GetWhiteRouteLoaderAsync();

        Assert.Equal(1, firstCount);
        Assert.Equal(new[] { "/public" }, firstRoutes);
        Assert.Equal(1, secondCount);
        Assert.Equal(new[] { "/private" }, secondRoutes);
    }

    /// <summary>
    /// <para>zh-cn:验证刷新读取失败时不会覆盖最后一次成功白名单，避免 Consul 短暂故障改变已有访问决策。</para>
    /// <para>en-us:Verifies that a failed refresh does not overwrite the last successful whitelist, preventing a transient Consul outage from changing established access decisions.</para>
    /// </summary>
    [Fact]
    public async Task RefreshWhiteRouteLoaderAsync_should_retain_the_last_snapshot_when_reading_fails()
    {
        AirWhiteRouteLoader.ResetForTesting();
        var invocationCount = 0;
        var loader = new AirWhiteRouteLoader(_ =>
        {
            invocationCount++;
            return invocationCount == 1
                ? Task.FromResult<List<string>?>(new List<string> { "/public" })
                : Task.FromException<List<string>?>(new InvalidOperationException("Consul unavailable"));
        });

        await loader.RefreshWhiteRouteLoaderAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => loader.RefreshWhiteRouteLoaderAsync());
        var routes = await new AirWhiteRouteLoader().GetWhiteRouteLoaderAsync();

        Assert.Equal(new[] { "/public" }, routes);
    }
}
