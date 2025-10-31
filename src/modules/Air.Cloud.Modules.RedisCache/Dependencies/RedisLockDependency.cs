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
        private int GetCounter(TimeSpan timeout,int StepWaitMilliseconds=200)
        {
            if (timeout.TotalMilliseconds % StepWaitMilliseconds == 0)
            {
                return  (int)(timeout.TotalMilliseconds / StepWaitMilliseconds);
            }
            else
            {
                return  (int)(timeout.TotalMilliseconds / StepWaitMilliseconds) + 1;
            }
        }

        public bool TryExecuteWithLock(string key, LockAction action, TimeSpan WaitMilliseconds, int StepWaitMilliseconds = 200, int LockMilliseconds = 30000)
        {
            var _redis = AppRealization.RedisCache.GetDatabase() as IDatabase;
            try
            {
                if (_redis.LockTake(key, AppRealization.PID.Get(), new TimeSpan(0,0,0,0, LockMilliseconds)))
                {
                    action.Success.Invoke();
                    _redis.LockRelease(key, AppRealization.PID.Get());
                    return true;
                }
                else
                {
                    int TotalWaitcounter = GetCounter(WaitMilliseconds, StepWaitMilliseconds);
                    var NowWaitcounter = 0;
                    var executed = false;
                    while (!executed && NowWaitcounter < TotalWaitcounter)
                    {
                        NowWaitcounter += 1;
                        Thread.Sleep(StepWaitMilliseconds);
                        if (_redis.LockTake(key, AppRealization.PID.Get(), new TimeSpan(0, 0, 0, 0, LockMilliseconds)))
                        {
                            try
                            {
                                action.Success.Invoke();
                                _redis.LockRelease(key, AppRealization.PID.Get());
                                executed = true;
                            }
                            catch (Exception ex)
                            {
                                AppRealization.Output.Print("系统锁通知", "在加锁执行方法的时候出现异常,异常内容:" + ex.Message,
                                       AppPrintLevel.Error,
                                       AdditionalParams: new Dictionary<string, object>()
                                       {
                                {"message",ex.Message },
                                {"StackTrace",ex.StackTrace }
                                       }
                                   );
                                executed = false;
                                break;
                            }
                        }
                        else
                        {
                            action.Waiting?.Invoke();
                        }
                    }
                    if (!executed)
                    {
                        action.Fail?.Invoke();
                        return false;
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                AppRealization.Output.Print("系统锁通知", "在加锁执行方法的时候出现异常,异常内容:" + ex.Message,
                       AppPrintLevel.Error,
                       AdditionalParams: new Dictionary<string, object>()
                       {
                                {"message",ex.Message },
                                {"StackTrace",ex.StackTrace }
                       }
                   );
                action.Fail?.Invoke();
                return false;
            }
        }
        public async  Task<bool> TryExecuteWithLockAsync(string key, LockAsyncAction action, TimeSpan WaitMilliseconds, int StepWaitMilliseconds = 200, int LockMilliseconds = 30000)
        {
            var _redis = AppRealization.RedisCache.GetDatabase() as IDatabase;
            try
            {
                if (await _redis.LockTakeAsync(key, AppRealization.PID.Get(),  new TimeSpan(0, 0, 0, 0, LockMilliseconds)))
                {
                    try
                    {
                        var Ac = action.Success;
                        await Ac();
                        await _redis.LockReleaseAsync(key, AppRealization.PID.Get());
                        return true;
                    }
                    catch (Exception ex)
                    {
                        AppRealization.Output.Print("系统锁通知", "在加锁执行方法的时候出现异常,异常内容:" + ex.Message,
                               AppPrintLevel.Error,
                               AdditionalParams: new Dictionary<string, object>()
                               {
                                {"message",ex.Message },
                                {"StackTrace",ex.StackTrace }
                               }
                           );
                        action.Fail?.Invoke();
                        return false;
                    }
                }
                else
                {
                    int TotalWaitcounter = GetCounter(WaitMilliseconds, StepWaitMilliseconds);
                    var NowWaitcounter = 0;
                    var executed = false;
                    while (!executed && NowWaitcounter < TotalWaitcounter)
                    {
                        NowWaitcounter += 1;
                        Thread.Sleep(StepWaitMilliseconds);
                        if (await _redis.LockTakeAsync(key, AppRealization.PID.Get(), new TimeSpan(0, 0, 0, 0, LockMilliseconds)))
                        {
                            try
                            {
                                var Ac = action.Success;
                                await Ac();
                                await _redis.LockReleaseAsync(key, AppRealization.PID.Get());
                                return true;
                            }
                            catch (Exception ex)
                            {
                                AppRealization.Output.Print("系统锁通知", "在加锁执行方法的时候出现异常,异常内容:" + ex.Message,
                                       AppPrintLevel.Error,
                                       AdditionalParams: new Dictionary<string, object>()
                                       {
                                {"message",ex.Message },
                                {"StackTrace",ex.StackTrace }
                                       }
                                   );
                                executed = false;
                                break;
                            }
                        }
                        else
                        {
                            action.Waiting?.Invoke();
                        }
                    }
                    if (!executed)
                    {
                        action.Fail?.Invoke();
                        return false;
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                AppRealization.Output.Print("系统锁通知", "在加锁执行方法的时候出现异常,异常内容:" + ex.Message,
                      AppPrintLevel.Error,
                      AdditionalParams: new Dictionary<string, object>()
                      {
                                {"message",ex.Message },
                                {"StackTrace",ex.StackTrace }
                      }
                  );
                action.Fail?.Invoke();
                return false;
            }
        }

        public bool TryExecuteWithLock(string key, Action action, TimeSpan WaitMilliseconds, int StepWaitMilliseconds = 200, int LockMilliseconds = 30000)
        {
            return TryExecuteWithLock(key, new LockAction()
            {
                Success = action,
                Fail = () => { },
                Waiting = () => { }
            }, WaitMilliseconds, StepWaitMilliseconds,LockMilliseconds);
        }

        public async Task<bool> TryExecuteWithLockAsync(string key, Func<Task> action, TimeSpan WaitMilliseconds, int StepWaitMilliseconds = 200, int LockMilliseconds = 30000)
        {
           return await TryExecuteWithLockAsync(key, new LockAsyncAction()
            {
                Success = action,
                Fail = () => { },
                Waiting = () => { }
            }, WaitMilliseconds, StepWaitMilliseconds, LockMilliseconds);
        }
    }
}
