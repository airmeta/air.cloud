using Air.Cloud.Core.Standard.Cache.Redis;

using StackExchange.Redis;

namespace Air.Cloud.Modules.RedisCache.Extensions
{
    public  class RedisLockHandle : IDisposable
    {
        private readonly string _lockKey;
        private readonly IDatabase _redis;
        private readonly RedisValue _token;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="timeSpan">超时时间</param>
        /// <param name="lockKey">锁建</param>
        /// <param name="count">重试次数</param>
        /// <exception cref="Exception"></exception>
        public RedisLockHandle(TimeSpan timeSpan, string lockKey, int count = 300, int step = 10) : this(AppRealization.RedisCache, timeSpan, lockKey, count, step)
        {

        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="redisCaches"></param>
        /// <param name="timeSpan">超时时间</param>
        /// <param name="lockKey">锁建</param>
        /// <param name="count">重试次数</param>
        /// <exception cref="Exception"></exception>
        public RedisLockHandle(IRedisCacheStandard redisCaches, TimeSpan timeSpan, string lockKey, int count = 300, int step = 10) : this(redisCaches.GetDatabase() as IDatabase, timeSpan, lockKey, count, step)
        {

        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="redis"></param>
        /// <param name="timeSpan">超时时间</param>
        /// <param name="lockKey">锁建</param>
        /// <param name="count">重试次数</param>
        /// <exception cref="Exception"></exception>
        public RedisLockHandle(IDatabase redis, TimeSpan timeSpan, string lockKey, int count = 300, int step = 10)
        {
            _lockKey = lockKey;
            _token = Environment.MachineName;
            _redis = redis;

            var counter = 0;
            var executed = false;
            if (_redis.LockTake(lockKey, _token, timeSpan))
            {
                return;
            }
            else
            {
                while (!executed && counter < count)
                {
                    counter += 1;
                    Thread.Sleep(step);
                    if (_redis.LockTake(lockKey, _token, timeSpan))
                    {
                        executed = true;
                    }
                }
                if (!executed)
                {
                    throw new Exception("系统繁忙,请稍后再试!");
                }
            }
        }
        /// <summary>
        /// 是否释放
        /// </summary>
        private bool isDispose = false;
        public void Dispose()
        {
            if (!isDispose)
            {
                try
                {
                    _redis.LockRelease(_lockKey, _token);
                    isDispose = true;
                }
                finally
                {

                }
            }
        }
    }
}
