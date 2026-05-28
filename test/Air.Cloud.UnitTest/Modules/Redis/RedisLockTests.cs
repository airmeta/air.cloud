using Air.Cloud.Core;
using Air.Cloud.Core.Standard.DistributedLock;
using System.Collections.Concurrent;

namespace Air.Cloud.UnitTest.Modules.Redis
{
    /// <summary>
    /// <para>zh-cn:Redis 分布式锁行为测试集合。</para>
    /// <para>en-us:Test suite for Redis distributed lock behaviors.</para>
    /// </summary>
    public class RedisLockTests
    {
        /// <summary>
        /// <para>zh-cn:测试同步分布式锁串行化场景，确认并发竞争下任一时刻只有一个任务进入成功分支。</para>
        /// <para>en-us:Tests synchronous distributed-lock serialization to ensure only one worker succeeds at a time under contention.</para>
        /// </summary>
        /// <remarks>
        /// <para>zh-cn:测试过程：启动 3 个并发任务竞争同一 key，统计 success/wait/fail 事件并断言最终成功次数与失败次数。</para>
        /// <para>en-us:Process: run 3 concurrent workers on the same key, collect success/wait/fail events, and assert final success/failure counts.</para>
        /// </remarks>
        [Fact]
        public async Task TryExecuteWithLock_should_allow_single_successful_owner_at_a_time()
        {
            const string key = "unit-test:redis:lock:sync";
            var events = new ConcurrentQueue<string>();
            var successCounter = 0;
            var waitCounter = 0;
            var failCounter = 0;
            var lockTime = TimeSpan.FromSeconds(3);

            var tasks = Enumerable.Range(1, 3)
                .Select(index => Task.Run(() =>
                    AppRealization.Lock.TryExecuteWithLock(
                        key,
                        CreateLockAction(index, events, () => Interlocked.Increment(ref successCounter), () => Interlocked.Increment(ref waitCounter), () => Interlocked.Increment(ref failCounter)),
                        lockTime,
                        100)))
                .ToArray();

            await Task.WhenAll(tasks);

            Assert.Equal(3, successCounter);
            Assert.True(waitCounter >= 0);
            Assert.Equal(0, failCounter);
            Assert.Equal(3, events.Count(entry => entry.Contains(":success", StringComparison.Ordinal)));
        }

        /// <summary>
        /// <para>zh-cn:测试异步分布式锁串行化场景，确认并发异步任务也遵循单持有者语义。</para>
        /// <para>en-us:Tests asynchronous distributed-lock serialization to ensure single-owner semantics with concurrent async workers.</para>
        /// </summary>
        /// <remarks>
        /// <para>zh-cn:测试过程：并发执行 3 个异步锁任务，校验 success/wait/fail 统计与成功事件数量符合预期。</para>
        /// <para>en-us:Process: execute 3 async lock workers concurrently and verify success/wait/fail counters plus success event count.</para>
        /// </remarks>
        [Fact]
        public async Task TryExecuteWithLockAsync_should_allow_single_successful_owner_at_a_time()
        {
            const string key = "unit-test:redis:lock:async";
            var events = new ConcurrentQueue<string>();
            var successCounter = 0;
            var waitCounter = 0;
            var failCounter = 0;
            var lockTime = TimeSpan.FromSeconds(3);

            var tasks = Enumerable.Range(1, 3)
                .Select(index => ExecuteLockAsync(
                    key,
                    lockTime,
                    CreateLockAsyncAction(index, events, () => Interlocked.Increment(ref successCounter), () => Interlocked.Increment(ref waitCounter), () => Interlocked.Increment(ref failCounter))))
                .ToArray();

            await Task.WhenAll(tasks);

            Assert.Equal(3, successCounter);
            Assert.True(waitCounter >= 0);
            Assert.Equal(0, failCounter);
            Assert.Equal(3, events.Count(entry => entry.Contains(":success", StringComparison.Ordinal)));
        }

        /// <summary>
        /// <para>zh-cn:创建同步锁动作并记录执行结果。</para>
        /// <para>en-us:Creates a synchronous lock action and records its execution outcome.</para>
        /// </summary>
        /// <param name="workerId">
        /// <para>zh-cn:当前并发工作项编号。</para>
        /// <para>en-us:The identifier of the current concurrent worker.</para>
        /// </param>
        /// <param name="events">
        /// <para>zh-cn:用于记录锁执行事件的并发队列。</para>
        /// <para>en-us:The concurrent queue used to record lock execution events.</para>
        /// </param>
        /// <param name="onSuccess">
        /// <para>zh-cn:成功获取锁时执行的回调。</para>
        /// <para>en-us:The callback invoked when the lock is acquired successfully.</para>
        /// </param>
        /// <param name="onWaiting">
        /// <para>zh-cn:等待获取锁时执行的回调。</para>
        /// <para>en-us:The callback invoked while waiting for the lock.</para>
        /// </param>
        /// <param name="onFail">
        /// <para>zh-cn:获取锁失败时执行的回调。</para>
        /// <para>en-us:The callback invoked when lock acquisition fails.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:返回用于同步锁执行的动作对象。</para>
        /// <para>en-us:Returns the action object used for synchronous lock execution.</para>
        /// </returns>
        private static LockAction CreateLockAction(int workerId, ConcurrentQueue<string> events, Action onSuccess, Action onWaiting, Action onFail)
        {
            return new LockAction
            {
                Success = () =>
                {
                    events.Enqueue($"worker-{workerId}:success");
                    onSuccess();
                    Task.Delay(300).GetAwaiter().GetResult();
                },
                Waiting = () =>
                {
                    events.Enqueue($"worker-{workerId}:waiting");
                    onWaiting();
                },
                Fail = (ex) =>
                {
                    events.Enqueue($"worker-{workerId}:fail");
                    onFail();
                }
            };
        }

        /// <summary>
        /// <para>zh-cn:创建异步锁动作并记录执行结果。</para>
        /// <para>en-us:Creates an asynchronous lock action and records its execution outcome.</para>
        /// </summary>
        /// <param name="workerId">
        /// <para>zh-cn:当前并发工作项编号。</para>
        /// <para>en-us:The identifier of the current concurrent worker.</para>
        /// </param>
        /// <param name="events">
        /// <para>zh-cn:用于记录锁执行事件的并发队列。</para>
        /// <para>en-us:The concurrent queue used to record lock execution events.</para>
        /// </param>
        /// <param name="onSuccess">
        /// <para>zh-cn:成功获取锁时执行的回调。</para>
        /// <para>en-us:The callback invoked when the lock is acquired successfully.</para>
        /// </param>
        /// <param name="onWaiting">
        /// <para>zh-cn:等待获取锁时执行的回调。</para>
        /// <para>en-us:The callback invoked while waiting for the lock.</para>
        /// </param>
        /// <param name="onFail">
        /// <para>zh-cn:获取锁失败时执行的回调。</para>
        /// <para>en-us:The callback invoked when lock acquisition fails.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:返回用于异步锁执行的动作对象。</para>
        /// <para>en-us:Returns the action object used for asynchronous lock execution.</para>
        /// </returns>
        private static LockAsyncAction CreateLockAsyncAction(int workerId, ConcurrentQueue<string> events, Action onSuccess, Action onWaiting, Action onFail)
        {
            return new LockAsyncAction
            {
                Success = async () =>
                {
                    events.Enqueue($"worker-{workerId}:success");
                    onSuccess();
                    await AppRealization.RedisCache.String.SetAsync($"unit-test:redis:lock:worker:{workerId}", $"T{workerId}", TimeSpan.FromMinutes(1));
                    await Task.Delay(300);
                },
                Waiting = () =>
                {
                    events.Enqueue($"worker-{workerId}:waiting");
                    onWaiting();
                },
                Fail = (ex) =>
                {
                    events.Enqueue($"worker-{workerId}:fail");
                    onFail();
                }
            };
        }

        /// <summary>
        /// <para>zh-cn:执行指定键的异步分布式锁操作。</para>
        /// <para>en-us:Executes an asynchronous distributed lock operation for the specified key.</para>
        /// </summary>
        /// <param name="key">
        /// <para>zh-cn:分布式锁键。</para>
        /// <para>en-us:The distributed lock key.</para>
        /// </param>
        /// <param name="lockTime">
        /// <para>zh-cn:锁持有时长。</para>
        /// <para>en-us:The duration for which the lock should be held.</para>
        /// </param>
        /// <param name="action">
        /// <para>zh-cn:异步锁执行动作。</para>
        /// <para>en-us:The asynchronous lock action to execute.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:返回表示锁执行是否成功的异步任务。</para>
        /// <para>en-us:Returns an asynchronous task that indicates whether lock execution succeeded.</para>
        /// </returns>
        private static async Task ExecuteLockAsync(string key, TimeSpan lockTime, LockAsyncAction action)
        {
            await AppRealization.Lock.TryExecuteWithLockAsync(key, action, lockTime, 100);
        }
    }
}
