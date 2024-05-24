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
    /// RedisCache list的实现为一个双向链表，即可以支持反向查找和遍历，更方便操作，不过带来了部分额外的内存开销，
    /// Redis内部的很多实现，包括发送缓冲队列等也都是用的这个数据结构。  
    /// 一般是左进右出或者右进左出 
    /// </summary>
    public class ListCache : IListCache
    {
        protected readonly IDatabase Redis;
        public ListCache(IDatabase Redis)
        {
            this.Redis = Redis;
        }
        #region 同步方法
        /// <summary>
        /// 从左侧向list中添加一个值，返回集合总数
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public long LeftPush<T>(string key, T value)
        {
            string? jValue = AppRealization.JSON.Serialize(value);
            return Redis.ListLeftPush(key, jValue);
        }

        /// <summary>
        /// 从左侧向list中添加多个值，返回集合总数
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public long LeftPush<T>(string key, List<T> value)
        {
            RedisValue[] valueList = RedisCacheProvider.ConvertRedisValue(value.ToArray());
            return Redis.ListLeftPush(key, valueList);
        }

        /// <summary>
        /// 从右侧向list中添加一个值，返回集合总数
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public long RightPush<T>(string key, T value)
        {
            string? jValue = AppRealization.JSON.Serialize(value);
            return Redis.ListRightPush(key, jValue);
        }

        /// <summary>
        /// 从右侧向list中添加多个值，返回集合总数
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public long RightPush<T>(string key, List<T> value)
        {
            RedisValue[] valueList = RedisCacheProvider.ConvertRedisValue(value.ToArray());
            return Redis.ListRightPush(key, valueList);
        }

        /// <summary>
        /// 从左侧向list中取出一个值并从list中删除
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public T LeftPop<T>(string key)
        {
            var rValue = Redis.ListLeftPop(key);
            return AppRealization.JSON.Deserialize<T>(rValue);
        }

        /// <summary>
        /// 从右侧向list中取出一个值并从list中删除
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public T RightPop<T>(string key)
        {
            var rValue = Redis.ListRightPop(key);
            return AppRealization.JSON.Deserialize<T>(rValue);
        }

        /// <summary>
        /// 从key的List中右侧取出一个值，并从左侧添加到destination集合中，且返回该数据对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">要取出数据的List名称</param>
        /// <param name="destination">要添加到的List名称</param>
        /// <returns></returns>
        public T RightPopLeftPush<T>(string key, string destination)
        {
            var rValue = Redis.ListRightPopLeftPush(key, destination);
            return AppRealization.JSON.Deserialize<T>(rValue);
        }

        /// <summary>
        /// 在key的List指定值pivot之后插入value，返回集合总数
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="pivot">索引值</param>
        /// <param name="value">要插入的值</param>
        /// <returns></returns>
        public long InsertAfter<T>(string key, T pivot, T value)
        {
            string? pValue = AppRealization.JSON.Serialize(pivot);
            string? jValue = AppRealization.JSON.Serialize(value);
            return Redis.ListInsertAfter(key, pValue, jValue);
        }

        /// <summary>
        /// 在key的List指定值pivot之前插入value，返回集合总数
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="pivot">索引值</param>
        /// <param name="value">要插入的值</param>
        /// <returns></returns>
        public long InsertBefore<T>(string key, T pivot, T value)
        {
            string? pValue = AppRealization.JSON.Serialize(pivot);
            string? jValue = AppRealization.JSON.Serialize(value);
            return Redis.ListInsertBefore(key, pValue, jValue);
        }

        /// <summary>
        /// 从key的list中取出所有数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public List<T> Range<T>(string key)
        {
            var rValue = Redis.ListRange(key);
            return RedisCacheProvider.ConvetList<T>(rValue);
        }

        /// <summary>
        /// 从key的List获取指定索引的值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public T GetByIndex<T>(string key, long index)
        {
            var rValue = Redis.ListGetByIndex(key, index);
            return AppRealization.JSON.Deserialize<T>(rValue);
        }

        /// <summary>
        /// 获取key的list中数据个数
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public long Length(string key)
        {
            return Redis.ListLength(key);
        }

        /// <summary>
        /// 从key的List中移除指定的值，返回删除个数
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public long Remove<T>(string key, T value)
        {
            string? jValue = AppRealization.JSON.Serialize(value);
            return Redis.ListRemove(key, jValue);
        }
        #endregion

        #region 异步方法
        /// <summary>
        /// 从左侧向list中添加一个值，返回集合总数
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public async Task<long> LeftPushAsync<T>(string key, T value)
        {
            string? jValue = AppRealization.JSON.Serialize(value);
            return await Redis.ListLeftPushAsync(key, jValue);
        }

        /// <summary>
        /// 从左侧向list中添加多个值，返回集合总数
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public async Task<long> LeftPushAsync<T>(string key, List<T> value)
        {
            RedisValue[] valueList = RedisCacheProvider.ConvertRedisValue(value.ToArray());
            return await Redis.ListLeftPushAsync(key, valueList);
        }

        /// <summary>
        /// 从右侧向list中添加一个值，返回集合总数
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public async Task<long> RightPushAsync<T>(string key, T value)
        {
            string? jValue = AppRealization.JSON.Serialize(value);
            return await Redis.ListRightPushAsync(key, jValue);
        }

        /// <summary>
        /// 从右侧向list中添加多个值，返回集合总数
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public async Task<long> RightPushAsync<T>(string key, List<T> value)
        {
            RedisValue[] valueList = RedisCacheProvider.ConvertRedisValue(value.ToArray());
            return await Redis.ListRightPushAsync(key, valueList);
        }

        /// <summary>
        /// 从左侧向list中取出一个值并从list中删除
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public async Task<T> LeftPopAsync<T>(string key)
        {
            var rValue = await Redis.ListLeftPopAsync(key);
            return AppRealization.JSON.Deserialize<T>(rValue);
        }

        /// <summary>
        /// 从右侧向list中取出一个值并从list中删除
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public async Task<T> RightPopAsync<T>(string key)
        {
            var rValue = await Redis.ListRightPopAsync(key);
            return AppRealization.JSON.Deserialize<T>(rValue);
        }

        /// <summary>
        /// 从key的List中右侧取出一个值，并从左侧添加到destination集合中，且返回该数据对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">要取出数据的List名称</param>
        /// <param name="destination">要添加到的List名称</param>
        /// <returns></returns>
        public async Task<T> RightPopLeftPushAsync<T>(string key, string destination)
        {
            var rValue = await Redis.ListRightPopLeftPushAsync(key, destination);
            return AppRealization.JSON.Deserialize<T>(rValue);
        }

        /// <summary>
        /// 在key的List指定值pivot之后插入value，返回集合总数
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="pivot">索引值</param>
        /// <param name="value">要插入的值</param>
        /// <returns></returns>
        public async Task<long> InsertAfterAsync<T>(string key, T pivot, T value)
        {
            string? pValue = AppRealization.JSON.Serialize(pivot);
            string? jValue = AppRealization.JSON.Serialize(value);
            return await Redis.ListInsertAfterAsync(key, pValue, jValue);
        }

        /// <summary>
        /// 在key的List指定值pivot之前插入value，返回集合总数
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="pivot">索引值</param>
        /// <param name="value">要插入的值</param>
        /// <returns></returns>
        public async Task<long> InsertBeforeAsync<T>(string key, T pivot, T value)
        {
            string? pValue = AppRealization.JSON.Serialize(pivot);
            string? jValue = AppRealization.JSON.Serialize(value);
            return await Redis.ListInsertBeforeAsync(key, pValue, jValue);
        }

        /// <summary>
        /// 从key的list中取出所有数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public async Task<List<T>> RangeAsync<T>(string key)
        {
            var rValue = await Redis.ListRangeAsync(key);
            return RedisCacheProvider.ConvetList<T>(rValue);
        }

        /// <summary>
        /// 从key的List获取指定索引的值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public async Task<T> GetByIndexAsync<T>(string key, long index)
        {
            var rValue = await Redis.ListGetByIndexAsync(key, index);
            return AppRealization.JSON.Deserialize<T>(rValue);
        }

        /// <summary>
        /// 获取key的list中数据个数
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public async Task<long> LengthAsync(string key)
        {
            return await Redis.ListLengthAsync(key);
        }

        /// <summary>
        /// 从key的List中移除指定的值，返回删除个数
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public async Task<long> RemoveAsync<T>(string key, T value)
        {
            string? jValue = AppRealization.JSON.Serialize(value);
            return await Redis.ListRemoveAsync(key, jValue);
        }
        #endregion
    }
}
