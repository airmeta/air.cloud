using Air.Cloud.DynamicApp.Contracts.Identifiers;
using Air.Cloud.DynamicApp.Contracts.Runtime;
using Air.Cloud.DynamicApp.Host;

namespace Air.Cloud.DynamicApp.UnitTest.Host;

/// <summary>
/// <para>zh-cn:DynamicApp 运行实例操作协调器测试。</para>
/// <para>en-us:Tests for the DynamicApp runtime operation coordinator.</para>
/// </summary>
public sealed class DynamicAppRuntimeOperationCoordinatorTests
{
    /// <summary>
    /// <para>zh-cn:验证同一个运行实例的并发操作会串行执行，并且释放后后续操作可以继续获取锁。</para>
    /// <para>en-us:Verifies that concurrent operations for one runtime instance execute serially and that a later operation acquires the lock after release.</para>
    /// </summary>
    [Fact]
    public async Task AcquireAsync_should_serialize_operations_for_same_runtime_key()
    {
        var coordinator = new DynamicAppRuntimeOperationCoordinator();
        var key = new DynamicAppRuntimeKey(
            new DynamicModVersionKey(
                new DynamicAppId("security-platform"),
                new DynamicModId("auth"),
                new DynamicAppVersion("1.0.0")),
            "blue");
        await using var first = await coordinator.AcquireAsync(key);
        var secondAcquired = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var second = Task.Run(async () =>
        {
            await using var handle = await coordinator.AcquireAsync(key);
            secondAcquired.SetResult(true);
        });

        await Task.Delay(100);
        Assert.False(secondAcquired.Task.IsCompleted);

        await first.DisposeAsync();
        await second;
        Assert.True(secondAcquired.Task.IsCompletedSuccessfully);
    }

    /// <summary>
    /// <para>zh-cn:验证同一 DynamicApp/Mod 不同版本的家族操作仍然串行，避免激活和归档交叉覆盖状态。</para>
    /// <para>en-us:Verifies that family operations for different versions of one DynamicApp/Mod remain serialized so activation and archiving cannot overwrite each other.</para>
    /// </summary>
    [Fact]
    public async Task AcquirePackageAsync_should_serialize_different_versions_in_one_family()
    {
        var coordinator = new DynamicAppRuntimeOperationCoordinator();
        var firstKey = new DynamicModVersionKey(new DynamicAppId("security-platform"), new DynamicModId("auth"), new DynamicAppVersion("1.0.0"));
        var secondKey = new DynamicModVersionKey(firstKey.DynamicAppId, firstKey.ModId, new DynamicAppVersion("2.0.0"));
        await using var first = await coordinator.AcquirePackageAsync(firstKey);
        var secondAcquired = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var second = Task.Run(async () =>
        {
            await using var handle = await coordinator.AcquirePackageAsync(secondKey);
            secondAcquired.SetResult(true);
        });

        await Task.Delay(100);
        Assert.False(secondAcquired.Task.IsCompleted);

        await first.DisposeAsync();
        await second;
        Assert.True(secondAcquired.Task.IsCompletedSuccessfully);
    }
}
