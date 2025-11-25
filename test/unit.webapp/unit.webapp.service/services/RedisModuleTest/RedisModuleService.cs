
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

using Air.Cloud.Core;
using Air.Cloud.Core.Standard.Cache.Redis;
using Air.Cloud.Core.Standard.DistributedLock.Attributes;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace unit.webapp.service.services.RedisModuleTest
{
    [Route("api/cache")]
    public  class RedisModuleService:IRedisModuleService
    {
        [HttpGet("redis")]
        [AllowAnonymous]
        public bool RedisCacheTest()
        {
            IRedisCacheStandard redis = AppRealization.RedisCache;
            redis.Key.Fulsh();
            AppRealization.Cache.SetCache("123", "456");
            string Value1 = AppRealization.Cache.GetCache("123");

            AppRealization.Cache.SetCache("1234", "456", new TimeSpan(0, 0, 2));

            Thread.Sleep(3000);
            string Value2 = AppRealization.Cache.GetCache("1234");


            redis.String.Set("String123", "456");

            string Value3 = redis.String.Get("String123");

            redis.Hash.Set("Hash123", "456", "789");

            string Value4 = redis.Hash.Get<string>("Hash123", "456");

            redis.Set.Add("Set123", "123");

            string? Value5 = redis.Set.Elements<string>("Set123").FirstOrDefault();

            return true;
        }

        /// <summary>
        /// 加锁测试
        /// </summary>
        /// <returns></returns>
        [HttpGet("lock")]
        [Authorize("123312")]
        //[AllowAnonymous]
        [DistributedLock(3000,FailMessage = "拿锁失败",LockKey ="Lock")]
        public bool RedisCacheTest1()
        {
            AppRealization.Cache.SetCache("123", "456");
            string Value1 = AppRealization.Cache.GetCache("123");
            //Thread.Sleep(10000);
            return true;
        }

        /// <summary>
        /// 加锁测试
        /// </summary>
        /// <param name="Key"></param>
        /// <returns></returns>
        [HttpPost("lock")]
        [AllowAnonymous]
        [DistributedLock(3000, FailMessage = "拿锁失败")]
        public bool RedisCacheTest1(object Key)
        {
            AppRealization.Cache.SetCache("123", "456");
            string Value1 = AppRealization.Cache.GetCache("123");
            Thread.Sleep(10000);
            return true;
        }
    }
}
