using Air.Cloud.Core.Standard;
using Air.Cloud.Core.Standard.Cache.Redis;
using Air.Cloud.Modules.RedisCache.Provider;
using Newtonsoft.Json;

using SSS.Modules.Redis.Service;

using StackExchange.Redis;

using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Xml.Linq;

namespace SSS.Modules.Redis
{

    /// <summary>
    /// Redis操作方法基础类
    /// </summary>
    public class RedisCacheKeyDependency : IRedisCacheKeyStandard
    {
        /// <summary>
        /// Redis操作对象
        /// </summary>
        private  readonly IDatabase Redis;
        public RedisCacheKeyDependency(IDatabase Redis)
        {
            this.Redis = Redis;
        }
        /// <summary>
        /// 获取Redis事务对象
        /// </summary>
        /// <returns></returns>
        public ITransaction CreateTransaction() => Redis.CreateTransaction();

        /// <summary>
        /// 获取Redis服务和常用操作对象
        /// </summary>
        /// <returns></returns>
        public IDatabase GetDatabase() => Redis;

        /// <summary>
        /// 获取Redis服务
        /// </summary>
        /// <param name="hostAndPort"></param>
        /// <returns></returns>
        public IServer GetServer(string hostAndPort) => RedisConnectorProvider.Instance.GetServer(hostAndPort);

        /// <summary>
        /// 执行Redis事务
        /// </summary>
        /// <param name="act"></param>
        /// <returns></returns>
        public bool RedisTransaction(Action<ITransaction> act)
        {
            var tran = Redis.CreateTransaction();
            act.Invoke(tran);
            bool committed = tran.Execute();
            return committed;
        }

        /// <summary>
        /// Redis阻塞锁
        /// </summary>
        /// <remarks>阻塞锁</remarks>
        /// <param name="act">执行的委托</param>
        /// <param name="ts">锁住时间</param>
        /// <returns>返回一个bool类型，指明委托是否被执行到了</returns>
        public async Task<bool> BlockLockTake(Func<Task> act, TimeSpan ts, string key, int count = 300)
        {
            RedisValue token = Environment.MachineName;
            string lockKey = key;
            var counter = 0;
            //是否成功执行委托
            var executed = false;
            if (Redis.LockTake(lockKey, token, ts))
            {
                try
                {
                    await act();
                    executed = true;
                }
                finally
                {
                    Redis.LockRelease(lockKey, token);
                }
            }
            else
            {
                while (!executed && counter < count)
                {
                    counter += 1;
                    Thread.Sleep(10);
                    if (Redis.LockTake(lockKey, token, ts))
                    {
                        try
                        {
                            var s = new Stopwatch();
                            s.Start();
                            await act();
                            s.Stop();
                            executed = true;
                        }
                        finally
                        {
                            Redis.LockRelease(lockKey, token);
                        }
                    }
                }
                if (!executed)
                {
                    throw new Exception("系统繁忙,请稍后再试!");
                }
            }
            return executed;
        }

        #region 常用Key操作

        #region 同步方法

        /// <summary>
        /// 删除单个key
        /// </summary>
        /// <param name="key">要删除的key</param>
        /// <returns>是否删除成功</returns>
        public bool Delete(string key) => Redis.KeyDelete(key);
        /// <summary>
        /// 删除多个key
        /// </summary>
        /// <param name="keys">要删除的key集合</param>
        /// <returns>成功删除的个数</returns>
        public long Delete(params string[] keys) => Redis.KeyDelete(keys.Select(o => (RedisKey)o).ToArray());

        /// <summary>
        /// 清空当前DataBase中所有Key
        /// </summary>
        public void Fulsh() => Redis.Execute("FLUSHDB");
        /// <summary>
        /// 判断key是否存在
        /// </summary>
        /// <param name="key">要判断的key</param>
        /// <returns></returns>
        public bool Exists(string key) => Redis.KeyExists(key);

        /// <summary>
        /// 重新命名key
        /// </summary>
        /// <param name="key">就的redis key</param>
        /// <param name="newKey">新的redis key</param>
        /// <returns></returns>
        public bool Rename(string key, string newKey)=> Redis.KeyRename(key, newKey);

        /// <summary>
        /// 设置Key的过期时间
        /// </summary>
        /// <param name="key">RedisCache key</param>
        /// <param name="expiry">过期时间</param>
        /// <returns></returns>
        public bool Expire(string key, TimeSpan? expiry = default)=> Redis.KeyExpire(key, expiry);
        #endregion 同步方法

        #region 异步方法

        /// <summary>
        /// 删除单个key
        /// </summary>
        /// <param name="key">要删除的key</param>
        /// <returns>是否删除成功</returns>
        public async Task<bool> DeleteAsync(string key)=> await Redis.KeyDeleteAsync(key);
        /// <summary>
        /// 删除多个key
        /// </summary>
        /// <param name="keys">要删除的key集合</param>
        /// <returns>成功删除的个数</returns>
        public async Task<long> DeleteAsync(params string[] keys)=> await Redis.KeyDeleteAsync(keys.Select(o => (RedisKey)o).ToArray());

        /// <summary>
        /// 清空当前DataBase中所有Key
        /// </summary>
        public async Task FulshAsync() => await Redis.ExecuteAsync("FLUSHDB");
        /// <summary>
        /// 判断key是否存在
        /// </summary>
        /// <param name="key">要判断的key</param>
        /// <returns></returns>
        public async Task<bool> ExistsAsync(string key)=> await Redis.KeyExistsAsync(key);
        /// <summary>
        /// 重新命名key
        /// </summary>
        /// <param name="key">就的redis key</param>
        /// <param name="newKey">新的redis key</param>
        /// <returns></returns>
        public async Task<bool> RenameAsync(string key, string newKey)=> await Redis.KeyRenameAsync(key, newKey);
        /// <summary>
        /// 设置Key的过期时间
        /// </summary>
        /// <param name="key">RedisCache key</param>
        /// <param name="expiry">过期时间</param>
        /// <returns></returns>
        public async Task<bool> ExpireAsync(string key, TimeSpan? expiry = default) => await Redis.KeyExpireAsync(key, expiry);
        #endregion 异步方法

        #endregion 常用Key操作
    }
}