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
    /// <para>zh-cn:Redis 缓存能力测试集合。</para>
    /// <para>en-us:Test suite for Redis cache capabilities.</para>
    /// </summary>
    public class RedisCacheTest
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
        /// <para>zh-cn:测试 Redis 键空间清理场景，确认执行 Flush 不会抛出异常。</para>
        /// <para>en-us:Tests Redis key-space cleanup behavior to ensure Flush executes without exception.</para>
        /// </summary>
        /// <remarks>
        /// <para>zh-cn:测试过程：获取 Redis 客户端后直接执行 Key.Fulsh，验证调用链可正常完成。</para>
        /// <para>en-us:Process: obtain Redis client and execute Key.Fulsh directly, verifying invocation completes successfully.</para>
        /// </remarks>
        [Fact]
        public void Flush_should_execute_without_throwing()
        {
            IRedisCacheStandard redis = AppRealization.RedisCache;

            redis.Key.Fulsh();
        }

        /// <summary>
        /// <para>zh-cn:测试缓存写后读场景，确认 SetCache 后可读取到非空字符串结果。</para>
        /// <para>en-us:Tests write-then-read cache behavior to ensure SetCache returns a non-empty value when read back.</para>
        /// </summary>
        /// <remarks>
        /// <para>zh-cn:测试过程：写入固定 key/value 后读取同一 key，断言读取结果非空。</para>
        /// <para>en-us:Process: write fixed key/value, read same key, and assert returned value is non-empty.</para>
        /// </remarks>
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
        /// <para>zh-cn:测试 TTL 过期场景，确认带过期时间的缓存在超时后不可读。</para>
        /// <para>en-us:Tests TTL expiration behavior to ensure expiring cache entries become unavailable after timeout.</para>
        /// </summary>
        /// <remarks>
        /// <para>zh-cn:测试过程：写入 2 秒 TTL 的键值后等待 3 秒再读取，断言返回空值。</para>
        /// <para>en-us:Process: set a 2-second TTL key, wait 3 seconds, read it, and assert empty result.</para>
        /// </remarks>
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
        /// <para>zh-cn:测试认证连接读写场景，确认提供认证配置后可正常写入并读取 Redis 值。</para>
        /// <para>en-us:Tests authenticated-connection read/write to ensure values can be set and retrieved when auth config is present.</para>
        /// </summary>
        /// <remarks>
        /// <para>zh-cn:测试过程：读取认证配置创建 Redis 依赖并执行 set/get，若未配置则按用例约定直接返回。</para>
        /// <para>en-us:Process: load auth configuration, create Redis dependency, execute set/get; return early when configuration is absent by test design.</para>
        /// </remarks>
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
        /// <para>zh-cn:从配置系统读取指定名称的 Redis 配置。</para>
        /// <para>en-us:Reads the Redis settings for the specified section name from configuration.</para>
        /// </summary>
        /// <param name="sectionName">
        /// <para>zh-cn:目标 Redis 配置节名称。</para>
        /// <para>en-us:The name of the target Redis configuration section.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:返回有效 Redis 配置；若不存在或无效则返回 null。</para>
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
