using Air.Cloud.Core.Standard;
using Air.Cloud.Core.Standard.Cache.Redis;
using Air.Cloud.Core.Standard.Dependencies;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using unit.webapp.model.Domains;

namespace unit.webapp.service.services.test
{
    [Route("api")]
    public class TestService : IDynamicApiController, ITransient
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
            IRedisCacheStandard redis = AppStandardRealization.RedisCacheStandard;
            redis.Key.Fulsh();
            AppStandardRealization.AppCacheStandard.SetCache("123", "456");
            string Value1 = AppStandardRealization.AppCacheStandard.GetCache("123");

            AppStandardRealization.AppCacheStandard.SetCache("1234", "456", new TimeSpan(0, 0, 2));

            Thread.Sleep(3000);
            string Value2 = AppStandardRealization.AppCacheStandard.GetCache("1234");


            redis.String.Set("String123", "456");

            string Value3 = redis.String.Get("String123");

            redis.Hash.Set("Hash123", "456", "789");

            string Value4 = redis.Hash.Get<string>("Hash123", "456");

            redis.Set.Add("Set123", "123");

            string? Value5 = redis.Set.Elements<string>("Set123").FirstOrDefault();

            return "";
        }
    }
}
