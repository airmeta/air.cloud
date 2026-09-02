using Air.Cloud.DynamicApp.Contracts.Identifiers;
using Air.Cloud.DynamicApp.Contracts.Runtime;
using Air.Cloud.DynamicApp.Host;

namespace Air.Cloud.DynamicApp.UnitTest.Host;

/// <summary>
/// <para>zh-cn:DynamicApp 进程日志广播器单元测试，验证异步订阅可以接收实时日志。</para>
/// <para>en-us:Unit tests for the DynamicApp process-log broadcaster, verifying asynchronous subscriptions receive live entries.</para>
/// </summary>
public sealed class DynamicAppProcessLogBroadcasterTests
{
    /// <summary>
    /// <para>zh-cn:验证发布日志后当前订阅者可以收到同一条日志。</para>
    /// <para>en-us:Verifies a current subscriber receives the same entry after it is published.</para>
    /// </summary>
    [Fact]
    public async Task Subscribe_should_receive_published_entry()
    {
        var broadcaster = new DynamicAppProcessLogBroadcaster();
        var key = new DynamicModVersionKey(
            new DynamicAppId("security-platform"),
            new DynamicModId("auth"),
            new DynamicAppVersion("local"));
        var entry = new DynamicAppProcessLogEntry
        {
            Key = key,
            Stream = "stdout",
            Message = "ready"
        };

        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await using var subscription = broadcaster.Subscribe(key, cancellation.Token).GetAsyncEnumerator(cancellation.Token);
        var next = subscription.MoveNextAsync().AsTask();
        await Task.Delay(20, cancellation.Token);

        broadcaster.Publish(entry);

        Assert.True(await next);
        Assert.Same(entry, subscription.Current);
    }

    /// <summary>
    /// <para>zh-cn:验证同一包版本的不同部署实例只会收到各自的实时日志。</para>
    /// <para>en-us:Verifies that different deployment instances of one package version receive only their own live logs.</para>
    /// </summary>
    [Fact]
    public async Task Subscribe_should_isolate_deployment_instances()
    {
        var broadcaster = new DynamicAppProcessLogBroadcaster();
        var packageKey = new DynamicModVersionKey(
            new DynamicAppId("security-platform"),
            new DynamicModId("auth"),
            new DynamicAppVersion("1.0.0"));
        var blueKey = new DynamicAppRuntimeKey(packageKey, "blue");
        var grayKey = new DynamicAppRuntimeKey(packageKey, "gray");

        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await using var subscription = broadcaster.Subscribe(blueKey, cancellation.Token).GetAsyncEnumerator(cancellation.Token);
        var next = subscription.MoveNextAsync().AsTask();
        await Task.Delay(20, cancellation.Token);

        broadcaster.Publish(new DynamicAppProcessLogEntry
        {
            Key = packageKey,
            DeploymentId = "gray",
            Stream = "stdout",
            Message = "gray"
        });
        await Task.Delay(20, cancellation.Token);
        Assert.False(next.IsCompleted);

        broadcaster.Publish(new DynamicAppProcessLogEntry
        {
            Key = packageKey,
            DeploymentId = "blue",
            Stream = "stdout",
            Message = "blue"
        });

        Assert.True(await next);
        Assert.Equal("blue", subscription.Current.Message);
        Assert.Equal("blue", subscription.Current.DeploymentId);
    }
}
