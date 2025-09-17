using Air.Cloud.Core;
using Air.Cloud.Core.App;
using Air.Cloud.Core.Extensions;
using Air.Cloud.Core.Standard.Cache.Redis;
using Air.Cloud.Modules.RedisCache.Dependencies;
using Air.Cloud.Modules.RedisCache.Internal;
using Air.Cloud.Modules.RedisCache.Options;

namespace Air.Cloud.UnitTest.Modules.Redis
{
    public  class RedisCacheTest
    {

        [Fact]
        public void TestKeyFlush()
        {
            IRedisCacheStandard redis = AppRealization.RedisCache;
            redis.Key.Fulsh();
        }

        [Fact]
        public void TestKeyGetSet()
        {
            AppRealization.Cache.SetCache("123", "456");
            string Value1 = AppRealization.Cache.GetCache("123");

            Assert.Equal("456", Value1);
        }

        [Fact]
        public void TestKeyStoreTime()
        {
            AppRealization.Cache.SetCache("1234", "456", new TimeSpan(0, 0, 2));
            Thread.Sleep(3000);
            string Value2 = AppRealization.Cache.GetCache("1234");
            Assert.True(Value2.IsNullOrEmpty());
        }


        [Fact]
        public void TestConnectRedisByUserNameAndPassword()
        {
            RedisSettingsOptions redisSettingsOptions = new RedisSettingsOptions()
            {
                ConnectionString = "192.168.100.158:6380,allowadmin=true",
                UserName = "default",
                Password = "szfc@001!xc"
            };

            RedisCacheDependency redisCacheDependency = new RedisCacheDependency(redisSettingsOptions,false,0);
            redisCacheDependency.SetCache("123", "456");
            string Value1 = redisCacheDependency.GetCache("123");
            Assert.Equal("456", Value1);
        }

    }
}
