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
    /// Sorted Sets是将 Set 中的元素增加了一个权重参数 score，使得集合中的元素能够按 score 进行有序排列
    /// 1.带有权重的元素，比如一个游戏的用户得分排行榜
    /// 2.比较复杂的数据结构，一般用到的场景不算太多
    /// </summary>
    public class SortedSetCache :ISortedSetCache
    {
        private readonly IDatabase Redis;
        public SortedSetCache(IDatabase Redis)
        {
            this.Redis = Redis;
        }
        #region 同步方法

        /// <summary>
        /// 添加一个值到Key
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="score">排序分数，为空将获取集合中最大score加1</param>
        /// <returns></returns>
        public bool Add<T>(string key, T value, double? score = null) => Redis.SortedSetAdd(key, RedisCacheProvider.ConvertJson(value), score ?? _GetScore(key));

        /// <summary>
        /// 添加一个集合到Key
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="score">排序分数，为空将获取集合中最大score加1</param>
        /// <returns></returns>
        public long Add<T>(string key, List<T> value, double? score = null)
        {

            double scoreNum = score ?? _GetScore(key);
            SortedSetEntry[] rValue = value.Select(o => new SortedSetEntry(RedisCacheProvider.ConvertJson(o), scoreNum++)).ToArray();
            return Redis.SortedSetAdd(key, rValue);
        }

        /// <summary>
        /// 获取集合中的数量
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public long Length(string key) => Redis.SortedSetLength(key);

        /// <summary>
        /// 获取指定起始值到结束值的集合数量
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="startValue">起始值</param>
        /// <param name="endValue">结束值</param>
        /// <returns></returns>
        public long LengthByValue<T>(string key, T startValue, T endValue)
        {
            var sValue = RedisCacheProvider.ConvertJson(startValue);
            var eValue = RedisCacheProvider.ConvertJson(endValue);
            return Redis.SortedSetLengthByValue(key, sValue, eValue);
        }

        /// <summary>
        /// 获取指定Key的排序Score值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public double? Score<T>(string key, T value) => Redis.SortedSetScore(key, RedisCacheProvider.ConvertJson(value));

        /// <summary>
        /// 获取指定Key中最小Score值
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public double? MinScore(string key) => Redis.SortedSetRangeByRankWithScores(key, 0, 0, Order.Ascending).FirstOrDefault().Score;
        /// <summary>
        /// 获取指定Key中最大Score值
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public double MaxScore(string key) => Redis.SortedSetRangeByRankWithScores(key, 0, 0, Order.Descending).FirstOrDefault().Score;

        /// <summary>
        /// 删除Key中指定的值
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public long Remove<T>(string key, params T[] value) => Redis.SortedSetRemove(key, RedisCacheProvider.ConvertRedisValue(value));
        /// <summary>
        /// 删除指定起始值到结束值的数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="startValue">起始值</param>
        /// <param name="endValue">结束值</param>
        /// <returns></returns>
        public long RemoveRangeByValue<T>(string key, T startValue, T endValue)
        {
            var sValue = RedisCacheProvider.ConvertJson(startValue);
            var eValue = RedisCacheProvider.ConvertJson(endValue);
            return Redis.SortedSetRemoveRangeByValue(key, sValue, eValue);
        }

        /// <summary>
        /// 删除 从 start 开始的 stop 条数据
        /// </summary>
        /// <param name="key"></param>
        /// <param name="start"></param>
        /// <param name="stop"></param>
        /// <returns></returns>
        public long RemoveRangeByRank(string key, long start, long stop) => Redis.SortedSetRemoveRangeByRank(key, start, stop);
        /// <summary>
        /// 根据排序分数Score，删除从 start 开始的 stop 条数据
        /// </summary>
        /// <param name="key"></param>
        /// <param name="start"></param>
        /// <param name="stop"></param>
        /// <returns></returns>
        public long RemoveRangeByScore(string key, double start, double stop) => Redis.SortedSetRemoveRangeByScore(key, start, stop);

        /// <summary>
        /// 获取从 start 开始的 stop 条数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="start">起始数</param>
        /// <param name="stop">-1表示到结束，0为1条</param>
        /// <param name="desc">是否按降序排列</param>
        /// <returns></returns>
        public List<T> RangeByRank<T>(string key, long start = 0, long stop = -1, bool desc = false)
        {
            var rValue = Redis.SortedSetRangeByRank(key, start, stop, desc ? Order.Descending : Order.Ascending);
            return RedisCacheProvider.ConvetList<T>(rValue);
        }

        /// <summary>
        /// 获取从 start 开始的 stop 条数据包含Score，返回数据格式：Key=值，Value = Score
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="start">起始数</param>
        /// <param name="stop">-1表示到结束，0为1条</param>
        /// <param name="desc">是否按降序排列</param>
        /// <returns></returns>
        public Dictionary<T, double> RangeByRankWithScores<T>(string key, long start = 0, long stop = -1, bool desc = false) where T : class, new()
        {
            var rValue = Redis.SortedSetRangeByRankWithScores(key, start, stop, desc ? Order.Descending : Order.Ascending);
            Dictionary<T, double> dicList = new Dictionary<T, double>();
            foreach (var item in rValue)
            {
                dicList.Add(RedisCacheProvider.ConvertObj<T>(item.Element), item.Score);
            }
            return dicList;
        }

        /// <summary>
        ///  根据Score排序 获取从 start 开始的 stop 条数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="start">起始数</param>
        /// <param name="stop">-1表示到结束，0为1条</param>
        /// <param name="desc">是否按降序排列</param>
        /// <returns></returns>
        public List<T> RangeByScore<T>(string key, double start = 0, double stop = -1, bool desc = false)
        {
            var rValue = Redis.SortedSetRangeByScore(key, start, stop, Exclude.None, desc ? Order.Descending : Order.Ascending);
            return RedisCacheProvider.ConvetList<T>(rValue);
        }

        /// <summary>
        /// 根据Score排序  获取从 start 开始的 stop 条数据包含Score，返回数据格式：Key=值，Value = Score
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="start">起始数</param>
        /// <param name="stop">-1表示到结束，0为1条</param>
        /// <param name="desc">是否按降序排列</param>
        /// <returns></returns>
        public Dictionary<T, double> RangeByScoreWithScores<T>(string key, double start = 0, double stop = -1, bool desc = false)
        {
            var rValue = Redis.SortedSetRangeByScoreWithScores(key, start, stop, Exclude.None, desc ? Order.Descending : Order.Ascending);
            Dictionary<T, double> dicList = new Dictionary<T, double>();
            foreach (var item in rValue)
            {
                dicList.Add(RedisCacheProvider.ConvertObj<T>(item.Element), item.Score);
            }
            return dicList;
        }

        /// <summary>
        /// 获取指定起始值到结束值的数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="startValue">起始值</param>
        /// <param name="endValue">结束值</param>
        /// <returns></returns>
        public List<T> RangeByValue<T>(string key, T startValue, T endValue)
        {
            var sValue = RedisCacheProvider.ConvertJson(startValue);
            var eValue = RedisCacheProvider.ConvertJson(endValue);
            var rValue = Redis.SortedSetRangeByValue(key, sValue, eValue);
            return RedisCacheProvider.ConvetList<T>(rValue);
        }

        /// <summary>
        /// 获取几个集合的并集,并保存到一个新Key中
        /// </summary>
        /// <param name="destination">保存的新Key名称</param>
        /// <param name="keys">要操作的Key集合</param>
        /// <returns></returns>
        public long CombineUnionAndStore(string destination, params string[] keys) => _SortedSetCombineAndStore(SetOperation.Union, destination, keys);
        /// <summary>
        /// 获取几个集合的交集,并保存到一个新Key中
        /// </summary>
        /// <param name="destination">保存的新Key名称</param>
        /// <param name="keys">要操作的Key集合</param>
        /// <returns></returns>
        public long CombineIntersectAndStore(string destination, params string[] keys) => _SortedSetCombineAndStore(SetOperation.Intersect, destination, keys);

        /// <summary>
        /// 修改指定Key和值的Scores在原值上减去scores，并返回最终Scores
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="scores"></param>
        /// <returns></returns>
        public double Decrement<T>(string key, T value, double scores) => Redis.SortedSetDecrement(key, RedisCacheProvider.ConvertJson(value), scores);
        /// <summary>
        /// 修改指定Key和值的Scores在原值上增加scores，并返回最终Scores
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="scores"></param>
        /// <returns></returns>
        public double Increment<T>(string key, T value, double scores) => Redis.SortedSetIncrement(key, RedisCacheProvider.ConvertJson(value), scores);
        #endregion

        #region 异步方法

        /// <summary>
        /// 添加一个值到Key
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="score">排序分数，为空将获取集合中最大score加1</param>
        /// <returns></returns>
        public async Task<bool> AddAsync<T>(string key, T value, double? score = null) => await Redis.SortedSetAddAsync(key, RedisCacheProvider.ConvertJson(value), score ?? _GetScore(key));

        /// <summary>
        /// 添加一个集合到Key
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="score">排序分数，为空将获取集合中最大score加1</param>
        /// <returns></returns>
        public async Task<long> AddAsync<T>(string key, List<T> value, double? score = null)
        {
            double scoreNum = score ?? _GetScore(key);
            SortedSetEntry[] rValue = value.Select(o => new SortedSetEntry(RedisCacheProvider.ConvertJson(o), scoreNum++)).ToArray();
            return await Redis.SortedSetAddAsync(key, rValue);
        }

        /// <summary>
        /// 获取集合中的数量
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public async Task<long> LengthAsync(string key) => await Redis.SortedSetLengthAsync(key);

        /// <summary>
        /// 获取指定起始值到结束值的集合数量
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="startValue">起始值</param>
        /// <param name="endValue">结束值</param>
        /// <returns></returns>
        public async Task<long> LengthByValueAsync<T>(string key, T startValue, T endValue)
        {
            var sValue = RedisCacheProvider.ConvertJson(startValue);
            var eValue = RedisCacheProvider.ConvertJson(endValue);
            return await Redis.SortedSetLengthByValueAsync(key, sValue, eValue);
        }

        /// <summary>
        /// 获取指定Key的排序Score值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public async Task<double?> SetScoreAsync<T>(string key, T value) => await Redis.SortedSetScoreAsync(key, RedisCacheProvider.ConvertJson(value));

        /// <summary>
        /// 获取指定Key中最小Score值
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public async Task<double> MinScoreAsync(string key) => (await Redis.SortedSetRangeByRankWithScoresAsync(key, 0, 0, Order.Ascending)).FirstOrDefault().Score;
        /// <summary>
        /// 获取指定Key中最大Score值
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public async Task<double> MaxScoreAsync(string key) => (await Redis.SortedSetRangeByRankWithScoresAsync(key, 0, 0, Order.Descending)).FirstOrDefault().Score;
        /// <summary>
        /// 删除Key中指定的值
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public async Task<long> RemoveAsync<T>(string key, params T[] value) => await Redis.SortedSetRemoveAsync(key, RedisCacheProvider.ConvertRedisValue(value));

        /// <summary>
        /// 删除指定起始值到结束值的数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="startValue">起始值</param>
        /// <param name="endValue">结束值</param>
        /// <returns></returns>
        public async Task<long> RemoveRangeByValueAsync<T>(string key, T startValue, T endValue)
        {
            var sValue = RedisCacheProvider.ConvertJson(startValue);
            var eValue = RedisCacheProvider.ConvertJson(endValue);
            return await Redis.SortedSetRemoveRangeByValueAsync(key, sValue, eValue);
        }

        /// <summary>
        /// 删除 从 start 开始的 stop 条数据
        /// </summary>
        /// <param name="key"></param>
        /// <param name="start"></param>
        /// <param name="stop"></param>
        /// <returns></returns>
        public async Task<long> RemoveRangeByRankAsync(string key, long start, long stop) => await Redis.SortedSetRemoveRangeByRankAsync(key, start, stop);
        /// <summary>
        /// 根据排序分数Score，删除从 start 开始的 stop 条数据
        /// </summary>
        /// <param name="key"></param>
        /// <param name="start"></param>
        /// <param name="stop"></param>
        /// <returns></returns>
        public async Task<long> RemoveRangeByScoreAsync(string key, double start, double stop) => await Redis.SortedSetRemoveRangeByScoreAsync(key, start, stop);
        /// <summary>
        /// 获取从 start 开始的 stop 条数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="start">起始数</param>
        /// <param name="stop">-1表示到结束，0为1条</param>
        /// <param name="desc">是否按降序排列</param>
        /// <returns></returns>
        public async Task<List<T>> RangeByRankAsync<T>(string key, long start = 0, long stop = -1, bool desc = false)
        {
            var rValue = await Redis.SortedSetRangeByRankAsync(key, start, stop, desc ? Order.Descending : Order.Ascending);
            return RedisCacheProvider.ConvetList<T>(rValue);
        }
        /// <summary>
        /// 获取从 start 开始的 stop 条数据包含Score，返回数据格式：Key=值，Value = Score
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="start">起始数</param>
        /// <param name="stop">-1表示到结束，0为1条</param>
        /// <param name="desc">是否按降序排列</param>
        /// <returns></returns>
        public async Task<Dictionary<T, double>> RangeByRankWithScoresAsync<T>(string key, long start = 0, long stop = -1, bool desc = false)
        {
            var rValue = await Redis.SortedSetRangeByRankWithScoresAsync(key, start, stop, desc ? Order.Descending : Order.Ascending);
            Dictionary<T, double> dicList = new Dictionary<T, double>();
            foreach (var item in rValue)
            {
                dicList.Add(RedisCacheProvider.ConvertObj<T>(item.Element), item.Score);
            }
            return dicList;
        }

        /// <summary>
        ///  根据Score排序 获取从 start 开始的 stop 条数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="start">起始数</param>
        /// <param name="stop">-1表示到结束，0为1条</param>
        /// <param name="desc">是否按降序排列</param>
        /// <returns></returns>
        public async Task<List<T>> RangeByScoreAsync<T>(string key, double start = 0, double stop = -1, bool desc = false)
        {
            var rValue = await Redis.SortedSetRangeByScoreAsync(key, start, stop, Exclude.None, desc ? Order.Descending : Order.Ascending);
            return RedisCacheProvider.ConvetList<T>(rValue);
        }

        /// <summary>
        /// 根据Score排序  获取从 start 开始的 stop 条数据包含Score，返回数据格式：Key=值，Value = Score
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="start">起始数</param>
        /// <param name="stop">-1表示到结束，0为1条</param>
        /// <param name="desc">是否按降序排列</param>
        /// <returns></returns>
        public async Task<Dictionary<T, double>> RangeByScoreWithScoresAsync<T>(string key, double start = 0, double stop = -1, bool desc = false)
        {
            var rValue = await Redis.SortedSetRangeByScoreWithScoresAsync(key, start, stop, Exclude.None, desc ? Order.Descending : Order.Ascending);
            Dictionary<T, double> dicList = new Dictionary<T, double>();
            foreach (var item in rValue)
            {
                dicList.Add(RedisCacheProvider.ConvertObj<T>(item.Element), item.Score);
            }
            return dicList;
        }

        /// <summary>
        /// 获取指定起始值到结束值的数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="startValue">起始值</param>
        /// <param name="endValue">结束值</param>
        /// <returns></returns>
        public async Task<List<T>> RangeByValueAsync<T>(string key, T startValue, T endValue)
        {
            var sValue = RedisCacheProvider.ConvertJson(startValue);
            var eValue = RedisCacheProvider.ConvertJson(endValue);
            var rValue = await Redis.SortedSetRangeByValueAsync(key, sValue, eValue);
            return RedisCacheProvider.ConvetList<T>(rValue);
        }

        /// <summary>
        /// 获取几个集合的并集,并保存到一个新Key中
        /// </summary>
        /// <param name="destination">保存的新Key名称</param>
        /// <param name="keys">要操作的Key集合</param>
        /// <returns></returns>
        public async Task<long> CombineUnionAndStoreAsync(string destination, params string[] keys) => await _SortedSetCombineAndStoreAsync(SetOperation.Union, destination, keys);
        /// <summary>
        /// 获取几个集合的交集,并保存到一个新Key中
        /// </summary>
        /// <param name="destination">保存的新Key名称</param>
        /// <param name="keys">要操作的Key集合</param>
        /// <returns></returns>
        public async Task<long> CombineIntersectAndStoreAsync(string destination, params string[] keys) => await _SortedSetCombineAndStoreAsync(SetOperation.Intersect, destination, keys);
        /// <summary>
        /// 修改指定Key和值的Scores在原值上减去scores，并返回最终Scores
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="scores"></param>
        /// <returns></returns>
        public async Task<double> DecrementAsync<T>(string key, T value, double scores) => await Redis.SortedSetDecrementAsync(key, RedisCacheProvider.ConvertJson(value), scores);
        /// <summary>
        /// 修改指定Key和值的Scores在原值上增加scores，并返回最终Scores
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="scores"></param>
        /// <returns></returns>
        public async Task<double> IncrementAsync<T>(string key, T value, double scores) => await Redis.SortedSetIncrementAsync(key, RedisCacheProvider.ConvertJson(value), scores);
        #endregion

        #region 内部辅助方法
        /// <summary>
        /// 获取指定Key中最大Score值,
        /// </summary>
        /// <param name="key">key名称，注意要先添加上Key前缀</param>
        /// <returns></returns>
        private double _GetScore(string key)
        {
            double dValue = 0;
            var rValue = Redis.SortedSetRangeByRankWithScores(key, 0, 0, Order.Descending).FirstOrDefault();
            dValue = rValue.Score;
            return dValue + 1;
        }

        /// <summary>
        /// 获取几个集合的交叉并集合,并保存到一个新Key中
        /// </summary>
        /// <param name="operation">Union：并集  Intersect：交集  Difference：差集  详见 <see cref="SetOperation"/></param>
        /// <param name="destination">保存的新Key名称</param>
        /// <param name="keys">要操作的Key集合</param>
        /// <returns></returns>
        private long _SortedSetCombineAndStore(SetOperation operation, string destination, params string[] keys)
        {
            RedisKey[] keyList = RedisCacheProvider.ConvertRedisKeys(keys);
            var rValue = Redis.SortedSetCombineAndStore(operation, destination, keyList);
            return rValue;

        }

        /// <summary>
        /// 获取几个集合的交叉并集合,并保存到一个新Key中
        /// </summary>
        /// <param name="operation">Union：并集  Intersect：交集  Difference：差集  详见 <see cref="SetOperation"/></param>
        /// <param name="destination">保存的新Key名称</param>
        /// <param name="keys">要操作的Key集合</param>
        /// <returns></returns>
        private async Task<long> _SortedSetCombineAndStoreAsync(SetOperation operation, string destination, params string[] keys)
        {
            RedisKey[] keyList = RedisCacheProvider.ConvertRedisKeys(keys);
            var rValue = await Redis.SortedSetCombineAndStoreAsync(operation, destination, keyList);
            return rValue;
        }

        #endregion
    }
}
