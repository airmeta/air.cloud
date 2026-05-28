using Air.Cloud.Core.Modules.AppPrint;
using Air.Cloud.Core.Standard.DistributedLock;

using StackExchange.Redis;

namespace Air.Cloud.Modules.RedisCache.Dependencies
{
    /// <summary>
    /// <para>zh-cn:Redis锁依赖实现</para>
    /// <para>en-us:Redis Lock Dependency Implementation</para>
    /// </summary>
    public class RedisLockDependency : IDistributedLockStandard
    {
        #region Private Methods

        /// <summary>
        /// 获取最大等待次数
        /// </summary>
        private int GetCounter(
            TimeSpan timeout,
            int stepWaitMilliseconds = 200)
        {
            if (stepWaitMilliseconds <= 0)
            {
                stepWaitMilliseconds = 200;
            }

            // 至少尝试一次
            return Math.Max(
                1,
                (int)Math.Ceiling(
                    timeout.TotalMilliseconds /
                    stepWaitMilliseconds
                )
            );
        }

        /// <summary>
        /// 参数标准化
        /// </summary>
        private void NormalizeArguments(
            ref int stepWaitMilliseconds,
            ref int lockMilliseconds)
        {
            if (stepWaitMilliseconds <= 0)
            {
                stepWaitMilliseconds = 200;
            }

            if (lockMilliseconds <= 0)
            {
                lockMilliseconds = 30000;
            }

            // 防止锁时间小于等待间隔
            lockMilliseconds =
                Math.Max(
                    lockMilliseconds,
                    stepWaitMilliseconds);
        }

        /// <summary>
        /// 统一错误日志
        /// </summary>
        private void PrintError(
            string title,
            Exception ex,
            string key)
        {
            AppRealization.Output.Print(
                "系统锁通知",
                $"{title}:{ex.Message}",
                AppPrintLevel.Error,
                AdditionalParams:
                new Dictionary<string, object>()
                {
                    {"message", ex.Message},
                    {"StackTrace", ex.StackTrace},
                    {"key", key}
                });
        }

        /// <summary>
        /// 安全调用 Waiting
        /// Waiting 属于辅助通知
        /// 不允许影响主流程
        /// </summary>
        private void SafeInvokeWaiting(
            Action waiting,
            string key)
        {
            try
            {
                waiting?.Invoke();
            }
            catch (Exception ex)
            {
                PrintError(
                    "Waiting回调异常",
                    ex,
                    key);
            }
        }

        /// <summary>
        /// 安全调用 Fail
        /// Fail异常会包装为AggregateException
        /// </summary>
        private void SafeInvokeFail(
            Action<Exception> fail,
            Exception originalException,
            string key)
        {
            try
            {
                fail?.Invoke(originalException);
            }
            catch (Exception failEx)
            {
                PrintError(
                    "Fail回调异常",
                    failEx,
                    key);

                throw new AggregateException(
                    "Redis lock execution failed",
                    originalException,
                    failEx);
            }
        }

        /// <summary>
        /// 安全释放同步锁
        /// </summary>
        private void SafeReleaseLock(
            IDatabase redis,
            string key,
            string token)
        {
            try
            {
                redis.LockRelease(
                    key,
                    token);
            }
            catch (Exception ex)
            {
                PrintError(
                    "Redis锁释放异常",
                    ex,
                    key);
            }
        }

        /// <summary>
        /// 安全释放异步锁
        /// </summary>
        private async Task SafeReleaseLockAsync(
            IDatabase redis,
            string key,
            string token)
        {
            try
            {
                await redis.LockReleaseAsync(
                    key,
                    token);
            }
            catch (Exception ex)
            {
                PrintError(
                    "Redis锁释放异常",
                    ex,
                    key);
            }
        }

        #endregion

        #region 同步版本

        public void TryExecuteWithLock(
            string key,
            LockAction action,
            TimeSpan waitTimeout,
            int stepWaitMilliseconds = 200,
            int lockMilliseconds = 30000)
        {
            var redis =
                AppRealization.RedisCache.GetDatabase()
                as IDatabase;

            if (redis == null)
            {
                SafeInvokeFail(
                    action.Fail,
                    new Exception("Redis database is null"),
                    key);

                return;
            }

            NormalizeArguments(
                ref stepWaitMilliseconds,
                ref lockMilliseconds);

            // 每次加锁唯一token
            var token = Guid.NewGuid().ToString("N");

            // 当前实例是否持有锁
            var hasLock = false;

            // 是否成功执行
            var successExecuted = false;

            // 真实异常
            Exception realException = null;

            try
            {
                int totalWaitCounter =
                    GetCounter(
                        waitTimeout,
                        stepWaitMilliseconds);

                int nowWaitCounter = 0;

                DateTime lastWaitingNotify =
                    DateTime.MinValue;

                while (nowWaitCounter < totalWaitCounter)
                {
                    nowWaitCounter++;

                    bool acquired =
                        redis.LockTake(
                            key,
                            token,
                            TimeSpan.FromMilliseconds(
                                lockMilliseconds));

                    if (acquired)
                    {
                        hasLock = true;

                        try
                        {
                            action.Success?.Invoke();

                            successExecuted = true;

                            return;
                        }
                        catch (Exception ex)
                        {
                            realException = ex;

                            PrintError(
                                "加锁执行方法异常",
                                ex,
                                key);

                            break;
                        }
                    }

                    // Waiting回调节流
                    if ((DateTime.Now - lastWaitingNotify)
                        .TotalSeconds >= 1)
                    {
                        SafeInvokeWaiting(
                            action.Waiting,
                            key);

                        lastWaitingNotify = DateTime.Now;
                    }

                    // 线性退避
                    var delay =
                        Math.Min(
                            stepWaitMilliseconds *
                            nowWaitCounter,
                            1000);

                    Thread.Sleep(delay);
                }

                // 未成功执行
                if (!successExecuted)
                {
                    realException ??=
                        new Exception(
                            $"Failed to acquire lock, key: {key}");

                    SafeInvokeFail(
                        action.Fail,
                        realException,
                        key);
                }
            }
            catch (Exception ex)
            {
                PrintError(
                    "Redis锁系统异常",
                    ex,
                    key);

                SafeInvokeFail(
                    action.Fail,
                    ex,
                    key);
            }
            finally
            {
                // 仅当前实例持有锁时释放
                if (hasLock)
                {
                    SafeReleaseLock(
                        redis,
                        key,
                        token);
                }
            }
        }

        #endregion

        #region 异步版本

        public async Task TryExecuteWithLockAsync(
            string key,
            LockAsyncAction action,
            TimeSpan waitTimeout,
            int stepWaitMilliseconds = 200,
            int lockMilliseconds = 30000)
        {
            var redis =
                AppRealization.RedisCache.GetDatabase()
                as IDatabase;

            if (redis == null)
            {
                SafeInvokeFail(
                    action.Fail,
                    new Exception("Redis database is null"),
                    key);

                return;
            }

            NormalizeArguments(
                ref stepWaitMilliseconds,
                ref lockMilliseconds);

            // 每次加锁唯一token
            var token = Guid.NewGuid().ToString("N");

            // 当前实例是否持有锁
            var hasLock = false;

            // 是否成功执行
            var successExecuted = false;

            // 真实异常
            Exception realException = null;

            try
            {
                int totalWaitCounter =
                    GetCounter(
                        waitTimeout,
                        stepWaitMilliseconds);

                int nowWaitCounter = 0;

                DateTime lastWaitingNotify =
                    DateTime.MinValue;

                while (nowWaitCounter < totalWaitCounter)
                {
                    nowWaitCounter++;

                    bool acquired =
                        await redis.LockTakeAsync(
                            key,
                            token,
                            TimeSpan.FromMilliseconds(
                                lockMilliseconds));

                    if (acquired)
                    {
                        hasLock = true;

                        try
                        {
                            if (action.Success != null)
                            {
                                await action.Success();
                            }

                            successExecuted = true;

                            return;
                        }
                        catch (Exception ex)
                        {
                            realException = ex;

                            PrintError(
                                "加锁执行方法异常",
                                ex,
                                key);

                            break;
                        }
                    }

                    // Waiting回调节流
                    if ((DateTime.Now - lastWaitingNotify)
                        .TotalSeconds >= 1)
                    {
                        SafeInvokeWaiting(
                            action.Waiting,
                            key);

                        lastWaitingNotify = DateTime.Now;
                    }

                    // 线性退避
                    var delay =
                        Math.Min(
                            stepWaitMilliseconds *
                            nowWaitCounter,
                            1000);

                    await Task.Delay(delay);
                }

                // 未成功执行
                if (!successExecuted)
                {
                    realException ??=
                        new Exception(
                            $"Failed to acquire lock, key: {key}");

                    SafeInvokeFail(
                        action.Fail,
                        realException,
                        key);
                }
            }
            catch (Exception ex)
            {
                PrintError(
                    "Redis锁系统异常",
                    ex,
                    key);

                SafeInvokeFail(
                    action.Fail,
                    ex,
                    key);
            }
            finally
            {
                // 仅当前实例持有锁时释放
                if (hasLock)
                {
                    await SafeReleaseLockAsync(
                        redis,
                        key,
                        token);
                }
            }
        }

        #endregion

        #region 简化同步调用

        public void TryExecuteWithLock(
            string key,
            Action action,
            TimeSpan waitTimeout,
            int stepWaitMilliseconds = 200,
            int lockMilliseconds = 30000)
        {
            TryExecuteWithLock(
                key,
                new LockAction()
                {
                    Success = action,
                    Fail = (ex) => { },
                    Waiting = () => { }
                },
                waitTimeout,
                stepWaitMilliseconds,
                lockMilliseconds);
        }

        #endregion

        #region 简化异步调用

        public async Task TryExecuteWithLockAsync(
            string key,
            Func<Task> action,
            TimeSpan waitTimeout,
            int stepWaitMilliseconds = 200,
            int lockMilliseconds = 30000)
        {
            await TryExecuteWithLockAsync(
                key,
                new LockAsyncAction()
                {
                    Success = action,
                    Fail = (ex) => { },
                    Waiting = () => { }
                },
                waitTimeout,
                stepWaitMilliseconds,
                lockMilliseconds);
        }

        #endregion
    }
}