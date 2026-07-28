/*
 * Copyright (c) 2024-2030 星曳数据
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 *
 * This file is provided under the Mozilla Public License Version 2.0,
 * and the "NO WARRANTY" clause of the MPL is hereby expressly
 * acknowledged.
 */
using Air.Cloud.Core.Standard.Cache.Redis;
using StackExchange.Redis;
using Air.Cloud.Core;
using Air.Cloud.WebApp.FriendlyException;

namespace air.cloud.security.common.Extensions
{
    /// <summary>
    /// redis阻塞锁
    /// </summary>
    public class RedisLockHandler : IDisposable
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
        public RedisLockHandler(TimeSpan timeSpan, string lockKey, int count = 300, int step = 10) : this(AppRealization.RedisCache, timeSpan, lockKey, count, step)
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
        public RedisLockHandler(IRedisCacheStandard redisCaches, TimeSpan timeSpan, string lockKey, int count = 300, int step = 10) : this(redisCaches.GetDatabase() as IDatabase, timeSpan, lockKey, count, step)
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
        public RedisLockHandler(IDatabase redis, TimeSpan timeSpan, string lockKey, int count = 300,int step=10)
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
                    throw Oops.Oh("系统繁忙,请稍后再试!");
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
