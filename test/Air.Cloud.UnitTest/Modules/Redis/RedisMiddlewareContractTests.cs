using Air.Cloud.Core.Standard.Cache;
using Air.Cloud.Core.Standard.DistributedLock;

namespace Air.Cloud.UnitTest.Modules.Redis
{
    /// <summary>
    /// <para>zh-cn:Redis 中间件契约单元测试，方法名与真实 Redis 集成测试保持同步。</para>
    /// <para>en-us:Redis middleware contract unit tests with method names synchronized with real Redis integration tests.</para>
    /// </summary>
    /// <remarks>
    /// <para>zh-cn:该类不连接 Redis，只验证业务调用依赖的缓存与锁契约；真实模块行为由同名集成测试兜底。</para>
    /// <para>en-us:This class does not connect to Redis; it verifies cache and lock contracts used by business code, while real module behavior is covered by same-named integration tests.</para>
    /// </remarks>
    public class RedisMiddlewareContractTests
    {
        /// <summary>
        /// <para>zh-cn:与 Redis 集成测试同名，验证字符串缓存写入、读取与删除契约。</para>
        /// <para>en-us:Same name as the Redis integration test, verifying string cache set, get, and remove contracts.</para>
        /// </summary>
        [Fact]
        public void Redis_should_set_get_and_remove_string_value()
        {
            IAppCacheStandard cache = new InMemoryAppCacheStandard();
            const string key = "redis-contract:string";
            const string value = "redis-value";

            Assert.True(cache.SetCache(key, value, TimeSpan.FromMinutes(1)));
            Assert.Equal(value, cache.GetCache(key));
            Assert.True(cache.RemoveCache(key));
            Assert.Null(cache.GetCache(key));
        }

        /// <summary>
        /// <para>zh-cn:与 Redis 集成测试同名，验证对象缓存序列化写入与反序列化读取契约。</para>
        /// <para>en-us:Same name as the Redis integration test, verifying object cache serialization and deserialization contracts.</para>
        /// </summary>
        [Fact]
        public void Redis_should_roundtrip_object_value()
        {
            IAppCacheStandard cache = new InMemoryAppCacheStandard();
            const string key = "redis-contract:object";
            var value = new RedisContractPayload
            {
                Id = "payload-1",
                Name = "redis-object"
            };

            Assert.True(cache.SetCache(key, value, TimeSpan.FromMinutes(1)));
            var cached = cache.GetCache<RedisContractPayload>(key);

            Assert.NotNull(cached);
            Assert.Equal(value.Id, cached.Id);
            Assert.Equal(value.Name, cached.Name);
        }

        /// <summary>
        /// <para>zh-cn:与 Redis 集成测试同名，验证分布式锁成功获取后只执行成功回调。</para>
        /// <para>en-us:Same name as the Redis integration test, verifying the distributed lock executes only the success callback after acquisition.</para>
        /// </summary>
        [Fact]
        public void Redis_lock_should_execute_success_callback_once()
        {
            IDistributedLockStandard redisLock = new InMemoryDistributedLockStandard();
            var successCount = 0;
            Exception? failure = null;

            redisLock.TryExecuteWithLock(
                "redis-contract:lock",
                new LockAction
                {
                    Success = () => successCount++,
                    Fail = exception => failure = exception
                },
                TimeSpan.FromSeconds(1),
                10,
                1000);

            Assert.Equal(1, successCount);
            Assert.Null(failure);
        }

        /// <summary>
        /// <para>zh-cn:与 Redis 集成测试同名，验证锁已被占用时不会执行成功回调，并会进入失败补偿回调。</para>
        /// <para>en-us:Same name as the Redis integration test, verifying an occupied lock skips success and invokes the failure callback.</para>
        /// </summary>
        [Fact]
        public void Redis_lock_should_call_fail_when_lock_is_not_acquired()
        {
            var redisLock = new InMemoryDistributedLockStandard();
            var key = "redis-contract:lock-busy";
            var successCount = 0;
            Exception? failure = null;

            redisLock.HoldLock(key);
            try
            {
                redisLock.TryExecuteWithLock(
                    key,
                    new LockAction
                    {
                        Success = () => successCount++,
                        Fail = exception => failure = exception
                    },
                    TimeSpan.FromMilliseconds(50),
                    10,
                    1000);
            }
            finally
            {
                redisLock.ReleaseLock(key);
            }

            Assert.Equal(0, successCount);
            Assert.NotNull(failure);
            Assert.Contains("Failed to acquire lock", failure.Message);
        }

        /// <summary>
        /// <para>zh-cn:与 Redis 集成测试同名，验证成功回调内部抛出异常时，锁标准会把原始异常传递给 Fail 回调。</para>
        /// <para>en-us:Same name as the Redis integration test, verifying the original success-callback exception is delivered to the Fail callback.</para>
        /// </summary>
        [Fact]
        public void Redis_lock_should_invoke_fail_when_success_callback_throws()
        {
            IDistributedLockStandard redisLock = new InMemoryDistributedLockStandard();
            var successException = new InvalidOperationException("success-callback-error");
            Exception? failure = null;

            redisLock.TryExecuteWithLock(
                "redis-contract:lock-success-throws",
                new LockAction
                {
                    Success = () => throw successException,
                    Fail = exception => failure = exception
                },
                TimeSpan.FromSeconds(1),
                10,
                1000);

            Assert.Same(successException, failure);
        }

        /// <summary>
        /// <para>zh-cn:与 Redis 集成测试同名，验证 Waiting 回调只是等待通知；即使 Waiting 抛出异常，也不能覆盖最终失败补偿。</para>
        /// <para>en-us:Same name as the Redis integration test, verifying Waiting is only a notification callback and its exception cannot replace final failure compensation.</para>
        /// </summary>
        [Fact]
        public void Redis_lock_should_ignore_waiting_callback_exception()
        {
            var redisLock = new InMemoryDistributedLockStandard();
            var key = "redis-contract:lock-waiting-throws";
            var waitingCount = 0;
            Exception? failure = null;

            redisLock.HoldLock(key);
            try
            {
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
                    TimeSpan.FromMilliseconds(50),
                    10,
                    1000);
            }
            finally
            {
                redisLock.ReleaseLock(key);
            }

            Assert.True(waitingCount > 0);
            Assert.NotNull(failure);
            Assert.Contains("Failed to acquire lock", failure.Message);
        }

        /// <summary>
        /// <para>zh-cn:与 Redis 集成测试同名，验证 Fail 回调自身异常会被包装为 AggregateException，避免吞掉补偿失败。</para>
        /// <para>en-us:Same name as the Redis integration test, verifying a Fail callback exception is wrapped as AggregateException instead of being swallowed.</para>
        /// </summary>
        [Fact]
        public void Redis_lock_should_wrap_fail_callback_exception()
        {
            IDistributedLockStandard redisLock = new InMemoryDistributedLockStandard();

            var exception = Assert.Throws<AggregateException>(() =>
                redisLock.TryExecuteWithLock(
                    "redis-contract:lock-fail-throws",
                    new LockAction
                    {
                        Success = () => throw new InvalidOperationException("success-callback-error"),
                        Fail = _ => throw new InvalidOperationException("fail-callback-error")
                    },
                    TimeSpan.FromSeconds(1),
                    10,
                    1000));

            Assert.Contains(
                FlattenMessages(exception),
                message => message.Contains("success-callback-error"));
            Assert.Contains(
                FlattenMessages(exception),
                message => message.Contains("fail-callback-error"));
        }

        /// <summary>
        /// <para>zh-cn:与 Redis 集成测试同名，验证异步锁成功路径只执行一次 Success 回调，并且不会触发 Fail。</para>
        /// <para>en-us:Same name as the Redis integration test, verifying the async lock success path executes Success once and does not invoke Fail.</para>
        /// </summary>
        [Fact]
        public async Task Redis_lock_async_should_execute_success_callback_once()
        {
            IDistributedLockStandard redisLock = new InMemoryDistributedLockStandard();
            var successCount = 0;
            Exception? failure = null;

            await redisLock.TryExecuteWithLockAsync(
                "redis-contract:lock-async",
                new LockAsyncAction
                {
                    Success = () =>
                    {
                        successCount++;
                        return Task.CompletedTask;
                    },
                    Fail = exception => failure = exception
                },
                TimeSpan.FromSeconds(1),
                10,
                1000);

            Assert.Equal(1, successCount);
            Assert.Null(failure);
        }

        /// <summary>
        /// <para>zh-cn:与 Redis 集成测试同名，验证成功执行后会释放锁，同一个 Key 可以被后续调用重新获取。</para>
        /// <para>en-us:Same name as the Redis integration test, verifying the lock is released after success so the same key can be acquired again.</para>
        /// </summary>
        [Fact]
        public void Redis_lock_should_release_lock_after_success()
        {
            IDistributedLockStandard redisLock = new InMemoryDistributedLockStandard();
            const string key = "redis-contract:lock-release";
            var successCount = 0;

            redisLock.TryExecuteWithLock(
                key,
                () => successCount++,
                TimeSpan.FromSeconds(1),
                10,
                1000);

            redisLock.TryExecuteWithLock(
                key,
                () => successCount++,
                TimeSpan.FromSeconds(1),
                10,
                1000);

            Assert.Equal(2, successCount);
        }

        /// <summary>
        /// <para>zh-cn:与 Redis 集成测试同名，验证 Hash、List、Set、SortedSet 与 Key 基础结构操作形成读写闭环。</para>
        /// <para>en-us:Same name as the Redis integration test, verifying Hash, List, Set, SortedSet, and Key structure operations form a read/write loop.</para>
        /// </summary>
        [Fact]
        public void Redis_native_structures_should_roundtrip_hash_list_set_sortedset_and_key()
        {
            var native = new InMemoryRedisNativeContracts();

            native.HashSet("hash", "field", "hash-value");
            native.ListRightPush("list", "list-value");
            native.SetAdd("set", "set-value");
            native.SortedSetAdd("sorted-set", "sorted-value", 10);
            native.KeyRename("hash", "hash-renamed");

            Assert.Equal("hash-value", native.HashGet("hash-renamed", "field"));
            Assert.Equal("list-value", native.ListRightPop("list"));
            Assert.True(native.SetContains("set", "set-value"));
            Assert.Equal("sorted-value", native.SortedSetFirst("sorted-set"));
            Assert.False(native.KeyExists("hash"));
            Assert.True(native.KeyExists("hash-renamed"));
        }

        /// <summary>
        /// <para>zh-cn:与 Redis 集成测试同名，验证多个工作项竞争同一把锁时，成功回调不会并发重入。</para>
        /// <para>en-us:Same name as the Redis integration test, verifying success callbacks do not re-enter concurrently when multiple workers compete for one lock.</para>
        /// </summary>
        [Fact]
        public async Task Redis_lock_should_serialize_concurrent_workers()
        {
            IDistributedLockStandard redisLock = new InMemoryDistributedLockStandard();
            var activeOwners = 0;
            var maxActiveOwners = 0;
            var successCount = 0;

            var tasks = Enumerable.Range(0, 4)
                .Select(_ => Task.Run(() =>
                    redisLock.TryExecuteWithLock(
                        "redis-contract:lock-concurrent",
                        new LockAction
                        {
                            Success = () =>
                            {
                                var current = Interlocked.Increment(ref activeOwners);
                                UpdateMax(ref maxActiveOwners, current);
                                Thread.Sleep(40);
                                Interlocked.Decrement(ref activeOwners);
                                Interlocked.Increment(ref successCount);
                            },
                            Fail = exception => throw exception
                        },
                        TimeSpan.FromSeconds(2),
                        10,
                        200)))
                .ToArray();

            await Task.WhenAll(tasks);

            Assert.Equal(4, successCount);
            Assert.Equal(1, maxActiveOwners);
        }

        /// <summary>
        /// <para>zh-cn:与 Redis 集成测试同名，验证业务执行时间超过锁租约时，第二个持有者可能在第一个回调结束前拿到锁。</para>
        /// <para>en-us:Same name as the Redis integration test, verifying a second owner may acquire the lock before the first callback ends when business execution exceeds the lease.</para>
        /// </summary>
        [Fact]
        public async Task Redis_lock_should_expose_lease_expiry_risk_when_business_runs_longer_than_lock()
        {
            IDistributedLockStandard redisLock = new InMemoryDistributedLockStandard();
            var firstEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var secondEnteredBeforeFirstExited = false;
            var firstExited = false;

            var firstTask = Task.Run(() =>
                redisLock.TryExecuteWithLock(
                    "redis-contract:lock-lease-risk",
                    () =>
                    {
                        firstEntered.SetResult();
                        Thread.Sleep(180);
                        firstExited = true;
                    },
                    TimeSpan.FromSeconds(1),
                    10,
                    60));

            await firstEntered.Task;

            var secondTask = Task.Run(() =>
                redisLock.TryExecuteWithLock(
                    "redis-contract:lock-lease-risk",
                    () => secondEnteredBeforeFirstExited = !firstExited,
                    TimeSpan.FromSeconds(1),
                    10,
                    60));

            await Task.WhenAll(firstTask, secondTask);

            Assert.True(secondEnteredBeforeFirstExited);
        }

        /// <summary>
        /// <para>zh-cn:与 Redis 集成测试同名，验证异步 Success 抛出异常时会进入 Fail 回调。</para>
        /// <para>en-us:Same name as the Redis integration test, verifying async Success exceptions are delivered to the Fail callback.</para>
        /// </summary>
        [Fact]
        public async Task Redis_lock_async_should_invoke_fail_when_success_callback_throws()
        {
            IDistributedLockStandard redisLock = new InMemoryDistributedLockStandard();
            var successException = new InvalidOperationException("async-success-callback-error");
            Exception? failure = null;

            await redisLock.TryExecuteWithLockAsync(
                "redis-contract:lock-async-success-throws",
                new LockAsyncAction
                {
                    Success = () => throw successException,
                    Fail = exception => failure = exception
                },
                TimeSpan.FromSeconds(1),
                10,
                1000);

            Assert.Same(successException, failure);
        }

        /// <summary>
        /// <para>zh-cn:与 Redis 集成测试同名，验证异步 Fail 自身抛出异常时会包装为 AggregateException。</para>
        /// <para>en-us:Same name as the Redis integration test, verifying async Fail callback exceptions are wrapped as AggregateException.</para>
        /// </summary>
        [Fact]
        public async Task Redis_lock_async_should_wrap_fail_callback_exception()
        {
            IDistributedLockStandard redisLock = new InMemoryDistributedLockStandard();

            var exception = await Assert.ThrowsAsync<AggregateException>(() =>
                redisLock.TryExecuteWithLockAsync(
                    "redis-contract:lock-async-fail-throws",
                    new LockAsyncAction
                    {
                        Success = () => throw new InvalidOperationException("async-success-callback-error"),
                        Fail = _ => throw new InvalidOperationException("async-fail-callback-error")
                    },
                    TimeSpan.FromSeconds(1),
                    10,
                    1000));

            Assert.Contains(
                FlattenMessages(exception),
                message => message.Contains("async-success-callback-error"));
            Assert.Contains(
                FlattenMessages(exception),
                message => message.Contains("async-fail-callback-error"));
        }

        /// <summary>
        /// <para>zh-cn:与 Redis 集成测试同名，验证非法等待间隔与锁时长会被标准化，仍能成功执行锁回调。</para>
        /// <para>en-us:Same name as the Redis integration test, verifying invalid wait-step and lock duration values are normalized and the lock callback still runs.</para>
        /// </summary>
        [Fact]
        public void Redis_lock_should_normalize_invalid_timing_arguments()
        {
            IDistributedLockStandard redisLock = new InMemoryDistributedLockStandard();
            var successCount = 0;

            redisLock.TryExecuteWithLock(
                "redis-contract:lock-normalize",
                () => successCount++,
                TimeSpan.FromMilliseconds(10),
                0,
                0);

            Assert.Equal(1, successCount);
        }

        private sealed class RedisContractPayload
        {
            public string Id { get; set; } = string.Empty;

            public string Name { get; set; } = string.Empty;
        }

        private sealed class InMemoryAppCacheStandard : IAppCacheStandard
        {
            private readonly Dictionary<string, string> _values = new();

            public bool SetCache(string Key, string Content, TimeSpan? timeSpan = null)
            {
                _values[Key] = Content;
                return true;
            }

            public string GetCache(string Key)
            {
                return _values.TryGetValue(Key, out var value) ? value : null!;
            }

            public bool SetCache<T>(string Key, T t, TimeSpan? timeSpan = null)
                where T : class, new()
            {
                _values[Key] = System.Text.Json.JsonSerializer.Serialize(t);
                return true;
            }

            public T GetCache<T>(string Key)
                where T : class, new()
            {
                return _values.TryGetValue(Key, out var value)
                    ? System.Text.Json.JsonSerializer.Deserialize<T>(value)!
                    : null!;
            }

            public bool RemoveCache(string Key)
            {
                return _values.Remove(Key);
            }
        }

        private sealed class InMemoryDistributedLockStandard : IDistributedLockStandard
        {
            private readonly Dictionary<string, DateTime> _locks = new();
            private readonly object _syncRoot = new();

            public void HoldLock(string key)
            {
                lock (_syncRoot)
                {
                    _locks[key] = DateTime.MaxValue;
                }
            }

            public void ReleaseLock(string key)
            {
                lock (_syncRoot)
                {
                    _locks.Remove(key);
                }
            }

            public void TryExecuteWithLock(string key, Action action, TimeSpan WaitMilliseconds, int StepWaitMilliseconds = 200, int LockMilliseconds = 30000)
            {
                TryExecuteWithLock(
                    key,
                    new LockAction
                    {
                        Success = action
                    },
                    WaitMilliseconds,
                    StepWaitMilliseconds,
                    LockMilliseconds);
            }

            public void TryExecuteWithLock(string key, LockAction action, TimeSpan WaitMilliseconds, int StepWaitMilliseconds = 200, int LockMilliseconds = 30000)
            {
                NormalizeTiming(ref StepWaitMilliseconds, ref LockMilliseconds);
                var deadline = DateTime.UtcNow.Add(WaitMilliseconds);
                var acquired = TryAcquire(key, LockMilliseconds);

                while (!acquired && DateTime.UtcNow < deadline)
                {
                    TryInvokeWaiting(action.Waiting);
                    Thread.Sleep(StepWaitMilliseconds);
                    acquired = TryAcquire(key, LockMilliseconds);
                }

                if (!acquired)
                {
                    TryInvokeFail(action.Fail, new InvalidOperationException($"Failed to acquire lock, key: {key}"));
                    return;
                }

                try
                {
                    try
                    {
                        action.Success?.Invoke();
                    }
                    catch (Exception exception)
                    {
                        TryInvokeFail(action.Fail, exception);
                    }
                }
                finally
                {
                    ReleaseLock(key);
                }
            }

            public Task TryExecuteWithLockAsync(string key, Func<Task> action, TimeSpan WaitMilliseconds, int StepWaitMilliseconds = 200, int LockMilliseconds = 30000)
            {
                return TryExecuteWithLockAsync(
                    key,
                    new LockAsyncAction
                    {
                        Success = action
                    },
                    WaitMilliseconds,
                    StepWaitMilliseconds,
                    LockMilliseconds);
            }

            public async Task TryExecuteWithLockAsync(string key, LockAsyncAction action, TimeSpan WaitMilliseconds, int StepWaitMilliseconds = 200, int LockMilliseconds = 30000)
            {
                NormalizeTiming(ref StepWaitMilliseconds, ref LockMilliseconds);
                var deadline = DateTime.UtcNow.Add(WaitMilliseconds);
                var acquired = TryAcquire(key, LockMilliseconds);

                while (!acquired && DateTime.UtcNow < deadline)
                {
                    TryInvokeWaiting(action.Waiting);
                    await Task.Delay(StepWaitMilliseconds);
                    acquired = TryAcquire(key, LockMilliseconds);
                }

                if (!acquired)
                {
                    TryInvokeFail(action.Fail, new InvalidOperationException($"Failed to acquire lock, key: {key}"));
                    return;
                }

                try
                {
                    try
                    {
                        if (action.Success != null)
                        {
                            await action.Success.Invoke();
                        }
                    }
                    catch (Exception exception)
                    {
                        TryInvokeFail(action.Fail, exception);
                    }
                }
                finally
                {
                    ReleaseLock(key);
                }
            }

            private bool TryAcquire(string key, int lockMilliseconds)
            {
                lock (_syncRoot)
                {
                    if (_locks.TryGetValue(key, out var expiresAt) && expiresAt > DateTime.UtcNow)
                    {
                        return false;
                    }

                    _locks[key] = DateTime.UtcNow.AddMilliseconds(lockMilliseconds);
                    return true;
                }
            }

            private static void NormalizeTiming(ref int stepWaitMilliseconds, ref int lockMilliseconds)
            {
                if (stepWaitMilliseconds <= 0)
                {
                    stepWaitMilliseconds = 200;
                }

                if (lockMilliseconds <= 0)
                {
                    lockMilliseconds = 30000;
                }

                lockMilliseconds = Math.Max(lockMilliseconds, stepWaitMilliseconds);
            }

            private static void TryInvokeWaiting(Action waiting)
            {
                try
                {
                    waiting?.Invoke();
                }
                catch
                {
                    // zh-cn:Waiting 只用于等待通知，异常不能影响加锁主流程。
                    // en-us:Waiting is only a notification callback; its exception must not affect the lock flow.
                }
            }

            private static void TryInvokeFail(Action<Exception> fail, Exception originalException)
            {
                try
                {
                    fail?.Invoke(originalException);
                }
                catch (Exception failException)
                {
                    throw new AggregateException(
                        "Redis lock execution failed",
                        originalException,
                        failException);
                }
            }
        }

        private sealed class InMemoryRedisNativeContracts
        {
            private readonly Dictionary<string, Dictionary<string, string>> _hashValues = new();
            private readonly Dictionary<string, List<string>> _listValues = new();
            private readonly Dictionary<string, HashSet<string>> _setValues = new();
            private readonly Dictionary<string, SortedDictionary<double, string>> _sortedSetValues = new();

            public void HashSet(string key, string field, string value)
            {
                if (!_hashValues.TryGetValue(key, out var hash))
                {
                    hash = new Dictionary<string, string>();
                    _hashValues[key] = hash;
                }

                hash[field] = value;
            }

            public string HashGet(string key, string field)
            {
                return _hashValues[key][field];
            }

            public void ListRightPush(string key, string value)
            {
                if (!_listValues.TryGetValue(key, out var list))
                {
                    list = new List<string>();
                    _listValues[key] = list;
                }

                list.Add(value);
            }

            public string ListRightPop(string key)
            {
                var list = _listValues[key];
                var value = list[^1];
                list.RemoveAt(list.Count - 1);
                return value;
            }

            public void SetAdd(string key, string value)
            {
                if (!_setValues.TryGetValue(key, out var set))
                {
                    set = new HashSet<string>();
                    _setValues[key] = set;
                }

                set.Add(value);
            }

            public bool SetContains(string key, string value)
            {
                return _setValues.TryGetValue(key, out var set) && set.Contains(value);
            }

            public void SortedSetAdd(string key, string value, double score)
            {
                if (!_sortedSetValues.TryGetValue(key, out var sortedSet))
                {
                    sortedSet = new SortedDictionary<double, string>();
                    _sortedSetValues[key] = sortedSet;
                }

                sortedSet[score] = value;
            }

            public string SortedSetFirst(string key)
            {
                return _sortedSetValues[key].First().Value;
            }

            public void KeyRename(string key, string newKey)
            {
                if (_hashValues.Remove(key, out var hash))
                {
                    _hashValues[newKey] = hash;
                }
            }

            public bool KeyExists(string key)
            {
                return _hashValues.ContainsKey(key)
                    || _listValues.ContainsKey(key)
                    || _setValues.ContainsKey(key)
                    || _sortedSetValues.ContainsKey(key);
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
    }
}
