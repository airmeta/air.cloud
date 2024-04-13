using Air.Cloud.Core.Standard.Cache.Redis;
using Air.Cloud.Modules.RedisCache.Provider;

using StackExchange.Redis;

namespace SSS.Modules.Redis.Service
{

    /// <summary>
    /// key-value 键值对:value可以是序列化的数据
    /// </summary>
    public class StringCache : IStringCache
    {
        private readonly IDatabase Redis;
        public StringCache(IDatabase Redis)
        {
            this.Redis = Redis;
        }

        #region 同步方法
        /// <summary>
        /// 添加单个key value
        /// </summary>
        /// <param name="key">RedisCache Key</param>
        /// <param name="value">保存的值</param>
        /// <param name="expiry">过期时间</param>
        /// <returns></returns>
        public bool Set(string key, string value, TimeSpan? expiry = default) => Redis.StringSet(key, value, expiry);

        /// <summary>
        /// 添加多个key/value
        /// </summary>
        /// <param name="valueList">key/value集合</param>
        /// <returns></returns>
        public bool Set(Dictionary<string, string> valueList) => Redis.StringSet(valueList.Select(p => new KeyValuePair<RedisKey, RedisValue>(p.Key, p.Value)).ToArray());
        /// <summary>
        /// 保存一个对象
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="key">保存的Key名称</param>
        /// <param name="value">对象实体</param>
        /// <param name="expiry">过期时间</param>
        /// <returns></returns>
        public bool Set<T>(string key, T value, TimeSpan? expiry = default) => Redis.StringSet(key, RedisCacheProvider.ConvertJson(value), expiry);
        /// <summary>
        /// 获取多个key的value值
        /// </summary>
        /// <param name="keys">要获取值的Key集合</param>
        /// <returns></returns>
        public List<string> Get(params string[] keys) => Redis.StringGet(RedisCacheProvider.ConvertRedisKeys(keys)).Select(o => o.ToString()).ToList();

        /// <summary>
        /// 获取多个key的value值
        /// </summary>
        /// <typeparam name="T">返回数据类型</typeparam>
        /// <param name="keys">要获取值的Key集合</param>
        /// <returns></returns>
        public List<T> Get<T>(params string[] keys) => RedisCacheProvider.ConvetList<T>(Redis.StringGet(RedisCacheProvider.ConvertRedisKeys(keys)));
        #endregion

        #region 异步方法
        /// <summary>
        /// 异步方法 保存单个key value
        /// </summary>
        /// <param name="key">RedisCache Key</param>
        /// <param name="value">保存的值</param>
        /// <param name="expiry">过期时间</param>
        /// <returns></returns>
        public async Task<bool> SetAsync(string key, string value, TimeSpan? expiry = default) => await Redis.StringSetAsync(key, value, expiry);
        /// <summary>
        /// 异步方法 添加多个key/value
        /// </summary>
        /// <param name="valueList">key/value集合</param>
        /// <returns></returns>
        public async Task<bool> SetAsync(Dictionary<string, string> valueList) => await Redis.StringSetAsync(valueList.Select(p => new KeyValuePair<RedisKey, RedisValue>(p.Key, p.Value)).ToArray());

        /// <summary>
        /// 异步方法 保存一个对象
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="key">保存的Key名称</param>
        /// <param name="obj">对象实体</param>
        /// <param name="expiry">过期时间</param>
        /// <returns></returns>
        public async Task<bool> SetAsync<T>(string key, T obj, TimeSpan? expiry = default) => await SetAsync(key, RedisCacheProvider.ConvertJson(obj) ?? string.Empty, expiry);

        /// <summary>
        /// 异步方法 获取多个key的value值
        /// </summary>
        /// <param name="keys">要获取值的Key集合</param>
        /// <returns></returns>
        public async Task<List<string>> GetAsync(params string[] keys) => (await Redis.StringGetAsync(RedisCacheProvider.ConvertRedisKeys(keys))).Select(o => o.ToString()).ToList();

        /// <summary>
        /// 异步方法 获取多个key的value值
        /// </summary>
        /// <typeparam name="T">返回数据类型</typeparam>
        /// <param name="keys">要获取值的Key集合</param>
        /// <returns></returns>
        public async Task<List<T>> GetAsync<T>(params string[] keys) => RedisCacheProvider.ConvetList<T>(await Redis.StringGetAsync(RedisCacheProvider.ConvertRedisKeys(keys)));

        public string Get(string Key) => Redis.StringGet(Key);
        public T Get<T>(string Key) => RedisCacheProvider.ConvertObj<T>(Redis.StringGet(Key));
        #endregion
    }
}
