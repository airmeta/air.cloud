using Air.Cloud.Core;
using Air.Cloud.Core.App;
using Air.Cloud.Core.Enums;
using Air.Cloud.Core.Standard.Cache;
using Air.Cloud.Core.Standard.Cache.Redis;
using Air.Cloud.Core.Standard.DistributedLock;
using Air.Cloud.Modules.RedisCache.Dependencies;
using Air.Cloud.Modules.RedisCache.Options;

using Microsoft.Extensions.DependencyInjection;

using StackExchange.Redis;

using System.Reflection;

namespace Air.Cloud.IntegrationTest.Modules.Redis;

/// <summary>
/// <para>zh-cn:Redis 真实实例集成测试，开启后会连接配置中的 Redis 实例并执行真实读写与锁操作。</para>
/// <para>en-us:Real Redis instance integration tests. When enabled, they connect to the configured Redis instance and execute real read/write and lock operations.</para>
/// </summary>
/// <remarks>
/// <para>zh-cn:默认通过 RedisIntegration:RunRedisTests 关闭；开启后 Redis 不可用应视为集成环境失败。</para>
/// <para>en-us:Disabled by RedisIntegration:RunRedisTests by default; when enabled, unavailable Redis should be treated as integration-environment failure.</para>
/// </remarks>
public class RedisRealInstanceIntegrationTests
{
    /// <summary>
    /// <para>zh-cn:验证 Redis 模块字符串缓存标准可以连接真实 Redis 并完成写入、读取和删除。</para>
    /// <para>en-us:Verifies the Redis module string-cache standard can connect to real Redis and complete set, get, and delete.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "Redis")]
    public void Redis_should_set_get_and_remove_string_value()
    {
        if (!IsEnabled())
        {
            return;
        }

        var cache = CreateRedisCache();
        var key = BuildKey("string");
        var value = $"redis-value-{Guid.NewGuid():N}";

        try
        {
            Assert.True(cache.SetCache(key, value, TimeSpan.FromMinutes(1)));
            Assert.Equal(value, cache.GetCache(key));
        }
        finally
        {
            cache.RemoveCache(key);
        }

        Assert.Null(cache.GetCache(key));
    }

    /// <summary>
    /// <para>zh-cn:验证 Redis 模块对象缓存可以完成序列化写入和反序列化读取。</para>
    /// <para>en-us:Verifies the Redis module object cache can serialize on write and deserialize on read.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "Redis")]
    public void Redis_should_roundtrip_object_value()
    {
        if (!IsEnabled())
        {
            return;
        }

        var cache = CreateRedisCache();
        var key = BuildKey("object");
        var value = new RedisPayload
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = "redis-object"
        };

        try
        {
            Assert.True(cache.SetCache(key, value, TimeSpan.FromMinutes(1)));
            var cached = cache.GetCache<RedisPayload>(key);

            Assert.NotNull(cached);
            Assert.Equal(value.Id, cached.Id);
            Assert.Equal(value.Name, cached.Name);
        }
        finally
        {
            cache.RemoveCache(key);
        }
    }

    /// <summary>
    /// <para>zh-cn:验证 Redis 分布式锁标准可以在真实 Redis 上获取锁并执行成功回调。</para>
    /// <para>en-us:Verifies the Redis distributed-lock standard can acquire a real Redis lock and execute the success callback.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "Redis")]
    public void Redis_lock_should_execute_success_callback_once()
    {
        if (!IsEnabled())
        {
            return;
        }

        var cache = CreateRedisCache();
        using var moduleHost = BuildRedisModuleHost(cache);
        var key = BuildKey("lock");
        var executed = 0;
        Exception? failed = null;

        try
        {
            var redisLock = new RedisLockDependency();
            redisLock.TryExecuteWithLock(
                key,
                new LockAction
                {
                    Success = () => executed++,
                    Fail = exception => failed = exception
                },
                TimeSpan.FromSeconds(3),
                100,
                3000);

            Assert.Equal(1, executed);
            Assert.Null(failed);
        }
        finally
        {
            cache.RemoveCache(key);
        }
    }

    /// <summary>
    /// <para>zh-cn:验证真实 Redis 锁已被其他持有者占用时，不会执行 Success，并会触发 Fail 补偿回调。</para>
    /// <para>en-us:Verifies Success is skipped and Fail compensation is invoked when the real Redis lock is already held.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "Redis")]
    public void Redis_lock_should_call_fail_when_lock_is_not_acquired()
    {
        if (!IsEnabled())
        {
            return;
        }

        var cache = CreateRedisCache();
        using var moduleHost = BuildRedisModuleHost(cache);
        var redis = GetRedisDatabase(cache);
        var key = BuildKey("lock-busy");
        var holderToken = Guid.NewGuid().ToString("N");
        var successCount = 0;
        Exception? failure = null;

        Assert.True(redis.LockTake(key, holderToken, TimeSpan.FromSeconds(5)));
        try
        {
            var redisLock = new RedisLockDependency();
            redisLock.TryExecuteWithLock(
                key,
                new LockAction
                {
                    Success = () => successCount++,
                    Fail = exception => failure = exception
                },
                TimeSpan.FromMilliseconds(120),
                20,
                500);
        }
        finally
        {
            redis.LockRelease(key, holderToken);
            cache.RemoveCache(key);
        }

        Assert.Equal(0, successCount);
        Assert.NotNull(failure);
        Assert.Contains("Failed to acquire lock", failure.Message);
    }

    /// <summary>
    /// <para>zh-cn:验证真实 Redis 锁成功获取后，如果 Success 业务回调抛出异常，会把原始异常传递给 Fail 补偿回调。</para>
    /// <para>en-us:Verifies the original Success callback exception is passed to Fail compensation after acquiring a real Redis lock.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "Redis")]
    public void Redis_lock_should_invoke_fail_when_success_callback_throws()
    {
        if (!IsEnabled())
        {
            return;
        }

        var cache = CreateRedisCache();
        using var moduleHost = BuildRedisModuleHost(cache);
        var key = BuildKey("lock-success-throws");
        var successException = new InvalidOperationException("success-callback-error");
        Exception? failure = null;

        try
        {
            var redisLock = new RedisLockDependency();
            redisLock.TryExecuteWithLock(
                key,
                new LockAction
                {
                    Success = () => throw successException,
                    Fail = exception => failure = exception
                },
                TimeSpan.FromSeconds(3),
                100,
                3000);
        }
        finally
        {
            cache.RemoveCache(key);
        }

        Assert.Same(successException, failure);
    }

    /// <summary>
    /// <para>zh-cn:验证真实 Redis 锁等待期间 Waiting 只是通知通道；Waiting 异常会被隔离，最终仍按未获取锁触发 Fail。</para>
    /// <para>en-us:Verifies Waiting is only a notification channel while waiting for a real Redis lock; its exception is isolated and final Fail still runs.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "Redis")]
    public void Redis_lock_should_ignore_waiting_callback_exception()
    {
        if (!IsEnabled())
        {
            return;
        }

        var cache = CreateRedisCache();
        using var moduleHost = BuildRedisModuleHost(cache);
        var redis = GetRedisDatabase(cache);
        var key = BuildKey("lock-waiting-throws");
        var holderToken = Guid.NewGuid().ToString("N");
        var waitingCount = 0;
        Exception? failure = null;

        Assert.True(redis.LockTake(key, holderToken, TimeSpan.FromSeconds(5)));
        try
        {
            var redisLock = new RedisLockDependency();
            redisLock.TryExecuteWithLock(
                key,
                new LockAction
                {
                    Success = () => throw new InvalidOperationException("success-should-not-run"),
                    Waiting = () =>
                    {
                        waitingCount++;
                        throw new InvalidOperationException("waiting-callback-error");
                    },
                    Fail = exception => failure = exception
                },
                TimeSpan.FromMilliseconds(120),
                20,
                500);
        }
        finally
        {
            redis.LockRelease(key, holderToken);
            cache.RemoveCache(key);
        }

        Assert.True(waitingCount > 0);
        Assert.NotNull(failure);
        Assert.Contains("Failed to acquire lock", failure.Message);
    }

    /// <summary>
    /// <para>zh-cn:验证真实 Redis 锁执行 Fail 补偿时，如果 Fail 自身抛出异常，会包装为 AggregateException 暴露给调用方。</para>
    /// <para>en-us:Verifies a Fail callback exception during real Redis lock execution is exposed to the caller as AggregateException.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "Redis")]
    public void Redis_lock_should_wrap_fail_callback_exception()
    {
        if (!IsEnabled())
        {
            return;
        }

        var cache = CreateRedisCache();
        using var moduleHost = BuildRedisModuleHost(cache);
        var key = BuildKey("lock-fail-throws");

        try
        {
            var redisLock = new RedisLockDependency();
            var exception = Assert.Throws<AggregateException>(() =>
                redisLock.TryExecuteWithLock(
                    key,
                    new LockAction
                    {
                        Success = () => throw new InvalidOperationException("success-callback-error"),
                        Fail = _ => throw new InvalidOperationException("fail-callback-error")
                    },
                    TimeSpan.FromSeconds(3),
                    100,
                    3000));

            Assert.Contains(
                FlattenMessages(exception),
                message => message.Contains("success-callback-error"));
            Assert.Contains(
                FlattenMessages(exception),
                message => message.Contains("fail-callback-error"));
        }
        finally
        {
            cache.RemoveCache(key);
        }
    }

    /// <summary>
    /// <para>zh-cn:验证真实 Redis 异步锁成功路径只执行一次 Success 回调，并且不会触发 Fail。</para>
    /// <para>en-us:Verifies the real Redis async lock success path executes Success once and does not invoke Fail.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "Redis")]
    public async Task Redis_lock_async_should_execute_success_callback_once()
    {
        if (!IsEnabled())
        {
            return;
        }

        var cache = CreateRedisCache();
        using var moduleHost = BuildRedisModuleHost(cache);
        var key = BuildKey("lock-async");
        var successCount = 0;
        Exception? failure = null;

        try
        {
            var redisLock = new RedisLockDependency();
            await redisLock.TryExecuteWithLockAsync(
                key,
                new LockAsyncAction
                {
                    Success = () =>
                    {
                        successCount++;
                        return Task.CompletedTask;
                    },
                    Fail = exception => failure = exception
                },
                TimeSpan.FromSeconds(3),
                100,
                3000);
        }
        finally
        {
            cache.RemoveCache(key);
        }

        Assert.Equal(1, successCount);
        Assert.Null(failure);
    }

    /// <summary>
    /// <para>zh-cn:验证真实 Redis 锁成功执行后会释放锁，同一个 Key 可以立刻被新的持有者重新获取。</para>
    /// <para>en-us:Verifies a real Redis lock is released after success so the same key can be immediately acquired by a new holder.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "Redis")]
    public void Redis_lock_should_release_lock_after_success()
    {
        if (!IsEnabled())
        {
            return;
        }

        var cache = CreateRedisCache();
        using var moduleHost = BuildRedisModuleHost(cache);
        var redis = GetRedisDatabase(cache);
        var key = BuildKey("lock-release");
        var holderToken = Guid.NewGuid().ToString("N");
        var successCount = 0;
        var reacquired = false;

        try
        {
            var redisLock = new RedisLockDependency();
            redisLock.TryExecuteWithLock(
                key,
                new LockAction
                {
                    Success = () => successCount++,
                    Fail = exception => throw exception
                },
                TimeSpan.FromSeconds(3),
                100,
                3000);

            reacquired = redis.LockTake(
                key,
                holderToken,
                TimeSpan.FromSeconds(5));
        }
        finally
        {
            if (reacquired)
            {
                redis.LockRelease(key, holderToken);
            }

            cache.RemoveCache(key);
        }

        Assert.Equal(1, successCount);
        Assert.True(reacquired);
    }

    /// <summary>
    /// <para>zh-cn:验证真实 Redis 的 Hash、List、Set、SortedSet 与 Key 基础结构操作可以形成读写闭环。</para>
    /// <para>en-us:Verifies real Redis Hash, List, Set, SortedSet, and Key structure operations form a read/write loop.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "Redis")]
    public void Redis_native_structures_should_roundtrip_hash_list_set_sortedset_and_key()
    {
        if (!IsEnabled())
        {
            return;
        }

        var cache = CreateRedisCache();
        var hashKey = BuildKey("native-hash");
        var renamedHashKey = BuildKey("native-hash-renamed");
        var listKey = BuildKey("native-list");
        var setKey = BuildKey("native-set");
        var sortedSetKey = BuildKey("native-sorted-set");

        try
        {
            Assert.True(cache.Hash.Set(hashKey, "field", "hash-value"));
            Assert.True(cache.Key.Rename(hashKey, renamedHashKey));

            Assert.Equal(1, cache.List.RightPush(listKey, "list-value"));
            var setValue = new RedisPayload
            {
                Id = "set-value",
                Name = "set-name"
            };
            var sortedSetValue = new RedisPayload
            {
                Id = "sorted-value",
                Name = "sorted-name"
            };

            Assert.Equal(1, cache.Set.Add(setKey, setValue));
            Assert.True(cache.SortedSet.Add(sortedSetKey, sortedSetValue, 10));

            Assert.Equal("hash-value", cache.Hash.Get<string>(renamedHashKey, "field"));
            Assert.Equal("list-value", cache.List.RightPop<string>(listKey));
            Assert.Contains(cache.Set.Elements<RedisPayload>(setKey), item => item.Id == setValue.Id);
            Assert.Equal(sortedSetValue.Id, cache.SortedSet.RangeByRank<RedisPayload>(sortedSetKey).Single().Id);
            Assert.False(cache.Key.Exists(hashKey));
            Assert.True(cache.Key.Exists(renamedHashKey));
        }
        finally
        {
            cache.RemoveCache(hashKey);
            cache.RemoveCache(renamedHashKey);
            cache.RemoveCache(listKey);
            cache.RemoveCache(setKey);
            cache.RemoveCache(sortedSetKey);
        }
    }

    /// <summary>
    /// <para>zh-cn:验证多个真实 Redis 锁工作项竞争同一 Key 时，Success 回调不会并发重入。</para>
    /// <para>en-us:Verifies success callbacks do not re-enter concurrently when multiple workers compete for the same real Redis lock key.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "Redis")]
    public async Task Redis_lock_should_serialize_concurrent_workers()
    {
        if (!IsEnabled())
        {
            return;
        }

        var cache = CreateRedisCache();
        using var moduleHost = BuildRedisModuleHost(cache);
        var key = BuildKey("lock-concurrent");
        var activeOwners = 0;
        var maxActiveOwners = 0;
        var successCount = 0;

        try
        {
            var tasks = Enumerable.Range(0, 4)
                .Select(_ => Task.Run(() =>
                {
                    var redisLock = new RedisLockDependency();
                    redisLock.TryExecuteWithLock(
                        key,
                        new LockAction
                        {
                            Success = () =>
                            {
                                var current = Interlocked.Increment(ref activeOwners);
                                UpdateMax(ref maxActiveOwners, current);
                                Thread.Sleep(80);
                                Interlocked.Decrement(ref activeOwners);
                                Interlocked.Increment(ref successCount);
                            },
                            Fail = exception => throw exception
                        },
                        TimeSpan.FromSeconds(5),
                        20,
                        1000);
                }))
                .ToArray();

            await Task.WhenAll(tasks);
        }
        finally
        {
            cache.RemoveCache(key);
        }

        Assert.Equal(4, successCount);
        Assert.Equal(1, maxActiveOwners);
    }

    /// <summary>
    /// <para>zh-cn:验证真实 Redis 锁租约短于业务执行时间时，第二个持有者可能在第一个回调结束前拿到锁。</para>
    /// <para>en-us:Verifies a second owner may acquire a real Redis lock before the first callback ends when the lease is shorter than business execution.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "Redis")]
    public async Task Redis_lock_should_expose_lease_expiry_risk_when_business_runs_longer_than_lock()
    {
        if (!IsEnabled())
        {
            return;
        }

        var cache = CreateRedisCache();
        using var moduleHost = BuildRedisModuleHost(cache);
        var key = BuildKey("lock-lease-risk");
        var firstEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondEnteredBeforeFirstExited = false;
        var firstExited = false;

        try
        {
            var firstTask = Task.Run(() =>
            {
                var redisLock = new RedisLockDependency();
                redisLock.TryExecuteWithLock(
                    key,
                    () =>
                    {
                        firstEntered.SetResult();
                        Thread.Sleep(300);
                        firstExited = true;
                    },
                    TimeSpan.FromSeconds(2),
                    20,
                    100);
            });

            await firstEntered.Task;

            var secondTask = Task.Run(() =>
            {
                var redisLock = new RedisLockDependency();
                redisLock.TryExecuteWithLock(
                    key,
                    () => secondEnteredBeforeFirstExited = !firstExited,
                    TimeSpan.FromSeconds(2),
                    20,
                    100);
            });

            await Task.WhenAll(firstTask, secondTask);
        }
        finally
        {
            cache.RemoveCache(key);
        }

        Assert.True(secondEnteredBeforeFirstExited);
    }

    /// <summary>
    /// <para>zh-cn:验证真实 Redis 异步 Success 抛出异常时会进入 Fail 回调。</para>
    /// <para>en-us:Verifies async Success exceptions are delivered to the Fail callback on a real Redis lock.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "Redis")]
    public async Task Redis_lock_async_should_invoke_fail_when_success_callback_throws()
    {
        if (!IsEnabled())
        {
            return;
        }

        var cache = CreateRedisCache();
        using var moduleHost = BuildRedisModuleHost(cache);
        var key = BuildKey("lock-async-success-throws");
        var successException = new InvalidOperationException("async-success-callback-error");
        Exception? failure = null;

        try
        {
            var redisLock = new RedisLockDependency();
            await redisLock.TryExecuteWithLockAsync(
                key,
                new LockAsyncAction
                {
                    Success = () => throw successException,
                    Fail = exception => failure = exception
                },
                TimeSpan.FromSeconds(3),
                100,
                3000);
        }
        finally
        {
            cache.RemoveCache(key);
        }

        Assert.Same(successException, failure);
    }

    /// <summary>
    /// <para>zh-cn:验证真实 Redis 异步 Fail 自身抛出异常时会包装为 AggregateException。</para>
    /// <para>en-us:Verifies async Fail callback exceptions on a real Redis lock are wrapped as AggregateException.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "Redis")]
    public async Task Redis_lock_async_should_wrap_fail_callback_exception()
    {
        if (!IsEnabled())
        {
            return;
        }

        var cache = CreateRedisCache();
        using var moduleHost = BuildRedisModuleHost(cache);
        var key = BuildKey("lock-async-fail-throws");

        try
        {
            var redisLock = new RedisLockDependency();
            var exception = await Assert.ThrowsAsync<AggregateException>(() =>
                redisLock.TryExecuteWithLockAsync(
                    key,
                    new LockAsyncAction
                    {
                        Success = () => throw new InvalidOperationException("async-success-callback-error"),
                        Fail = _ => throw new InvalidOperationException("async-fail-callback-error")
                    },
                    TimeSpan.FromSeconds(3),
                    100,
                    3000));

            Assert.Contains(
                FlattenMessages(exception),
                message => message.Contains("async-success-callback-error"));
            Assert.Contains(
                FlattenMessages(exception),
                message => message.Contains("async-fail-callback-error"));
        }
        finally
        {
            cache.RemoveCache(key);
        }
    }

    /// <summary>
    /// <para>zh-cn:验证真实 Redis 锁会标准化非法等待间隔与锁时长，仍能成功执行回调。</para>
    /// <para>en-us:Verifies real Redis lock normalizes invalid wait-step and lock duration values and still executes the callback.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "Redis")]
    public void Redis_lock_should_normalize_invalid_timing_arguments()
    {
        if (!IsEnabled())
        {
            return;
        }

        var cache = CreateRedisCache();
        using var moduleHost = BuildRedisModuleHost(cache);
        var key = BuildKey("lock-normalize");
        var successCount = 0;

        try
        {
            var redisLock = new RedisLockDependency();
            redisLock.TryExecuteWithLock(
                key,
                () => successCount++,
                TimeSpan.FromMilliseconds(10),
                0,
                0);
        }
        finally
        {
            cache.RemoveCache(key);
        }

        Assert.Equal(1, successCount);
    }

    /// <summary>
    /// <para>zh-cn:验证真实 Redis LockRelease 使用 token 校验，旧持有者不能释放租约过期后新持有者的锁。</para>
    /// <para>en-us:Verifies real Redis LockRelease checks tokens so an old owner cannot release a new owner's lock after lease expiry.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "Redis")]
    public void Redis_lock_should_not_release_lock_owned_by_other_token()
    {
        if (!IsEnabled())
        {
            return;
        }

        var cache = CreateRedisCache();
        using var moduleHost = BuildRedisModuleHost(cache);
        var redis = GetRedisDatabase(cache);
        var key = BuildKey("lock-token-release");
        var secondToken = Guid.NewGuid().ToString("N");
        var secondOwnerAcquired = false;

        try
        {
            var redisLock = new RedisLockDependency();
            redisLock.TryExecuteWithLock(
                key,
                () =>
                {
                    Thread.Sleep(180);
                    secondOwnerAcquired = redis.LockTake(key, secondToken, TimeSpan.FromSeconds(5));
                },
                TimeSpan.FromSeconds(2),
                20,
                80);

            Assert.True(secondOwnerAcquired);
            Assert.True(redis.LockRelease(key, secondToken));
        }
        finally
        {
            cache.RemoveCache(key);
        }
    }

    private static RedisCacheDependency CreateRedisCache()
    {
        return new RedisCacheDependency(new RedisSettingsOptions
        {
            ConnectionString = GetRequiredConfiguration("RedisIntegration:ConnectionString"),
            UserName = AppConfigurationLoader.InnerConfiguration["RedisIntegration:UserName"] ?? string.Empty,
            Password = AppConfigurationLoader.InnerConfiguration["RedisIntegration:Password"] ?? string.Empty
        });
    }

    private static RedisModuleHost BuildRedisModuleHost(RedisCacheDependency cache)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IRedisCacheStandard>(cache);
        services.AddSingleton<IAppCacheStandard>(cache);
        services.AddSingleton<IDistributedLockStandard, RedisLockDependency>();

        var originalRootServices = AppCore.RootServices;
        var originalInternalServices = AppCore.InternalServices;
        var originalStartType = AppCore.AppStartType;

        var serviceProvider = services.BuildServiceProvider();
        AppCore.RootServices = serviceProvider;
        AppCore.InternalServices = services;
        AppCore.AppStartType = AppStartupTypeEnum.WEB;

        return new RedisModuleHost(
            serviceProvider,
            originalRootServices,
            originalInternalServices,
            originalStartType);
    }

    private static IDatabase GetRedisDatabase(RedisCacheDependency cache)
    {
        return (IDatabase)cache.GetDatabase();
    }

    private static bool IsEnabled()
    {
        return string.Equals(
            AppConfigurationLoader.InnerConfiguration["RedisIntegration:RunRedisTests"],
            "true",
            StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildKey(string suffix)
    {
        var prefix = AppConfigurationLoader.InnerConfiguration["RedisIntegration:KeyPrefix"];
        if (string.IsNullOrWhiteSpace(prefix))
        {
            prefix = "air-cloud-it";
        }

        return $"{prefix}:redis:{suffix}:{Guid.NewGuid():N}";
    }

    private static string GetRequiredConfiguration(string key)
    {
        var value = AppConfigurationLoader.InnerConfiguration[key];
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"{key} is required.");
        }

        return value;
    }

    private static IEnumerable<string> FlattenMessages(Exception exception)
    {
        yield return exception.Message;

        if (exception is AggregateException aggregateException)
        {
            foreach (var innerException in aggregateException.InnerExceptions)
            {
                foreach (var innerMessage in FlattenMessages(innerException))
                {
                    yield return innerMessage;
                }
            }
        }
        else if (exception.InnerException != null)
        {
            foreach (var innerMessage in FlattenMessages(exception.InnerException))
            {
                yield return innerMessage;
            }
        }
    }

    private static void UpdateMax(ref int target, int candidate)
    {
        int snapshot;
        do
        {
            snapshot = target;
            if (candidate <= snapshot)
            {
                return;
            }
        }
        while (Interlocked.CompareExchange(ref target, candidate, snapshot) != snapshot);
    }

    private sealed class RedisPayload
    {
        public string Id { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;
    }

    private sealed class RedisModuleHost : IDisposable
    {
        private readonly IServiceProvider _originalRootServices;
        private readonly IServiceCollection _originalInternalServices;
        private readonly AppStartupTypeEnum _originalStartType;

        public RedisModuleHost(
            ServiceProvider serviceProvider,
            IServiceProvider originalRootServices,
            IServiceCollection originalInternalServices,
            AppStartupTypeEnum originalStartType)
        {
            ServiceProvider = serviceProvider;
            _originalRootServices = originalRootServices;
            _originalInternalServices = originalInternalServices;
            _originalStartType = originalStartType;
        }

        public ServiceProvider ServiceProvider { get; }

        public void Dispose()
        {
            AppCore.RootServices = _originalRootServices;
            AppCore.InternalServices = _originalInternalServices;
            AppCore.AppStartType = _originalStartType;
            ServiceProvider.Dispose();
        }
    }
}
