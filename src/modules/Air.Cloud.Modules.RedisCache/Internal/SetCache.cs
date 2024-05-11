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
using Air.Cloud.Core.Standard.Cache.Redis;
using Air.Cloud.Modules.RedisCache.Provider;

using StackExchange.Redis;

namespace SSS.Modules.Redis.Service
{

    /// <summary>
    /// Set：用哈希表来保持字符串的唯一性，没有先后顺序，存储一些集合性的数据
    /// 1.共同好友、二度好友
    /// 2.利用唯一性，可以统计访问网站的所有独立 IP
    /// </summary>
    public class SetCache : ISetCache
    {
        private readonly IDatabase Redis;
        public SetCache(IDatabase Redis)
        {
            this.Redis = Redis;
        }
        public ISetCache Sorted;
        #region 同步方法
        /// <summary>
        /// 在Key集合中添加多个value值
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="key">Key名称</param>
        /// <param name="value">值列表</param>
        /// <returns></returns>
        public long Add<T>(string key, params T[] value)
        {
            RedisValue[] valueList = RedisCacheProvider.ConvertRedisValue(value.ToArray());
            return Redis.SetAdd(key, valueList);
        }

        /// <summary>
        /// 获取key集合值的数量
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public long Length(string key)
        {
            return Redis.SetLength(key);
        }

        /// <summary>
        /// 判断Key集合中是否包含指定的值
        /// </summary>
        /// <typeparam name="T">值类型</typeparam>
        /// <param name="key"></param>
        /// <param name="value">要判断是值</param>
        /// <returns></returns>
        public bool Contains<T>(string key, T value)
        {
            string? jValue = RedisCacheProvider.ConvertJson(value);
            return Redis.SetContains(key, jValue);
        }

        /// <summary>
        /// 随机获取key集合中的一个值
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public T Random<T>(string key)
        {
            var rValue = Redis.SetRandomMember(key);
            return RedisCacheProvider.ConvertObj<T>(rValue);
        }

        /// <summary>
        /// 获取key所有值的集合
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public List<T> Elements<T>(string key)
        {
            var rValue = Redis.SetMembers(key);
            return RedisCacheProvider.ConvetList<T>(rValue);
        }

        /// <summary>
        /// 
        /// key集合中指定的value
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public long Remove<T>(string key, params T[] value)
        {

            RedisValue[] valueList = RedisCacheProvider.ConvertRedisValue(value);
            return Redis.SetRemove(key, valueList);
        }

        /// <summary>
        /// 随机删除key集合中的一个值，并返回该值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public T Pop<T>(string key)
        {

            var rValue = Redis.SetPop(key);
            return RedisCacheProvider.ConvertObj<T>(rValue);
        }


        /// <summary>
        /// 获取几个集合的并集
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="keys">要操作的Key集合</param>
        /// <returns></returns>
        public List<T> Union<T>(params string[] keys)
        {
            return _SetCombine<T>(SetOperation.Union, keys);
        }
        /// <summary>
        /// 获取几个集合的交集
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="keys">要操作的Key集合</param>
        /// <returns></returns>
        public List<T> Intersect<T>(params string[] keys)
        {
            return _SetCombine<T>(SetOperation.Intersect, keys);
        }
        /// <summary>
        /// 获取几个集合的差集
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="keys">要操作的Key集合</param>
        /// <returns></returns>t
        public List<T> Difference<T>(params string[] keys)
        {
            return _SetCombine<T>(SetOperation.Difference, keys);
        }

        /// <summary>
        /// 获取几个集合的并集,并保存到一个新Key中
        /// </summary>
        /// <param name="destination">保存的新Key名称</param>
        /// <param name="keys">要操作的Key集合</param>
        /// <returns></returns>
        public long UnionAndStore(string destination, params string[] keys)
        {
            return _SetCombineAndStore(SetOperation.Union, destination, keys);
        }
        /// <summary>
        /// 获取几个集合的交集,并保存到一个新Key中
        /// </summary>
        /// <param name="destination">保存的新Key名称</param>
        /// <param name="keys">要操作的Key集合</param>
        /// <returns></returns>
        public long IntersectAndStore(string destination, params string[] keys)
        {
            return _SetCombineAndStore(SetOperation.Intersect, destination, keys);
        }
        /// <summary>
        /// 获取几个集合的差集,并保存到一个新Key中
        /// </summary>
        /// <param name="destination">保存的新Key名称</param>
        /// <param name="keys">要操作的Key集合</param>
        /// <returns></returns>
        public long DifferenceAndStore(string destination, params string[] keys)
        {
            return _SetCombineAndStore(SetOperation.Difference, destination, keys);
        }
        #endregion

        #region 异步方法

        /// <summary>
        /// 在Key集合中添加多个value值
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="key">Key名称</param>
        /// <param name="value">值列表</param>
        /// <returns></returns>
        public async Task<long> AddAsync<T>(string key, params T[] value)
        {
            RedisValue[] valueList = RedisCacheProvider.ConvertRedisValue(value.ToArray());
            return await Redis.SetAddAsync(key, valueList);
        }

        /// <summary>
        /// 获取key集合值的数量
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public async Task<long> LengthAsync(string key)
        {
            return await Redis.SetLengthAsync(key);
        }

        /// <summary>
        /// 判断Key集合中是否包含指定的值
        /// </summary>
        /// <typeparam name="T">值类型</typeparam>
        /// <param name="key"></param>
        /// <param name="value">要判断是值</param>
        /// <returns></returns>
        public async Task<bool> ContainsAsync<T>(string key, T value)
        {
            string jValue = RedisCacheProvider.ConvertJson(value);
            return await Redis.SetContainsAsync(key, jValue);
        }

        /// <summary>
        /// 随机获取key集合中的一个值
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public async Task<T> RandomAsync<T>(string key)
        {
            var rValue = await Redis.SetRandomMemberAsync(key);
            return RedisCacheProvider.ConvertObj<T>(rValue);
        }

        /// <summary>
        /// 获取key所有值的集合
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public async Task<List<T>> ElementsAsync<T>(string key)
        {
            var rValue = await Redis.SetMembersAsync(key);
            return RedisCacheProvider.ConvetList<T>(rValue);
        }

        /// <summary>
        /// 删除key集合中指定的value
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public async Task<long> RemoveAsync<T>(string key, params T[] value)
        {
            RedisValue[] valueList = RedisCacheProvider.ConvertRedisValue(value);
            return await Redis.SetRemoveAsync(key, valueList);
        }

        /// <summary>
        /// 随机删除key集合中的一个值，并返回该值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public async Task<T> PopAsync<T>(string key)
        {
            var rValue = await Redis.SetPopAsync(key);
            return RedisCacheProvider.ConvertObj<T>(rValue);
        }


        /// <summary>
        /// 获取几个集合的并集
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="keys">要操作的Key集合</param>
        /// <returns></returns>
        public async Task<List<T>> UnionAsync<T>(params string[] keys)
        {
            return await _SetCombineAsync<T>(SetOperation.Union, keys);
        }
        /// <summary>
        /// 获取几个集合的交集
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="keys">要操作的Key集合</param>
        /// <returns></returns>
        public async Task<List<T>> IntersectAsync<T>(params string[] keys)
        {
            return await _SetCombineAsync<T>(SetOperation.Intersect, keys);
        }
        /// <summary>
        /// 获取几个集合的差集
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="keys">要操作的Key集合</param>
        /// <returns></returns>
        public async Task<List<T>> DifferenceAsync<T>(params string[] keys)
        {
            return await _SetCombineAsync<T>(SetOperation.Difference, keys);
        }



        /// <summary>
        /// 获取几个集合的并集,并保存到一个新Key中
        /// </summary>
        /// <param name="destination">保存的新Key名称</param>
        /// <param name="keys">要操作的Key集合</param>
        /// <returns></returns>
        public async Task<long> UnionAndStoreAsync(string destination, params string[] keys)
        {
            return await _SetCombineAndStoreAsync(SetOperation.Union, destination, keys);
        }
        /// <summary>
        /// 获取几个集合的交集,并保存到一个新Key中
        /// </summary>
        /// <param name="destination">保存的新Key名称</param>
        /// <param name="keys">要操作的Key集合</param>
        /// <returns></returns>
        public async Task<long> IntersectAndStoreAsync(string destination, params string[] keys)
        {
            return await _SetCombineAndStoreAsync(SetOperation.Intersect, destination, keys);
        }
        /// <summary>
        /// 获取几个集合的差集,并保存到一个新Key中
        /// </summary>
        /// <param name="destination">保存的新Key名称</param>
        /// <param name="keys">要操作的Key集合</param>
        /// <returns></returns>
        public async Task<long> DifferenceAndStoreAsync(string destination, params string[] keys)
        {
            return await _SetCombineAndStoreAsync(SetOperation.Difference, destination, keys);
        }

        #endregion

        #region 内部辅助方法
        /// <summary>
        /// 获取几个集合的交叉并集合
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="operation">Union：并集  Intersect：交集  Difference：差集  详见 <see cref="SetOperation"/></param>
        /// <param name="keys">要操作的Key集合</param>
        /// <returns></returns>
        private List<T> _SetCombine<T>(SetOperation operation, params string[] keys)
        {
            RedisKey[] keyList = RedisCacheProvider.ConvertRedisKeys(keys);
            var rValue = Redis.SetCombine(operation, keyList);
            return RedisCacheProvider.ConvetList<T>(rValue);
        }

        /// <summary>
        /// 获取几个集合的交叉并集合,并保存到一个新Key中
        /// </summary>
        /// <param name="operation">Union：并集  Intersect：交集  Difference：差集  详见 <see cref="SetOperation"/></param>
        /// <param name="destination">保存的新Key名称</param>
        /// <param name="keys">要操作的Key集合</param>
        /// <returns></returns>
        private long _SetCombineAndStore(SetOperation operation, string destination, params string[] keys)
        {
            RedisKey[] keyList = RedisCacheProvider.ConvertRedisKeys(keys);
            return Redis.SetCombineAndStore(operation, destination, keyList);
        }
        /// <summary>
        /// 获取几个集合的交叉并集合
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="operation">Union：并集  Intersect：交集  Difference：差集  详见 <see cref="SetOperation"/></param>
        /// <param name="keys">要操作的Key集合</param>
        /// <returns></returns>
        private async Task<List<T>> _SetCombineAsync<T>(SetOperation operation, params string[] keys)
        {
            RedisKey[] keyList = RedisCacheProvider.ConvertRedisKeys(keys);
            var rValue = await Redis.SetCombineAsync(operation, keyList);
            return RedisCacheProvider.ConvetList<T>(rValue);
        }
        /// <summary>
        /// 获取几个集合的交叉并集合,并保存到一个新Key中
        /// </summary>
        /// <param name="operation">Union：并集  Intersect：交集  Difference：差集  详见 <see cref="SetOperation"/></param>
        /// <param name="destination">保存的新Key名称</param>
        /// <param name="keys">要操作的Key集合</param>
        /// <returns></returns>
        private async Task<long> _SetCombineAndStoreAsync(SetOperation operation, string destination, params string[] keys)
        {
            RedisKey[] keyList = RedisCacheProvider.ConvertRedisKeys(keys);
            return await Redis.SetCombineAndStoreAsync(operation, destination, keyList);
        }

        #endregion

    }
}
