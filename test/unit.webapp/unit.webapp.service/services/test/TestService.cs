
/*
 * Copyright (c) 2024 星曳数据
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
using Air.Cloud.Core.Dependencies;
using Air.Cloud.Core.Standard.Cache.Redis;
using Air.Cloud.Core.Standard.DynamicServer;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using unit.webapp.model.Domains;

namespace unit.webapp.service.services.test
{
    [Route("api")]
    public class TestService : IDynamicService, ITransient
    {
        public ITestDomain Domain;
        public TestService(ITestDomain testDomain)
        {
            Domain = testDomain;
        }

        [HttpGet("test")]
        [AllowAnonymous]
        public object Test()
        {
            return new { name = "132" };
        }

        [HttpGet("search")]
        public object Get()
        {
            return Domain.Search(s => s.UserId == "a09cdb089b7f48498090d1f7f11c0e7b");
        }
        [HttpGet("redis")]
        [AllowAnonymous]
        public object RedisCacheTest()
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

            return "";
        }

        #region 测试服务
        [HttpPost("TESTSSS")]
        public object Send(Testss ss)
        {
            return ss;
        }
        #endregion
    }
    public class Testss
    {
        public int MyProperty { get; set; }
        public string Mo { get; set; }
    }
}
