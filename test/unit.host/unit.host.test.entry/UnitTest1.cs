using Air.Cloud.Core.Standard;
using Air.Cloud.Core.Standard.Cache.Redis;
using Air.Cloud.Modules.RedisCache.Dependencies;
using Air.Cloud.Modules.RedisCache.Extensions;

namespace unit.host.test.entry
{
    public class UnitTest1
    {

        /// <summary>
        /// ª∫¥Êkey get set≤‚ ‘
        /// </summary>
        [Fact]
        public void KeyGetSetTest()
        {
            //RedisCacheModuleExtensions.HostRedisCacheInject<RedisCacheDependency>();

            AppStandardRealization.AppCacheStandard.SetCache("123","456");

            Assert.Equal("456",AppStandardRealization.AppCacheStandard.GetCache("123"));
        }
        /// <summary>
        /// ª∫¥Êkey ≥¨ ±≤‚ ‘
        /// </summary>
        [Fact]
        public void KeyTimeOutTest()
        {
            //RedisCacheModuleExtensions.HostRedisCacheInject<RedisCacheDependency>();

            AppStandardRealization.AppCacheStandard.SetCache("123", "456",new TimeSpan(0,0,2));

            Thread.Sleep(3000);

            Assert.Equal("456", AppStandardRealization.AppCacheStandard.GetCache("123"));
        }
        /// <summary>
        /// Redis ‘º∂®◊¢»Î≤‚ ‘
        /// </summary>
        [Fact]
        public void KeyHeigherLevelDependencTest()
        {
            //RedisCacheModuleExtensions.HostRedisCacheInject<RedisCacheDependency>();

            IRedisCacheStandard redis = AppStandardRealization.RedisCacheStandard;

            redis.String.Set("String123", "456");

            Assert.Equal("456",redis.String.Get("String123"));

            redis.Hash.Set("Hash123", "456","789");

            Assert.Equal("789", redis.Hash.Get<string>("Hash123", "456"));

            redis.Set.Add("Set123","123");

            Assert.Equal("123", redis.Set.Elements<string>("Set123").FirstOrDefault());
        }
    }
}