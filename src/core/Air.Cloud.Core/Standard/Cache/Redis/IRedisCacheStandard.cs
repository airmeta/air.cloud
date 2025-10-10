/*
 * Copyright (c) 2024-2030 星曳数据
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 *
 * This file is provided under the Mozilla Public License Version 2.0,
 * and the "NO WARRANTY" clause of the MPL is hereby expressly
 * acknowledged.
 */

namespace Air.Cloud.Core.Standard.Cache.Redis
{
    /// <summary>
    /// Redis缓存标准
    /// </summary>
    public  interface IRedisCacheStandard:IAppCacheStandard
    {
        /// <summary>
        /// zh-cn: 获取数据库
        /// en-us: Get database
        /// </summary>
        public object GetDatabase();
        /// <summary>
        /// 更换数据库
        /// </summary>
        /// <param name="DataBaseIndex"></param>
        /// <returns></returns>
        IRedisCacheStandard Change(int DataBaseIndex = 0);
        /// <summary>
        /// zh-cn: Hash 存储
        /// en-us: Hash stored
        /// </summary>
        public IHashCache Hash { get; }
        /// <summary>
        /// zh-cn: String 存储
        /// en-us: String stored
        /// </summary>
        public IStringCache String { get; }
        /// <summary>
        /// zh-cn: List 存储
        /// en-us: List stored
        /// </summary>
        public IListCache List { get; }
        /// <summary>
        /// zh-cn: Set 存储
        /// en-us: Set stored
        /// </summary>
        public ISetCache Set { get; }
        /// <summary>
        /// zh-cn: SortedSet 存储
        /// en-us: SortedSet stored
        /// </summary>
        public ISortedSetCache SortedSet { get; }

        public IRedisCacheKeyStandard Key { get; }
    }
    public interface IStringCache
    {
        /// <summary>
        /// <para>zh-cn:根据键获取值</para>
        /// <para>en-us:Get value by key</para>
        /// </summary>
        /// <param name="key">
        ///  <para>zh-cn:键</para>
        ///  <para>en-us:Key</para>
        /// </param>
        /// <returns></returns>
        string Get(string key);
        List<string> Get(params string[] keys);
        /// <summary>
        /// <para>zh-cn:获取一个对象</para>
        /// <para>en-us:Get an object</para>
        /// </summary>
        /// <typeparam name="T">
        ///  <para>zh-cn:对象类型</para>
        ///  <para>en-us:Object type</para>
        /// </typeparam>
        /// <param name="Key">
        ///  <para>zh-cn:保存的Key名称</para>
        ///  <para>en-us:Saved Key MainAssemblyName</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:反序列化后的对象信息,如果未获取到值,或者为 Empty或null 则返回default </para>
        /// <para>en-us:Deserialized object information, if no value is obtained, or Empty or null, return default</para>
        /// </returns>
        T Get<T>(string Key);
        List<T> Get<T>(params string[] keys);
        Task<List<string>> GetAsync(params string[] keys);
        Task<List<T>> GetAsync<T>(params string[] keys);
        bool Set(Dictionary<string, string> valueList);
        bool Set(string key, string value, TimeSpan? expiry = null);
        bool Set<T>(string key, T value, TimeSpan? expiry = null);
        Task<bool> SetAsync(Dictionary<string, string> valueList);
        Task<bool> SetAsync(string key, string value, TimeSpan? expiry = null);
        Task<bool> SetAsync<T>(string key, T obj, TimeSpan? expiry = null);
    }

    public interface IListCache
    {
        T GetByIndex<T>(string key, long index);
        Task<T> GetByIndexAsync<T>(string key, long index);
        long InsertAfter<T>(string key, T pivot, T value);
        Task<long> InsertAfterAsync<T>(string key, T pivot, T value);
        long InsertBefore<T>(string key, T pivot, T value);
        Task<long> InsertBeforeAsync<T>(string key, T pivot, T value);
        T LeftPop<T>(string key);
        Task<T> LeftPopAsync<T>(string key);
        long LeftPush<T>(string key, List<T> value);
        long LeftPush<T>(string key, T value);
        Task<long> LeftPushAsync<T>(string key, List<T> value);
        Task<long> LeftPushAsync<T>(string key, T value);
        long Length(string key);
        Task<long> LengthAsync(string key);
        List<T> Range<T>(string key);
        Task<List<T>> RangeAsync<T>(string key);
        long Remove<T>(string key, T value);
        Task<long> RemoveAsync<T>(string key, T value);
        T RightPop<T>(string key);
        Task<T> RightPopAsync<T>(string key);
        T RightPopLeftPush<T>(string key, string destination);
        Task<T> RightPopLeftPushAsync<T>(string key, string destination);
        long RightPush<T>(string key, List<T> value);
        long RightPush<T>(string key, T value);
        Task<long> RightPushAsync<T>(string key, List<T> value);
        Task<long> RightPushAsync<T>(string key, T value);
    }
    public interface ISetCache
    {
        long Add<T>(string key, params T[] value);
        Task<long> AddAsync<T>(string key, params T[] value);
        bool Contains<T>(string key, T value);
        Task<bool> ContainsAsync<T>(string key, T value);
        List<T> Difference<T>(params string[] keys);
        long DifferenceAndStore(string destination, params string[] keys);
        Task<long> DifferenceAndStoreAsync(string destination, params string[] keys);
        Task<List<T>> DifferenceAsync<T>(params string[] keys);
        List<T> Elements<T>(string key);
        Task<List<T>> ElementsAsync<T>(string key);
        List<T> Intersect<T>(params string[] keys);
        long IntersectAndStore(string destination, params string[] keys);
        Task<long> IntersectAndStoreAsync(string destination, params string[] keys);
        Task<List<T>> IntersectAsync<T>(params string[] keys);
        long Length(string key);
        Task<long> LengthAsync(string key);
        T Pop<T>(string key);
        Task<T> PopAsync<T>(string key);
        T Random<T>(string key);
        Task<T> RandomAsync<T>(string key);
        long Remove<T>(string key, params T[] value);
        Task<long> RemoveAsync<T>(string key, params T[] value);
        List<T> Union<T>(params string[] keys);
        Task<List<T>> UnionAsync<T>(params string[] keys);
        long UnionAndStore(string destination, params string[] keys);
        Task<long> UnionAndStoreAsync(string destination, params string[] keys);
        
    }
    public interface ISortedSetCache
    {
        long Add<T>(string key, List<T> value, double? score = null);
        bool Add<T>(string key, T value, double? score = null);
        Task<long> AddAsync<T>(string key, List<T> value, double? score = null);
        Task<bool> AddAsync<T>(string key, T value, double? score = null);
        long CombineIntersectAndStore(string destination, params string[] keys);
        Task<long> CombineIntersectAndStoreAsync(string destination, params string[] keys);
        long CombineUnionAndStore(string destination, params string[] keys);
        Task<long> CombineUnionAndStoreAsync(string destination, params string[] keys);
        double Decrement<T>(string key, T value, double scores);
        Task<double> DecrementAsync<T>(string key, T value, double scores);
        double Increment<T>(string key, T value, double scores);
        Task<double> IncrementAsync<T>(string key, T value, double scores);
        long Length(string key);
        Task<long> LengthAsync(string key);
        long LengthByValue<T>(string key, T startValue, T endValue);
        Task<long> LengthByValueAsync<T>(string key, T startValue, T endValue);
        double MaxScore(string key);
        Task<double> MaxScoreAsync(string key);
        double? MinScore(string key);
        Task<double> MinScoreAsync(string key);
        List<T> RangeByRank<T>(string key, long start = 0, long stop = -1, bool desc = false);
        Task<List<T>> RangeByRankAsync<T>(string key, long start = 0, long stop = -1, bool desc = false);
        Dictionary<T, double> RangeByRankWithScores<T>(string key, long start = 0, long stop = -1, bool desc = false) where T : class, new();
        Task<Dictionary<T, double>> RangeByRankWithScoresAsync<T>(string key, long start = 0, long stop = -1, bool desc = false);
        List<T> RangeByScore<T>(string key, double start = 0, double stop = -1, bool desc = false);
        Task<List<T>> RangeByScoreAsync<T>(string key, double start = 0, double stop = -1, bool desc = false);
        Dictionary<T, double> RangeByScoreWithScores<T>(string key, double start = 0, double stop = -1, bool desc = false);
        Task<Dictionary<T, double>> RangeByScoreWithScoresAsync<T>(string key, double start = 0, double stop = -1, bool desc = false);
        List<T> RangeByValue<T>(string key, T startValue, T endValue);
        Task<List<T>> RangeByValueAsync<T>(string key, T startValue, T endValue);
        long Remove<T>(string key, params T[] value);
        Task<long> RemoveAsync<T>(string key, params T[] value);
        long RemoveRangeByRank(string key, long start, long stop);
        Task<long> RemoveRangeByRankAsync(string key, long start, long stop);
        long RemoveRangeByScore(string key, double start, double stop);
        Task<long> RemoveRangeByScoreAsync(string key, double start, double stop);
        long RemoveRangeByValue<T>(string key, T startValue, T endValue);
        Task<long> RemoveRangeByValueAsync<T>(string key, T startValue, T endValue);
        double? Score<T>(string key, T value);
        Task<double?> SetScoreAsync<T>(string key, T value);
    }

    public interface IHashCache
    {
        /// <summary>
        /// en-us: Removes the specified fields from the hash stored at key.
        /// zh-cn: 从指定的Hash存储中删除一个或多个数据
        /// </summary>
        /// <param name="Key">zh-cn:Hash存储的Key/en-us:hash stored key</param>
        /// <param name="HashKey">zh-cn:待删除的Key/en-us:The key to be deleted</param>
        /// <returns>zh-cn:已删除的字段数/en-us:The number of fields that were removed.</returns>
        bool Delete(string Key, string HashKey);
        /// <summary>
        /// en-us: Removes the specified fields from the hash stored at key.
        /// zh-cn: 从指定的Hash存储中删除一个或多个数据
        /// </summary>
        /// <param name="Key">zh-cn:Hash存储的Key/en-us:hash stored key</param>
        /// <param name="HashKeys">zh-cn:待删除的Key/en-us:The key to be deleted</param>
        /// <returns>zh-cn:已删除的字段数/en-us:The number of fields that were removed.</returns>
        long Delete(string Key, params string[] HashKeys);
        /// <summary>
        /// en-us: Removes the specified fields from the hash stored at key.
        /// zh-cn: 从指定的Hash存储中删除一个或多个数据
        /// </summary>
        /// <param name="Key">zh-cn:Hash存储的Key/en-us:hash stored key</param>
        /// <param name="HashKey">zh-cn:待删除的Key/en-us:The key to be deleted</param>
        /// <returns>zh-cn:已删除的字段数/en-us:The number of fields that were removed.</returns>
        Task<bool> DeleteAsync(string Key, string HashKey);
        /// <summary>
        /// en-us: Removes the specified fields from the hash stored at key.
        /// zh-cn: 从指定的Hash存储中删除一个或多个数据
        /// </summary>
        /// <param name="Key">zh-cn:Hash存储的Key/en-us:hash stored key</param>
        /// <param name="HashKeys">zh-cn:待删除的Key/en-us:The key to be deleted</param>
        /// <returns>zh-cn:已删除的字段数/en-us:The number of fields that were removed.</returns>
        Task<long> DeleteAsync(string Key, params string[] HashKeys);
        /// <summary>
        /// en-us: Determine if a hash field exists.
        /// zh-cn: 判断Hash存储中是否存在某个字段
        /// </summary>
        /// <param name="Key">zh-cn:Hash存储的Key/en-us:hash stored key</param>
        /// <param name="HashKeys">zh-cn:待判断的Key/en-us:The key to be check</param>
        /// <returns>zh-cn:是/否 en-us:yes/not</returns>
        bool Exists(string Key, string HashKeys);
        /// <summary>
        /// en-us: Determine if a hash field exists.
        /// zh-cn: 判断Hash存储中是否存在某个字段
        /// </summary>
        /// <param name="Key">zh-cn:Hash存储的Key/en-us:hash stored key</param>
        /// <param name="HashKeys">zh-cn:待判断的Key/en-us:The key to be check</param>
        /// <returns>zh-cn:是/否 en-us:yes/not</returns>
        Task<bool> ExistsAsync(string Key, string HashKeys);
        T Get<T>(string Key, string HashKeys);
        Task<T> GetAsync<T>(string Key, string HashKeys);
        Dictionary<string, T> GetAll<T>(string Key);
        Task<Dictionary<string, T>> GetAllAsync<T>(string Key);
        string[] Keys(string Key);
        Task<string[]> KeysAsync(string Key);
        bool Set<T>(string Key, string HashKeys, T t);
        Task<bool> SetAsync<T>(string Key, string HashKeys, T t);
    }


    public interface IRedisCacheKeyStandard
    {
        long Delete(params string[] keys);
        bool Delete(string key);
        Task<long> DeleteAsync(params string[] keys);
        Task<bool> DeleteAsync(string key);
        bool Exists(string key);
        Task<bool> ExistsAsync(string key);
        bool Expire(string key, TimeSpan? expiry = null);
        Task<bool> ExpireAsync(string key, TimeSpan? expiry = null);
        void Fulsh();
        Task FulshAsync();
        bool Rename(string key, string newKey);
        Task<bool> RenameAsync(string key, string newKey);
        Task<bool> BlockLockTake(Func<Task> act, TimeSpan ts, string key, int count = 300);
    }
}
