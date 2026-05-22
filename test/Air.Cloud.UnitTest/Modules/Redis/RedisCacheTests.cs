using Air.Cloud.Core;
using Air.Cloud.Core.App;
using Air.Cloud.Core.Extensions;
using Air.Cloud.Core.Standard.Cache.Redis;
using Air.Cloud.Modules.RedisCache.Dependencies;
using Air.Cloud.Modules.RedisCache.Options;
using Microsoft.Extensions.Configuration;

namespace Air.Cloud.UnitTest.Modules.Redis
{
    /// <summary>
    /// <para>zh-cn:Redis 缓存能力相关测试集合。</para>
    /// <para>en-us:Test suite for Redis cache capabilities.</para>
    /// </summary>
    public class RedisCacheTests
    {
        /// <summary>
        /// <para>zh-cn:默认 Redis 配置节名称。</para>
        /// <para>en-us:Configuration section name for the default Redis connection.</para>
        /// </summary>
        private const string DefaultRedisConnectionSection = "RedisSettings";

        /// <summary>
        /// <para>zh-cn:认证 Redis 配置节名称。</para>
        /// <para>en-us:Configuration section name for the authenticated Redis connection.</para>
        /// </summary>
        private const string AuthRedisConnectionSection = "RedisAuthTestSettings";

        /// <summary>
        /// <para>zh-cn:验证刷新 Redis 键空间操作能够正常执行。</para>
        /// <para>en-us:Verifies that flushing the Redis key space can execute successfully.</para>
        /// </summary>
        [Fact]
        public void Flush_should_execute_without_throwing()
        {
            IRedisCacheStandard redis = AppRealization.RedisCache;

            redis.Key.Fulsh();
        }

        /// <summary>
        /// <para>zh-cn:验证写入缓存后可以读取到非空结果。</para>
        /// <para>en-us:Verifies that writing a cache value returns a non-empty result when read back.</para>
        /// </summary>
        [Fact]
        public void SetCache_should_return_non_empty_value_when_cache_is_available()
        {
            const string cacheKey = "unit-test:redis:string";
            const string expectedValue = "456";

            AppRealization.Cache.SetCache(cacheKey, expectedValue);
            var actualValue = AppRealization.Cache.GetCache(cacheKey);

            Assert.False(actualValue.IsNullOrEmpty());
        }

        /// <summary>
        /// <para>zh-cn:验证带过期时间的缓存项会在超时后失效。</para>
        /// <para>en-us:Verifies that a cache entry with expiration becomes unavailable after the timeout.</para>
        /// </summary>
        [Fact]
        public async Task SetCache_with_expiration_should_expire_value()
        {
            const string cacheKey = "unit-test:redis:ttl";
            AppRealization.Cache.SetCache(cacheKey, "456", TimeSpan.FromSeconds(2));

            await Task.Delay(TimeSpan.FromSeconds(3));
            var actualValue = AppRealization.Cache.GetCache(cacheKey);

            Assert.True(actualValue.IsNullOrEmpty());
        }

        /// <summary>
        /// <para>zh-cn:验证 Redis String 结构写入后可以读取到相同值。</para>
        /// <para>en-us:Verifies that a Redis string value can be read back after being written.</para>
        /// </summary>
        [Fact]
        public void Redis_string_operations_should_round_trip_value()
        {
            const string cacheKey = "unit-test:redis:native:string";
            const string expectedValue = "456";
            var redis = AppRealization.RedisCache;

            redis.String.Set(cacheKey, expectedValue);
            var actualValue = redis.String.Get(cacheKey);

            Assert.Equal(expectedValue, actualValue);
        }

        /// <summary>
        /// <para>zh-cn:验证 Redis Hash 结构写入字段后可以读取到相同值。</para>
        /// <para>en-us:Verifies that a Redis hash field can be read back after being written.</para>
        /// </summary>
        [Fact]
        public void Redis_hash_operations_should_round_trip_value()
        {
            const string cacheKey = "unit-test:redis:native:hash";
            const string fieldKey = "field-456";
            const string expectedValue = "789";
            var redis = AppRealization.RedisCache;

            redis.Hash.Set(cacheKey, fieldKey, expectedValue);
            var actualValue = redis.Hash.Get<string>(cacheKey, fieldKey);

            Assert.Equal(expectedValue, actualValue);
        }

        /// <summary>
        /// <para>zh-cn:验证 Redis Set 结构添加成员后可以在成员集合中查询到该值。</para>
        /// <para>en-us:Verifies that a Redis set member can be found after it is added.</para>
        /// </summary>
        [Fact]
        public void Redis_set_operations_should_contain_added_member()
        {
            const string cacheKey = "unit-test:redis:native:set";
            const string expectedValue = "123";
            var redis = AppRealization.RedisCache;

            redis.Set.Add(cacheKey, expectedValue);
            var values = redis.Set.Elements<string>(cacheKey);

            Assert.Contains(expectedValue, values);
        }

        /// <summary>
        /// <para>zh-cn:验证使用认证配置时可以成功写入并读取 Redis 值。</para>
        /// <para>en-us:Verifies that values can be written and read when an authenticated Redis configuration is available.</para>
        /// </summary>
        [Fact]
        public void Authenticated_connection_should_set_and_get_value_when_configured()
        {
            var redisSettingsOptions = GetRedisSettings(AuthRedisConnectionSection);
            if (redisSettingsOptions == null)
            {
                return;
            }

            var redisCacheDependency = new RedisCacheDependency(redisSettingsOptions, false, 0);
            const string cacheKey = "unit-test:redis:auth";
            const string expectedValue = "456";

            redisCacheDependency.SetCache(cacheKey, expectedValue);
            var actualValue = redisCacheDependency.GetCache(cacheKey);

            Assert.Equal(expectedValue, actualValue);
        }

        /// <summary>
        /// <para>zh-cn:从配置系统中读取指定名称的 Redis 配置。</para>
        /// <para>en-us:Reads the Redis settings for the specified section name from configuration.</para>
        /// </summary>
        /// <param name="sectionName">
        /// <para>zh-cn:目标 Redis 配置节名称。</para>
        /// <para>en-us:The name of the target Redis configuration section.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:返回有效的 Redis 配置；如果不存在或无效则返回 null。</para>
        /// <para>en-us:Returns valid Redis settings, or null when the section is missing or invalid.</para>
        /// </returns>
        private static RedisSettingsOptions? GetRedisSettings(string sectionName)
        {
            var configuration = AppCore.GetService<IConfiguration>();
            var section = configuration.GetSection(sectionName);
            if (!section.Exists())
            {
                return null;
            }

            var settings = section.Get<RedisSettingsOptions>();
            if (settings == null || settings.ConnectionString.IsNullOrEmpty())
            {
                return null;
            }

            return settings;
        }
    }
}
