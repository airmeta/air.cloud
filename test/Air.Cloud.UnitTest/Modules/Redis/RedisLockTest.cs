using Air.Cloud.Core;
using Air.Cloud.Core.Standard.Cache.Redis;
using Air.Cloud.Core.Standard.DistributedLock;

using System.Threading.Tasks;

namespace Air.Cloud.UnitTest.Modules.Redis
{
    public class RedisLockTest
    {

        [Fact]
        public async Task TestKeyFlush()
        {

            //如果想要直观感受 则使用webapi服务或者控制台程序去运行

            string Key = "test1";

            TimeSpan LockTime = new TimeSpan(0, 0, 10);
            var T1 = Task.Run(() =>
            {
                AppRealization.Lock.TryExecuteWithLock(Key, new LockAction()
                {
                    Success = () =>
                    {
                        Console.WriteLine("T1拿到锁了,开始阻塞后续的执行");
                        Thread.Sleep(6000);
                    },
                    Fail = () =>
                    {
                        Console.WriteLine("T1没拿到锁,直接返回");
                    },
                    Waiting = () =>
                    {
                        Console.WriteLine("T1等待拿锁中");
                    }
                }, LockTime, 200);
            });
            var T2 = Task.Run(() =>
            {
                AppRealization.Lock.TryExecuteWithLock(Key, new LockAction()
                {
                    Success = () =>
                    {
                        Console.WriteLine("T2拿到锁了,开始阻塞后续的执行");
                        Thread.Sleep(6000);
                    },
                    Fail = () =>
                    {
                        Console.WriteLine("T2没拿到锁,直接返回");
                    },
                    Waiting = () =>
                    {
                        Console.WriteLine("T2等待拿锁中");
                    }
                }, LockTime);
            });

            var T3 = Task.Run(() =>
            {
                AppRealization.Lock.TryExecuteWithLock(Key, new LockAction()
                {
                    Success = () =>
                    {
                        Console.WriteLine("T3拿到锁了,开始阻塞后续的执行");
                        Thread.Sleep(6000);
                    },
                    Fail = () =>
                    {
                        Console.WriteLine("T3没拿到锁,直接返回");
                    },
                    Waiting = () =>
                    {
                        Console.WriteLine("T3等待拿锁中");
                    }
                }, LockTime);
            });

            await Task.WhenAll(T1, T2, T3);

        }

        public async Task TestKeyFlushAsync()
        {
            string Key = "test1";

            TimeSpan LockTime = new TimeSpan(0, 0, 10);
            var T1 = Task.Run(async () =>
            {
                var IsExecuteSuccess = await AppRealization.Lock.TryExecuteWithLockAsync(Key, new LockAsyncAction()
                {
                    Success = async () =>
                    {
                        await AppRealization.RedisCache.String.SetAsync("test_t1", "T1", new TimeSpan(0, 1, 0));
                        Console.WriteLine("T1拿到锁了,开始阻塞后续的执行");
                        Thread.Sleep(6000);
                    },
                    Fail = () =>
                    {
                        Console.WriteLine("T1没拿到锁,直接返回");
                    },
                    Waiting = () =>
                    {
                        Console.WriteLine("T1等待拿锁中");
                    }
                }, LockTime, 200);
            });
            var T2 = Task.Run(async () =>
            {
                var IsExecuteSuccess = await AppRealization.Lock.TryExecuteWithLockAsync(Key, new LockAsyncAction()
                {
                    Success = async () =>
                    {
                        await AppRealization.RedisCache.String.SetAsync("test_t2", "T2", new TimeSpan(0, 1, 0));
                        Console.WriteLine("T2拿到锁了,开始阻塞后续的执行");
                        Thread.Sleep(6000);
                    },
                    Fail = () =>
                    {
                        Console.WriteLine("T2没拿到锁,直接返回");
                    },
                    Waiting = () =>
                    {
                        Console.WriteLine("T2等待拿锁中");
                    }
                }, LockTime);
            });

            var T3 = Task.Run(async () =>
            {
                var IsExecuteSuccess = await AppRealization.Lock.TryExecuteWithLockAsync(Key, new LockAsyncAction()
                {
                    Success = async () =>
                    {
                        await AppRealization.RedisCache.String.SetAsync("test_t3", "T3", new TimeSpan(0, 1, 0));
                        Console.WriteLine("T3拿到锁了,开始阻塞后续的执行");
                        Thread.Sleep(6000);
                    },
                    Fail = () =>
                    {
                        Console.WriteLine("T3没拿到锁,直接返回");
                    },
                    Waiting = () =>
                    {
                        Console.WriteLine("T3等待拿锁中");
                    }
                }, LockTime);
            });

            await Task.WhenAll(T1, T2, T3);
        }
    }
}
