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
using Air.Cloud.Modules.RedisCache.Provider;

using StackExchange.Redis;
using Air.Cloud.Core.Standard.Cache.Redis;
using Microsoft.AspNetCore.DataProtection.KeyManagement;

namespace SSS.Modules.Redis.Service
{
    /// <summary>
    /// Hash:类似dictionary，通过索引快速定位到指定元素的，耗时均等，跟string的区别在于不用反序列化，直接修改某个字段
    /// string的话要么是 001:序列化整个实体
    ///           要么是 001_name:  001_pwd: 多个key-value
    /// Hash的话，一个hashid-{key:value;key:value;key:value;}
    /// 可以一次性查找实体，也可以单个，还可以单个修改
    /// </summary>
    public class HashCache : IHashCache
    {
        protected readonly IDatabase Redis = null;

        public HashCache(IDatabase redis)
        {
            Redis = redis;
        }

        #region 同步方法

        /// <summary>
        /// 判断某个数据是否已经被缓存
        /// </summary>
        /// <param name="key"></param>
        /// <param name="dataKey"></param>
        /// <returns></returns>
        public  bool Exists(string key, string dataKey)
        {
            return Redis.HashExists(key, dataKey);
        }

        /// <summary>
        /// 存储数据到hash表
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="dataKey"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public bool Set<T>(string key, string dataKey, T t)
        {
            string? json = RedisCacheProvider.ConvertJson(t);
            return Redis.HashSet(key, dataKey, json);
        }

        /// <summary>
        /// 移除hash中的多个值
        /// </summary>
        /// <param name="key"></param>
        /// <param name="dataKeys"></param>
        /// <returns></returns>
        public long Delete(string key, params string[] dataKeys)
        {
            var newValues = dataKeys.Select(o => (RedisValue)o).ToArray();
            return Redis.HashDelete(key, newValues);
        }

        /// <summary>
        /// 从hash表获取数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="dataKey"></param>
        /// <returns></returns>
        public T Get<T>(string key, string dataKey)
        {
            string value = Redis.HashGet(key, dataKey);
            return RedisCacheProvider.ConvertObj<T>(value);
        }

        /// <summary>
        /// 获取hashkey所有key名称
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string[] Keys(string key)
        {
            RedisValue[] values = Redis.HashKeys(key);
            return values.Select(o => o.ToString()).ToArray();
        }

        /// <summary>
        /// 获取hashkey所有key与值，必须保证Key内的所有数据类型一致
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public Dictionary<string, T> GetAll<T>(string key)
        {
            var query = Redis.HashGetAll(key);
            Dictionary<string, T> dic = new Dictionary<string, T>();
            foreach (var item in query)
            {
                dic.Add(item.Name, RedisCacheProvider.ConvertObj<T>(item.Value));
            }
            return dic;
        }

        #endregion 同步方法

        #region 异步方法

        /// <summary>
        /// 异步方法 判断某个数据是否已经被缓存
        /// </summary>
        /// <param name="key"></param>
        /// <param name="dataKey"></param>
        /// <returns></returns>
        public async Task<bool> ExistsAsync(string key, string dataKey)
        {
            return await Redis.HashExistsAsync(key, dataKey);
        }

        /// <summary>
        /// 异步方法 存储数据到hash表
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="dataKey"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public async Task<bool> SetAsync<T>(string key, string dataKey, T t)
        {
            string json = RedisCacheProvider.ConvertJson(t);
            return await Redis.HashSetAsync(key, dataKey, json);
        }

        /// <summary>
        /// 异步方法 移除hash中的多个值
        /// </summary>
        /// <param name="key"></param>
        /// <param name="dataKeys"></param>
        /// <returns></returns>
        public async Task<long> DeleteAsync(string key, params string[] dataKeys)
        {
            var newValues = dataKeys.Select(o => (RedisValue)o).ToArray();
            return await Redis.HashDeleteAsync(key, newValues);
        }

        /// <summary>
        /// 异步方法 从hash表获取数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="dataKey"></param>
        /// <returns></returns>
        public async Task<T> GetAsync<T>(string key, string dataKey)
        {
            string value = await Redis.HashGetAsync(key, dataKey);
            return RedisCacheProvider.ConvertObj<T>(value);
        }

        /// <summary>
        /// 异步方法 获取hashkey所有key名称
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public async Task<string[]> KeysAsync(string key)
        {
            RedisValue[] values = await Redis.HashKeysAsync(key);
            return values.Select(o => o.ToString()).ToArray();
        }

        /// <summary>
        /// 获取hashkey所有key与值，必须保证Key内的所有数据类型一致
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public async Task<Dictionary<string, T>> GetAllAsync<T>(string key)
        {
            var query = await Redis.HashGetAllAsync(key);
            Dictionary<string, T> dic = new Dictionary<string, T>();
            foreach (var item in query)
            {
                dic.Add(item.Name, RedisCacheProvider.ConvertObj<T>(item.Value));
            }
            return dic;
        }

        public bool Delete(string Key, string HashKey)=> Redis.HashDelete(Key, HashKey);

        public async Task<bool> DeleteAsync(string Key, string HashKey) => await Redis.HashDeleteAsync(Key, HashKey);

        #endregion 异步方法

    }
}
