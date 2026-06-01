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
    /// <para>zh-cn:定义 Redis 缓存模块对外暴露的统一访问标准，聚合字符串、哈希、列表、集合、有序集合和键管理等常用 Redis 数据结构能力。</para>
    /// <para>en-us:Defines the unified Redis cache access contract exposed by the Redis cache module, aggregating common Redis data structures such as strings, hashes, lists, sets, sorted sets, and key management.</para>
    /// </summary>
    public  interface IRedisCacheStandard:IAppCacheStandard
    {
        /// <summary>
        /// <para>zh-cn:获取当前 Redis 连接正在使用的底层数据库对象，用于在标准接口未覆盖的场景中访问原生 Redis 客户端能力。</para>
        /// <para>en-us:Gets the underlying database object used by the current Redis connection, allowing callers to access native Redis client capabilities when the standard contract does not cover a scenario.</para>
        /// </summary>
        /// <returns>
        /// <para>zh-cn:当前 Redis 数据库实例；具体运行时类型由 Redis 模块实现决定。</para>
        /// <para>en-us:The current Redis database instance; the concrete runtime type is determined by the Redis module implementation.</para>
        /// </returns>
        public object GetDatabase();
        /// <summary>
        /// <para>zh-cn:切换当前 Redis 缓存标准实例使用的数据库索引，并返回切换后的缓存访问对象。</para>
        /// <para>en-us:Switches the database index used by the current Redis cache standard instance and returns the cache access object after switching.</para>
        /// </summary>
        /// <param name="DataBaseIndex">
        /// <para>zh-cn:目标 Redis 数据库索引，默认值为 0。</para>
        /// <para>en-us:The target Redis database index. The default value is 0.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:使用指定数据库索引的 Redis 缓存标准实例。</para>
        /// <para>en-us:The Redis cache standard instance using the specified database index.</para>
        /// </returns>
        IRedisCacheStandard Change(int DataBaseIndex = 0);
        /// <summary>
        /// <para>zh-cn:获取 Redis Hash 数据结构的缓存操作入口。</para>
        /// <para>en-us:Gets the cache operation entry point for the Redis hash data structure.</para>
        /// </summary>
        public IHashCache Hash { get; }
        /// <summary>
        /// <para>zh-cn:获取 Redis String 数据结构的缓存操作入口，适用于简单字符串、序列化对象和批量键值读写。</para>
        /// <para>en-us:Gets the cache operation entry point for the Redis string data structure, suitable for plain strings, serialized objects, and batch key-value reads and writes.</para>
        /// </summary>
        public IStringCache String { get; }
        /// <summary>
        /// <para>zh-cn:获取 Redis List 数据结构的缓存操作入口，适用于队列、栈和有序列表场景。</para>
        /// <para>en-us:Gets the cache operation entry point for the Redis list data structure, suitable for queue, stack, and ordered-list scenarios.</para>
        /// </summary>
        public IListCache List { get; }
        /// <summary>
        /// <para>zh-cn:获取 Redis Set 数据结构的缓存操作入口，适用于去重集合、交集、并集和差集场景。</para>
        /// <para>en-us:Gets the cache operation entry point for the Redis set data structure, suitable for distinct collections, intersections, unions, and differences.</para>
        /// </summary>
        public ISetCache Set { get; }
        /// <summary>
        /// <para>zh-cn:获取 Redis Sorted Set 数据结构的缓存操作入口，适用于带分值排序、排名和范围查询的集合场景。</para>
        /// <para>en-us:Gets the cache operation entry point for the Redis sorted set data structure, suitable for score-based ordering, ranking, and range-query scenarios.</para>
        /// </summary>
        public ISortedSetCache SortedSet { get; }

        /// <summary>
        /// <para>zh-cn:获取 Redis 键管理操作入口，用于删除、判断存在、设置过期时间、刷新、重命名和分布式阻塞锁等键级操作。</para>
        /// <para>en-us:Gets the Redis key-management operation entry point for key-level operations such as delete, existence checks, expiration, flush, rename, and distributed blocking locks.</para>
        /// </summary>
        public IRedisCacheKeyStandard Key { get; }
    }
    /// <summary>
    /// <para>zh-cn:定义 Redis String 数据结构的缓存操作标准，支持字符串值、序列化对象、批量键值和异步读写。</para>
    /// <para>en-us:Defines the cache operation contract for the Redis string data structure, supporting string values, serialized objects, batch key-values, and asynchronous reads and writes.</para>
    /// </summary>
    public interface IStringCache
    {
        /// <summary>
        /// <para>zh-cn:根据指定键读取字符串值。</para>
        /// <para>en-us:Reads a string value by the specified key.</para>
        /// </summary>
        /// <param name="key">
        /// <para>zh-cn:需要读取的 Redis 键。</para>
        /// <para>en-us:The Redis key to read.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:键对应的字符串值；如果键不存在，具体返回值由 Redis 模块实现决定。</para>
        /// <para>en-us:The string value associated with the key; when the key does not exist, the concrete return value is determined by the Redis module implementation.</para>
        /// </returns>
        string Get(string key);
        /// <summary>
        /// <para>zh-cn:根据多个键批量读取字符串值。</para>
        /// <para>en-us:Reads string values in batch by multiple keys.</para>
        /// </summary>
        /// <param name="keys">
        /// <para>zh-cn:需要读取的一组 Redis 键。</para>
        /// <para>en-us:The Redis keys to read.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:按实现约定返回的字符串值集合，通常与输入键顺序对应。</para>
        /// <para>en-us:A collection of string values returned according to the implementation contract, usually corresponding to the input key order.</para>
        /// </returns>
        List<string> Get(params string[] keys);
        /// <summary>
        /// <para>zh-cn:根据指定键读取并反序列化对象值。</para>
        /// <para>en-us:Reads and deserializes an object value by the specified key.</para>
        /// </summary>
        /// <typeparam name="T">
        /// <para>zh-cn:目标对象类型。</para>
        /// <para>en-us:The target object type.</para>
        /// </typeparam>
        /// <param name="Key">
        /// <para>zh-cn:保存对象值的 Redis 键。</para>
        /// <para>en-us:The Redis key where the object value is stored.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:反序列化后的对象；如果没有获取到值、值为空或无法反序列化，具体返回值由实现决定，通常为 `default`。</para>
        /// <para>en-us:The deserialized object; if no value is found, the value is empty, or deserialization cannot be completed, the concrete return value is implementation-defined and is usually `default`.</para>
        /// </returns>
        T Get<T>(string Key);
        /// <summary>
        /// <para>zh-cn:根据多个键批量读取并反序列化对象值。</para>
        /// <para>en-us:Reads and deserializes object values in batch by multiple keys.</para>
        /// </summary>
        /// <typeparam name="T">
        /// <para>zh-cn:目标对象类型。</para>
        /// <para>en-us:The target object type.</para>
        /// </typeparam>
        /// <param name="keys">
        /// <para>zh-cn:需要读取的一组 Redis 键。</para>
        /// <para>en-us:The Redis keys to read.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:反序列化后的对象集合，顺序和缺失值处理由实现约定决定。</para>
        /// <para>en-us:A collection of deserialized objects. Ordering and missing-value handling are determined by the implementation contract.</para>
        /// </returns>
        List<T> Get<T>(params string[] keys);
        /// <summary>
        /// <para>zh-cn:异步根据多个键批量读取字符串值。</para>
        /// <para>en-us:Asynchronously reads string values in batch by multiple keys.</para>
        /// </summary>
        /// <param name="keys">
        /// <para>zh-cn:需要读取的一组 Redis 键。</para>
        /// <para>en-us:The Redis keys to read.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:表示异步读取操作的任务，结果为字符串值集合。</para>
        /// <para>en-us:A task representing the asynchronous read operation, with a collection of string values as the result.</para>
        /// </returns>
        Task<List<string>> GetAsync(params string[] keys);
        /// <summary>
        /// <para>zh-cn:异步根据多个键批量读取并反序列化对象值。</para>
        /// <para>en-us:Asynchronously reads and deserializes object values in batch by multiple keys.</para>
        /// </summary>
        /// <typeparam name="T">
        /// <para>zh-cn:目标对象类型。</para>
        /// <para>en-us:The target object type.</para>
        /// </typeparam>
        /// <param name="keys">
        /// <para>zh-cn:需要读取的一组 Redis 键。</para>
        /// <para>en-us:The Redis keys to read.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:表示异步读取操作的任务，结果为反序列化后的对象集合。</para>
        /// <para>en-us:A task representing the asynchronous read operation, with a collection of deserialized objects as the result.</para>
        /// </returns>
        Task<List<T>> GetAsync<T>(params string[] keys);
        /// <summary>
        /// <para>zh-cn:批量写入字符串键值对。</para>
        /// <para>en-us:Writes string key-value pairs in batch.</para>
        /// </summary>
        /// <param name="valueList">
        /// <para>zh-cn:需要写入的键值对集合，字典键为 Redis 键，字典值为字符串缓存值。</para>
        /// <para>en-us:The key-value pairs to write. Dictionary keys are Redis keys, and dictionary values are string cache values.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:如果批量写入成功则返回 `true`；否则返回 `false`。</para>
        /// <para>en-us:Returns `true` when the batch write succeeds; otherwise returns `false`.</para>
        /// </returns>
        bool Set(Dictionary<string, string> valueList);
        /// <summary>
        /// <para>zh-cn:写入单个字符串缓存值，并可选设置过期时间。</para>
        /// <para>en-us:Writes a single string cache value and optionally sets an expiration time.</para>
        /// </summary>
        /// <param name="key">
        /// <para>zh-cn:需要写入的 Redis 键。</para>
        /// <para>en-us:The Redis key to write.</para>
        /// </param>
        /// <param name="value">
        /// <para>zh-cn:需要保存的字符串值。</para>
        /// <para>en-us:The string value to store.</para>
        /// </param>
        /// <param name="expiry">
        /// <para>zh-cn:键的过期时间；为 `null` 时由实现决定是否设置过期时间。</para>
        /// <para>en-us:The key expiration time. When `null`, whether an expiration is set is determined by the implementation.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:如果写入成功则返回 `true`；否则返回 `false`。</para>
        /// <para>en-us:Returns `true` when the write succeeds; otherwise returns `false`.</para>
        /// </returns>
        bool Set(string key, string value, TimeSpan? expiry = null);
        /// <summary>
        /// <para>zh-cn:序列化并写入单个对象缓存值，并可选设置过期时间。</para>
        /// <para>en-us:Serializes and writes a single object cache value and optionally sets an expiration time.</para>
        /// </summary>
        /// <typeparam name="T">
        /// <para>zh-cn:需要保存的对象类型。</para>
        /// <para>en-us:The type of the object to store.</para>
        /// </typeparam>
        /// <param name="key">
        /// <para>zh-cn:需要写入的 Redis 键。</para>
        /// <para>en-us:The Redis key to write.</para>
        /// </param>
        /// <param name="value">
        /// <para>zh-cn:需要保存的对象值。</para>
        /// <para>en-us:The object value to store.</para>
        /// </param>
        /// <param name="expiry">
        /// <para>zh-cn:键的过期时间；为 `null` 时由实现决定是否设置过期时间。</para>
        /// <para>en-us:The key expiration time. When `null`, whether an expiration is set is determined by the implementation.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:如果写入成功则返回 `true`；否则返回 `false`。</para>
        /// <para>en-us:Returns `true` when the write succeeds; otherwise returns `false`.</para>
        /// </returns>
        bool Set<T>(string key, T value, TimeSpan? expiry = null);
        /// <summary>
        /// <para>zh-cn:异步批量写入字符串键值对。</para>
        /// <para>en-us:Asynchronously writes string key-value pairs in batch.</para>
        /// </summary>
        /// <param name="valueList">
        /// <para>zh-cn:需要写入的键值对集合，字典键为 Redis 键，字典值为字符串缓存值。</para>
        /// <para>en-us:The key-value pairs to write. Dictionary keys are Redis keys, and dictionary values are string cache values.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:表示异步写入操作的任务，结果表示批量写入是否成功。</para>
        /// <para>en-us:A task representing the asynchronous write operation, whose result indicates whether the batch write succeeded.</para>
        /// </returns>
        Task<bool> SetAsync(Dictionary<string, string> valueList);
        /// <summary>
        /// <para>zh-cn:异步写入单个字符串缓存值，并可选设置过期时间。</para>
        /// <para>en-us:Asynchronously writes a single string cache value and optionally sets an expiration time.</para>
        /// </summary>
        /// <param name="key">
        /// <para>zh-cn:需要写入的 Redis 键。</para>
        /// <para>en-us:The Redis key to write.</para>
        /// </param>
        /// <param name="value">
        /// <para>zh-cn:需要保存的字符串值。</para>
        /// <para>en-us:The string value to store.</para>
        /// </param>
        /// <param name="expiry">
        /// <para>zh-cn:键的过期时间；为 `null` 时由实现决定是否设置过期时间。</para>
        /// <para>en-us:The key expiration time. When `null`, whether an expiration is set is determined by the implementation.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:表示异步写入操作的任务，结果表示写入是否成功。</para>
        /// <para>en-us:A task representing the asynchronous write operation, whose result indicates whether the write succeeded.</para>
        /// </returns>
        Task<bool> SetAsync(string key, string value, TimeSpan? expiry = null);
        /// <summary>
        /// <para>zh-cn:异步序列化并写入单个对象缓存值，并可选设置过期时间。</para>
        /// <para>en-us:Asynchronously serializes and writes a single object cache value and optionally sets an expiration time.</para>
        /// </summary>
        /// <typeparam name="T">
        /// <para>zh-cn:需要保存的对象类型。</para>
        /// <para>en-us:The type of the object to store.</para>
        /// </typeparam>
        /// <param name="key">
        /// <para>zh-cn:需要写入的 Redis 键。</para>
        /// <para>en-us:The Redis key to write.</para>
        /// </param>
        /// <param name="obj">
        /// <para>zh-cn:需要保存的对象值。</para>
        /// <para>en-us:The object value to store.</para>
        /// </param>
        /// <param name="expiry">
        /// <para>zh-cn:键的过期时间；为 `null` 时由实现决定是否设置过期时间。</para>
        /// <para>en-us:The key expiration time. When `null`, whether an expiration is set is determined by the implementation.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:表示异步写入操作的任务，结果表示写入是否成功。</para>
        /// <para>en-us:A task representing the asynchronous write operation, whose result indicates whether the write succeeded.</para>
        /// </returns>
        Task<bool> SetAsync<T>(string key, T obj, TimeSpan? expiry = null);
    }

    /// <summary>
    /// <para>zh-cn:定义 Redis List 数据结构的缓存操作标准，支持按索引读取、左右弹出、左右推入、范围读取和元素删除。</para>
    /// <para>en-us:Defines the cache operation contract for the Redis list data structure, supporting index reads, left and right pops, left and right pushes, range reads, and element removal.</para>
    /// </summary>
    public interface IListCache
    {
        /// <summary>
        /// <para>zh-cn:根据列表键和索引读取指定位置的元素，并反序列化为目标类型。</para>
        /// <para>en-us:Reads the element at the specified index from the list key and deserializes it to the target type.</para>
        /// </summary>
        /// <typeparam name="T">
        /// <para>zh-cn:目标元素类型。</para>
        /// <para>en-us:The target element type.</para>
        /// </typeparam>
        /// <param name="key">
        /// <para>zh-cn:Redis List 键。</para>
        /// <para>en-us:The Redis list key.</para>
        /// </param>
        /// <param name="index">
        /// <para>zh-cn:需要读取的元素索引，具体索引规则遵循 Redis List 语义。</para>
        /// <para>en-us:The element index to read. The concrete indexing rule follows Redis list semantics.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:指定索引处的元素；当索引不存在时，返回值由实现决定。</para>
        /// <para>en-us:The element at the specified index; when the index does not exist, the return value is determined by the implementation.</para>
        /// </returns>
        T GetByIndex<T>(string key, long index);
        /// <summary>
        /// <para>zh-cn:异步根据列表键和索引读取指定位置的元素，并反序列化为目标类型。</para>
        /// <para>en-us:Asynchronously reads the element at the specified index from the list key and deserializes it to the target type.</para>
        /// </summary>
        /// <typeparam name="T">
        /// <para>zh-cn:目标元素类型。</para>
        /// <para>en-us:The target element type.</para>
        /// </typeparam>
        /// <param name="key">
        /// <para>zh-cn:Redis List 键。</para>
        /// <para>en-us:The Redis list key.</para>
        /// </param>
        /// <param name="index">
        /// <para>zh-cn:需要读取的元素索引，具体索引规则遵循 Redis List 语义。</para>
        /// <para>en-us:The element index to read. The concrete indexing rule follows Redis list semantics.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:表示异步读取操作的任务，结果为指定索引处的元素。</para>
        /// <para>en-us:A task representing the asynchronous read operation, with the element at the specified index as the result.</para>
        /// </returns>
        Task<T> GetByIndexAsync<T>(string key, long index);
        /// <summary>
        /// <para>zh-cn:在 Redis List 中指定基准元素之后插入新元素。</para>
        /// <para>en-us:Inserts a new element after the specified pivot element in a Redis list.</para>
        /// </summary>
        /// <typeparam name="T">
        /// <para>zh-cn:列表元素类型。</para>
        /// <para>en-us:The list element type.</para>
        /// </typeparam>
        /// <param name="key">
        /// <para>zh-cn:Redis List 键。</para>
        /// <para>en-us:The Redis list key.</para>
        /// </param>
        /// <param name="pivot">
        /// <para>zh-cn:作为插入位置参考的基准元素。</para>
        /// <para>en-us:The pivot element used as the insertion reference.</para>
        /// </param>
        /// <param name="value">
        /// <para>zh-cn:需要插入的新元素。</para>
        /// <para>en-us:The new element to insert.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:插入后的列表长度或 Redis 客户端返回的操作结果。</para>
        /// <para>en-us:The list length after insertion or the operation result returned by the Redis client.</para>
        /// </returns>
        long InsertAfter<T>(string key, T pivot, T value);
        /// <summary>
        /// <para>zh-cn:异步在 Redis List 中指定基准元素之后插入新元素。</para>
        /// <para>en-us:Asynchronously inserts a new element after the specified pivot element in a Redis list.</para>
        /// </summary>
        /// <typeparam name="T">
        /// <para>zh-cn:列表元素类型。</para>
        /// <para>en-us:The list element type.</para>
        /// </typeparam>
        /// <param name="key">
        /// <para>zh-cn:Redis List 键。</para>
        /// <para>en-us:The Redis list key.</para>
        /// </param>
        /// <param name="pivot">
        /// <para>zh-cn:作为插入位置参考的基准元素。</para>
        /// <para>en-us:The pivot element used as the insertion reference.</para>
        /// </param>
        /// <param name="value">
        /// <para>zh-cn:需要插入的新元素。</para>
        /// <para>en-us:The new element to insert.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:表示异步插入操作的任务，结果为插入后的列表长度或 Redis 客户端返回的操作结果。</para>
        /// <para>en-us:A task representing the asynchronous insert operation, with the list length after insertion or the Redis client operation result as the result.</para>
        /// </returns>
        Task<long> InsertAfterAsync<T>(string key, T pivot, T value);
        /// <summary>
        /// <para>zh-cn:在 Redis List 中指定基准元素之前插入新元素。</para>
        /// <para>en-us:Inserts a new element before the specified pivot element in a Redis list.</para>
        /// </summary>
        /// <typeparam name="T">
        /// <para>zh-cn:列表元素类型。</para>
        /// <para>en-us:The list element type.</para>
        /// </typeparam>
        /// <param name="key">
        /// <para>zh-cn:Redis List 键。</para>
        /// <para>en-us:The Redis list key.</para>
        /// </param>
        /// <param name="pivot">
        /// <para>zh-cn:作为插入位置参考的基准元素。</para>
        /// <para>en-us:The pivot element used as the insertion reference.</para>
        /// </param>
        /// <param name="value">
        /// <para>zh-cn:需要插入的新元素。</para>
        /// <para>en-us:The new element to insert.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:插入后的列表长度或 Redis 客户端返回的操作结果。</para>
        /// <para>en-us:The list length after insertion or the operation result returned by the Redis client.</para>
        /// </returns>
        long InsertBefore<T>(string key, T pivot, T value);
        /// <summary>
        /// <para>zh-cn:异步在 Redis List 中指定基准元素之前插入新元素。</para>
        /// <para>en-us:Asynchronously inserts a new element before the specified pivot element in a Redis list.</para>
        /// </summary>
        /// <typeparam name="T">
        /// <para>zh-cn:列表元素类型。</para>
        /// <para>en-us:The list element type.</para>
        /// </typeparam>
        /// <param name="key">
        /// <para>zh-cn:Redis List 键。</para>
        /// <para>en-us:The Redis list key.</para>
        /// </param>
        /// <param name="pivot">
        /// <para>zh-cn:作为插入位置参考的基准元素。</para>
        /// <para>en-us:The pivot element used as the insertion reference.</para>
        /// </param>
        /// <param name="value">
        /// <para>zh-cn:需要插入的新元素。</para>
        /// <para>en-us:The new element to insert.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:表示异步插入操作的任务，结果为插入后的列表长度或 Redis 客户端返回的操作结果。</para>
        /// <para>en-us:A task representing the asynchronous insert operation, with the list length after insertion or the Redis client operation result as the result.</para>
        /// </returns>
        Task<long> InsertBeforeAsync<T>(string key, T pivot, T value);
        /// <summary>
        /// <para>zh-cn:从 Redis List 左侧弹出一个元素，并反序列化为目标类型。</para>
        /// <para>en-us:Pops one element from the left side of a Redis list and deserializes it to the target type.</para>
        /// </summary>
        /// <typeparam name="T">
        /// <para>zh-cn:目标元素类型。</para>
        /// <para>en-us:The target element type.</para>
        /// </typeparam>
        /// <param name="key">
        /// <para>zh-cn:Redis List 键。</para>
        /// <para>en-us:The Redis list key.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:弹出的元素；当列表为空或键不存在时，返回值由实现决定。</para>
        /// <para>en-us:The popped element; when the list is empty or the key does not exist, the return value is determined by the implementation.</para>
        /// </returns>
        T LeftPop<T>(string key);
        /// <summary>
        /// <para>zh-cn:异步从 Redis List 左侧弹出一个元素，并反序列化为目标类型。</para>
        /// <para>en-us:Asynchronously pops one element from the left side of a Redis list and deserializes it to the target type.</para>
        /// </summary>
        /// <typeparam name="T">
        /// <para>zh-cn:目标元素类型。</para>
        /// <para>en-us:The target element type.</para>
        /// </typeparam>
        /// <param name="key">
        /// <para>zh-cn:Redis List 键。</para>
        /// <para>en-us:The Redis list key.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:表示异步弹出操作的任务，结果为弹出的元素。</para>
        /// <para>en-us:A task representing the asynchronous pop operation, with the popped element as the result.</para>
        /// </returns>
        Task<T> LeftPopAsync<T>(string key);
        /// <summary>
        /// <para>zh-cn:将多个元素从左侧推入 Redis List。</para>
        /// <para>en-us:Pushes multiple elements to the left side of a Redis list.</para>
        /// </summary>
        /// <typeparam name="T">
        /// <para>zh-cn:列表元素类型。</para>
        /// <para>en-us:The list element type.</para>
        /// </typeparam>
        /// <param name="key">
        /// <para>zh-cn:Redis List 键。</para>
        /// <para>en-us:The Redis list key.</para>
        /// </param>
        /// <param name="value">
        /// <para>zh-cn:需要推入列表的元素集合。</para>
        /// <para>en-us:The collection of elements to push into the list.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:推入后的列表长度或 Redis 客户端返回的操作结果。</para>
        /// <para>en-us:The list length after pushing or the operation result returned by the Redis client.</para>
        /// </returns>
        long LeftPush<T>(string key, List<T> value);
        /// <summary>
        /// <para>zh-cn:将单个元素从左侧推入 Redis List。</para>
        /// <para>en-us:Pushes a single element to the left side of a Redis list.</para>
        /// </summary>
        /// <typeparam name="T">
        /// <para>zh-cn:列表元素类型。</para>
        /// <para>en-us:The list element type.</para>
        /// </typeparam>
        /// <param name="key">
        /// <para>zh-cn:Redis List 键。</para>
        /// <para>en-us:The Redis list key.</para>
        /// </param>
        /// <param name="value">
        /// <para>zh-cn:需要推入列表的元素。</para>
        /// <para>en-us:The element to push into the list.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:推入后的列表长度或 Redis 客户端返回的操作结果。</para>
        /// <para>en-us:The list length after pushing or the operation result returned by the Redis client.</para>
        /// </returns>
        long LeftPush<T>(string key, T value);
        /// <summary>
        /// <para>zh-cn:异步将多个元素从左侧推入 Redis List。</para>
        /// <para>en-us:Asynchronously pushes multiple elements to the left side of a Redis list.</para>
        /// </summary>
        /// <typeparam name="T">
        /// <para>zh-cn:列表元素类型。</para>
        /// <para>en-us:The list element type.</para>
        /// </typeparam>
        /// <param name="key">
        /// <para>zh-cn:Redis List 键。</para>
        /// <para>en-us:The Redis list key.</para>
        /// </param>
        /// <param name="value">
        /// <para>zh-cn:需要推入列表的元素集合。</para>
        /// <para>en-us:The collection of elements to push into the list.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:表示异步推入操作的任务，结果为推入后的列表长度或 Redis 客户端返回的操作结果。</para>
        /// <para>en-us:A task representing the asynchronous push operation, with the list length after pushing or the Redis client operation result as the result.</para>
        /// </returns>
        Task<long> LeftPushAsync<T>(string key, List<T> value);
        /// <summary>
        /// <para>zh-cn:异步将单个元素从左侧推入 Redis List。</para>
        /// <para>en-us:Asynchronously pushes a single element to the left side of a Redis list.</para>
        /// </summary>
        /// <typeparam name="T">
        /// <para>zh-cn:列表元素类型。</para>
        /// <para>en-us:The list element type.</para>
        /// </typeparam>
        /// <param name="key">
        /// <para>zh-cn:Redis List 键。</para>
        /// <para>en-us:The Redis list key.</para>
        /// </param>
        /// <param name="value">
        /// <para>zh-cn:需要推入列表的元素。</para>
        /// <para>en-us:The element to push into the list.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:表示异步推入操作的任务，结果为推入后的列表长度或 Redis 客户端返回的操作结果。</para>
        /// <para>en-us:A task representing the asynchronous push operation, with the list length after pushing or the Redis client operation result as the result.</para>
        /// </returns>
        Task<long> LeftPushAsync<T>(string key, T value);
        /// <summary>
        /// <para>zh-cn:获取 Redis List 的元素数量。</para>
        /// <para>en-us:Gets the number of elements in a Redis list.</para>
        /// </summary>
        /// <param name="key">
        /// <para>zh-cn:Redis List 键。</para>
        /// <para>en-us:The Redis list key.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:列表中的元素数量。</para>
        /// <para>en-us:The number of elements in the list.</para>
        /// </returns>
        long Length(string key);
        /// <summary>
        /// <para>zh-cn:异步获取 Redis List 的元素数量。</para>
        /// <para>en-us:Asynchronously gets the number of elements in a Redis list.</para>
        /// </summary>
        /// <param name="key">
        /// <para>zh-cn:Redis List 键。</para>
        /// <para>en-us:The Redis list key.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:表示异步读取操作的任务，结果为列表中的元素数量。</para>
        /// <para>en-us:A task representing the asynchronous read operation, with the number of elements in the list as the result.</para>
        /// </returns>
        Task<long> LengthAsync(string key);
        /// <summary>
        /// <para>zh-cn:读取 Redis List 的元素范围，并反序列化为目标类型集合。</para>
        /// <para>en-us:Reads a range of elements from a Redis list and deserializes them to a collection of the target type.</para>
        /// </summary>
        /// <typeparam name="T">
        /// <para>zh-cn:目标元素类型。</para>
        /// <para>en-us:The target element type.</para>
        /// </typeparam>
        /// <param name="key">
        /// <para>zh-cn:Redis List 键。</para>
        /// <para>en-us:The Redis list key.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:列表范围内的元素集合；具体范围由实现默认值决定。</para>
        /// <para>en-us:The elements in the list range; the concrete range is determined by the implementation default.</para>
        /// </returns>
        List<T> Range<T>(string key);
        /// <summary>
        /// <para>zh-cn:异步读取 Redis List 的元素范围，并反序列化为目标类型集合。</para>
        /// <para>en-us:Asynchronously reads a range of elements from a Redis list and deserializes them to a collection of the target type.</para>
        /// </summary>
        /// <typeparam name="T">
        /// <para>zh-cn:目标元素类型。</para>
        /// <para>en-us:The target element type.</para>
        /// </typeparam>
        /// <param name="key">
        /// <para>zh-cn:Redis List 键。</para>
        /// <para>en-us:The Redis list key.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:表示异步读取操作的任务，结果为列表范围内的元素集合。</para>
        /// <para>en-us:A task representing the asynchronous read operation, with the elements in the list range as the result.</para>
        /// </returns>
        Task<List<T>> RangeAsync<T>(string key);
        /// <summary>
        /// <para>zh-cn:从 Redis List 中删除与指定值匹配的元素。</para>
        /// <para>en-us:Removes elements matching the specified value from a Redis list.</para>
        /// </summary>
        /// <typeparam name="T">
        /// <para>zh-cn:列表元素类型。</para>
        /// <para>en-us:The list element type.</para>
        /// </typeparam>
        /// <param name="key">
        /// <para>zh-cn:Redis List 键。</para>
        /// <para>en-us:The Redis list key.</para>
        /// </param>
        /// <param name="value">
        /// <para>zh-cn:需要删除的元素值。</para>
        /// <para>en-us:The element value to remove.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:被删除的元素数量。</para>
        /// <para>en-us:The number of removed elements.</para>
        /// </returns>
        long Remove<T>(string key, T value);
        /// <summary>
        /// <para>zh-cn:异步从 Redis List 中删除与指定值匹配的元素。</para>
        /// <para>en-us:Asynchronously removes elements matching the specified value from a Redis list.</para>
        /// </summary>
        /// <typeparam name="T">
        /// <para>zh-cn:列表元素类型。</para>
        /// <para>en-us:The list element type.</para>
        /// </typeparam>
        /// <param name="key">
        /// <para>zh-cn:Redis List 键。</para>
        /// <para>en-us:The Redis list key.</para>
        /// </param>
        /// <param name="value">
        /// <para>zh-cn:需要删除的元素值。</para>
        /// <para>en-us:The element value to remove.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:表示异步删除操作的任务，结果为被删除的元素数量。</para>
        /// <para>en-us:A task representing the asynchronous remove operation, with the number of removed elements as the result.</para>
        /// </returns>
        Task<long> RemoveAsync<T>(string key, T value);
        /// <summary>
        /// <para>zh-cn:从 Redis List 右侧弹出一个元素，并反序列化为目标类型。</para>
        /// <para>en-us:Pops one element from the right side of a Redis list and deserializes it to the target type.</para>
        /// </summary>
        /// <typeparam name="T">
        /// <para>zh-cn:目标元素类型。</para>
        /// <para>en-us:The target element type.</para>
        /// </typeparam>
        /// <param name="key">
        /// <para>zh-cn:Redis List 键。</para>
        /// <para>en-us:The Redis list key.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:弹出的元素；当列表为空或键不存在时，返回值由实现决定。</para>
        /// <para>en-us:The popped element; when the list is empty or the key does not exist, the return value is determined by the implementation.</para>
        /// </returns>
        T RightPop<T>(string key);
        /// <summary>
        /// <para>zh-cn:异步从 Redis List 右侧弹出一个元素，并反序列化为目标类型。</para>
        /// <para>en-us:Asynchronously pops one element from the right side of a Redis list and deserializes it to the target type.</para>
        /// </summary>
        /// <typeparam name="T">
        /// <para>zh-cn:目标元素类型。</para>
        /// <para>en-us:The target element type.</para>
        /// </typeparam>
        /// <param name="key">
        /// <para>zh-cn:Redis List 键。</para>
        /// <para>en-us:The Redis list key.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:表示异步弹出操作的任务，结果为弹出的元素。</para>
        /// <para>en-us:A task representing the asynchronous pop operation, with the popped element as the result.</para>
        /// </returns>
        Task<T> RightPopAsync<T>(string key);
        /// <summary>
        /// <para>zh-cn:从源 Redis List 右侧弹出一个元素，并将该元素推入目标 List 左侧。</para>
        /// <para>en-us:Pops one element from the right side of the source Redis list and pushes it to the left side of the destination list.</para>
        /// </summary>
        /// <typeparam name="T">
        /// <para>zh-cn:列表元素类型。</para>
        /// <para>en-us:The list element type.</para>
        /// </typeparam>
        /// <param name="key">
        /// <para>zh-cn:源 Redis List 键。</para>
        /// <para>en-us:The source Redis list key.</para>
        /// </param>
        /// <param name="destination">
        /// <para>zh-cn:目标 Redis List 键。</para>
        /// <para>en-us:The destination Redis list key.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:被移动的元素；当源列表为空或键不存在时，返回值由实现决定。</para>
        /// <para>en-us:The moved element; when the source list is empty or the key does not exist, the return value is determined by the implementation.</para>
        /// </returns>
        T RightPopLeftPush<T>(string key, string destination);
        /// <summary>
        /// <para>zh-cn:异步从源 Redis List 右侧弹出一个元素，并将该元素推入目标 List 左侧。</para>
        /// <para>en-us:Asynchronously pops one element from the right side of the source Redis list and pushes it to the left side of the destination list.</para>
        /// </summary>
        /// <typeparam name="T">
        /// <para>zh-cn:列表元素类型。</para>
        /// <para>en-us:The list element type.</para>
        /// </typeparam>
        /// <param name="key">
        /// <para>zh-cn:源 Redis List 键。</para>
        /// <para>en-us:The source Redis list key.</para>
        /// </param>
        /// <param name="destination">
        /// <para>zh-cn:目标 Redis List 键。</para>
        /// <para>en-us:The destination Redis list key.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:表示异步移动操作的任务，结果为被移动的元素。</para>
        /// <para>en-us:A task representing the asynchronous move operation, with the moved element as the result.</para>
        /// </returns>
        Task<T> RightPopLeftPushAsync<T>(string key, string destination);
        /// <summary>
        /// <para>zh-cn:将多个元素从右侧推入 Redis List。</para>
        /// <para>en-us:Pushes multiple elements to the right side of a Redis list.</para>
        /// </summary>
        /// <typeparam name="T">
        /// <para>zh-cn:列表元素类型。</para>
        /// <para>en-us:The list element type.</para>
        /// </typeparam>
        /// <param name="key">
        /// <para>zh-cn:Redis List 键。</para>
        /// <para>en-us:The Redis list key.</para>
        /// </param>
        /// <param name="value">
        /// <para>zh-cn:需要推入列表的元素集合。</para>
        /// <para>en-us:The collection of elements to push into the list.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:推入后的列表长度或 Redis 客户端返回的操作结果。</para>
        /// <para>en-us:The list length after pushing or the operation result returned by the Redis client.</para>
        /// </returns>
        long RightPush<T>(string key, List<T> value);
        /// <summary>
        /// <para>zh-cn:将单个元素从右侧推入 Redis List。</para>
        /// <para>en-us:Pushes a single element to the right side of a Redis list.</para>
        /// </summary>
        /// <typeparam name="T">
        /// <para>zh-cn:列表元素类型。</para>
        /// <para>en-us:The list element type.</para>
        /// </typeparam>
        /// <param name="key">
        /// <para>zh-cn:Redis List 键。</para>
        /// <para>en-us:The Redis list key.</para>
        /// </param>
        /// <param name="value">
        /// <para>zh-cn:需要推入列表的元素。</para>
        /// <para>en-us:The element to push into the list.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:推入后的列表长度或 Redis 客户端返回的操作结果。</para>
        /// <para>en-us:The list length after pushing or the operation result returned by the Redis client.</para>
        /// </returns>
        long RightPush<T>(string key, T value);
        /// <summary>
        /// <para>zh-cn:异步将多个元素从右侧推入 Redis List。</para>
        /// <para>en-us:Asynchronously pushes multiple elements to the right side of a Redis list.</para>
        /// </summary>
        /// <typeparam name="T">
        /// <para>zh-cn:列表元素类型。</para>
        /// <para>en-us:The list element type.</para>
        /// </typeparam>
        /// <param name="key">
        /// <para>zh-cn:Redis List 键。</para>
        /// <para>en-us:The Redis list key.</para>
        /// </param>
        /// <param name="value">
        /// <para>zh-cn:需要推入列表的元素集合。</para>
        /// <para>en-us:The collection of elements to push into the list.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:表示异步推入操作的任务，结果为推入后的列表长度或 Redis 客户端返回的操作结果。</para>
        /// <para>en-us:A task representing the asynchronous push operation, with the list length after pushing or the Redis client operation result as the result.</para>
        /// </returns>
        Task<long> RightPushAsync<T>(string key, List<T> value);
        /// <summary>
        /// <para>zh-cn:异步将单个元素从右侧推入 Redis List。</para>
        /// <para>en-us:Asynchronously pushes a single element to the right side of a Redis list.</para>
        /// </summary>
        /// <typeparam name="T">
        /// <para>zh-cn:列表元素类型。</para>
        /// <para>en-us:The list element type.</para>
        /// </typeparam>
        /// <param name="key">
        /// <para>zh-cn:Redis List 键。</para>
        /// <para>en-us:The Redis list key.</para>
        /// </param>
        /// <param name="value">
        /// <para>zh-cn:需要推入列表的元素。</para>
        /// <para>en-us:The element to push into the list.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:表示异步推入操作的任务，结果为推入后的列表长度或 Redis 客户端返回的操作结果。</para>
        /// <para>en-us:A task representing the asynchronous push operation, with the list length after pushing or the Redis client operation result as the result.</para>
        /// </returns>
        Task<long> RightPushAsync<T>(string key, T value);
    }
    /// <summary>
    /// <para>zh-cn:定义 Redis Set 数据结构的缓存操作标准，支持成员添加、成员判断、随机弹出、集合长度、交集、并集、差集和结果存储。</para>
    /// <para>en-us:Defines the cache operation contract for the Redis set data structure, supporting member adds, membership checks, random pops, set length, intersections, unions, differences, and result storage.</para>
    /// </summary>
    public interface ISetCache
    {
        /// <summary>
        /// <para>zh-cn:向指定 Redis Set 添加一个或多个成员。</para>
        /// <para>en-us:Adds one or more members to the specified Redis set.</para>
        /// </summary>
        /// <typeparam name="T">
        /// <para>zh-cn:集合成员类型。</para>
        /// <para>en-us:The set member type.</para>
        /// </typeparam>
        /// <param name="key">
        /// <para>zh-cn:Redis Set 键。</para>
        /// <para>en-us:The Redis set key.</para>
        /// </param>
        /// <param name="value">
        /// <para>zh-cn:需要添加到集合中的成员。</para>
        /// <para>en-us:The members to add to the set.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:成功添加到集合中的新成员数量。</para>
        /// <para>en-us:The number of new members successfully added to the set.</para>
        /// </returns>
        long Add<T>(string key, params T[] value);
        /// <summary>
        /// <para>zh-cn:异步向指定 Redis Set 添加一个或多个成员。</para>
        /// <para>en-us:Asynchronously adds one or more members to the specified Redis set.</para>
        /// </summary>
        /// <typeparam name="T">
        /// <para>zh-cn:集合成员类型。</para>
        /// <para>en-us:The set member type.</para>
        /// </typeparam>
        /// <param name="key">
        /// <para>zh-cn:Redis Set 键。</para>
        /// <para>en-us:The Redis set key.</para>
        /// </param>
        /// <param name="value">
        /// <para>zh-cn:需要添加到集合中的成员。</para>
        /// <para>en-us:The members to add to the set.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:表示异步添加操作的任务，结果为成功添加到集合中的新成员数量。</para>
        /// <para>en-us:A task representing the asynchronous add operation, with the number of new members successfully added to the set as the result.</para>
        /// </returns>
        Task<long> AddAsync<T>(string key, params T[] value);
        /// <summary>
        /// <para>zh-cn:判断指定成员是否存在于 Redis Set 中。</para>
        /// <para>en-us:Determines whether the specified member exists in a Redis set.</para>
        /// </summary>
        /// <typeparam name="T">
        /// <para>zh-cn:集合成员类型。</para>
        /// <para>en-us:The set member type.</para>
        /// </typeparam>
        /// <param name="key">
        /// <para>zh-cn:Redis Set 键。</para>
        /// <para>en-us:The Redis set key.</para>
        /// </param>
        /// <param name="value">
        /// <para>zh-cn:需要判断的成员值。</para>
        /// <para>en-us:The member value to check.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:如果成员存在则返回 `true`；否则返回 `false`。</para>
        /// <para>en-us:Returns `true` when the member exists; otherwise returns `false`.</para>
        /// </returns>
        bool Contains<T>(string key, T value);
        /// <summary>
        /// <para>zh-cn:异步判断指定成员是否存在于 Redis Set 中。</para>
        /// <para>en-us:Asynchronously determines whether the specified member exists in a Redis set.</para>
        /// </summary>
        /// <typeparam name="T">
        /// <para>zh-cn:集合成员类型。</para>
        /// <para>en-us:The set member type.</para>
        /// </typeparam>
        /// <param name="key">
        /// <para>zh-cn:Redis Set 键。</para>
        /// <para>en-us:The Redis set key.</para>
        /// </param>
        /// <param name="value">
        /// <para>zh-cn:需要判断的成员值。</para>
        /// <para>en-us:The member value to check.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:表示异步判断操作的任务，结果表示成员是否存在。</para>
        /// <para>en-us:A task representing the asynchronous membership check, whose result indicates whether the member exists.</para>
        /// </returns>
        Task<bool> ContainsAsync<T>(string key, T value);
        /// <summary>
        /// <para>zh-cn:计算多个 Redis Set 的差集，并反序列化为目标类型集合。</para>
        /// <para>en-us:Computes the difference of multiple Redis sets and deserializes the result to a collection of the target type.</para>
        /// </summary>
        /// <typeparam name="T">
        /// <para>zh-cn:集合成员类型。</para>
        /// <para>en-us:The set member type.</para>
        /// </typeparam>
        /// <param name="keys">
        /// <para>zh-cn:参与差集计算的 Redis Set 键集合。</para>
        /// <para>en-us:The Redis set keys participating in the difference operation.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:差集结果成员集合。</para>
        /// <para>en-us:The members in the difference result.</para>
        /// </returns>
        List<T> Difference<T>(params string[] keys);
        /// <summary>
        /// <para>zh-cn:计算多个 Redis Set 的差集，并将结果存储到目标键。</para>
        /// <para>en-us:Computes the difference of multiple Redis sets and stores the result in the destination key.</para>
        /// </summary>
        /// <param name="destination">
        /// <para>zh-cn:用于保存差集结果的目标 Redis Set 键。</para>
        /// <para>en-us:The destination Redis set key used to store the difference result.</para>
        /// </param>
        /// <param name="keys">
        /// <para>zh-cn:参与差集计算的 Redis Set 键集合。</para>
        /// <para>en-us:The Redis set keys participating in the difference operation.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:存储到目标集合中的成员数量。</para>
        /// <para>en-us:The number of members stored in the destination set.</para>
        /// </returns>
        long DifferenceAndStore(string destination, params string[] keys);
        /// <summary>
        /// <para>zh-cn:异步计算多个 Redis Set 的差集，并将结果存储到目标键。</para>
        /// <para>en-us:Asynchronously computes the difference of multiple Redis sets and stores the result in the destination key.</para>
        /// </summary>
        /// <param name="destination">
        /// <para>zh-cn:用于保存差集结果的目标 Redis Set 键。</para>
        /// <para>en-us:The destination Redis set key used to store the difference result.</para>
        /// </param>
        /// <param name="keys">
        /// <para>zh-cn:参与差集计算的 Redis Set 键集合。</para>
        /// <para>en-us:The Redis set keys participating in the difference operation.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:表示异步存储操作的任务，结果为存储到目标集合中的成员数量。</para>
        /// <para>en-us:A task representing the asynchronous store operation, with the number of members stored in the destination set as the result.</para>
        /// </returns>
        Task<long> DifferenceAndStoreAsync(string destination, params string[] keys);
        /// <summary>
        /// <para>zh-cn:异步计算多个 Redis Set 的差集，并反序列化为目标类型集合。</para>
        /// <para>en-us:Asynchronously computes the difference of multiple Redis sets and deserializes the result to a collection of the target type.</para>
        /// </summary>
        /// <typeparam name="T">
        /// <para>zh-cn:集合成员类型。</para>
        /// <para>en-us:The set member type.</para>
        /// </typeparam>
        /// <param name="keys">
        /// <para>zh-cn:参与差集计算的 Redis Set 键集合。</para>
        /// <para>en-us:The Redis set keys participating in the difference operation.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:表示异步差集计算的任务，结果为差集成员集合。</para>
        /// <para>en-us:A task representing the asynchronous difference operation, with the difference members as the result.</para>
        /// </returns>
        Task<List<T>> DifferenceAsync<T>(params string[] keys);
        /// <summary>
        /// <para>zh-cn:读取指定 Redis Set 中的全部成员。</para>
        /// <para>en-us:Reads all members from the specified Redis set.</para>
        /// </summary>
        /// <typeparam name="T">
        /// <para>zh-cn:集合成员类型。</para>
        /// <para>en-us:The set member type.</para>
        /// </typeparam>
        /// <param name="key">
        /// <para>zh-cn:Redis Set 键。</para>
        /// <para>en-us:The Redis set key.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:集合中的全部成员。</para>
        /// <para>en-us:All members in the set.</para>
        /// </returns>
        List<T> Elements<T>(string key);
        /// <summary>
        /// <para>zh-cn:异步读取指定 Redis Set 中的全部成员。</para>
        /// <para>en-us:Asynchronously reads all members from the specified Redis set.</para>
        /// </summary>
        /// <typeparam name="T">
        /// <para>zh-cn:集合成员类型。</para>
        /// <para>en-us:The set member type.</para>
        /// </typeparam>
        /// <param name="key">
        /// <para>zh-cn:Redis Set 键。</para>
        /// <para>en-us:The Redis set key.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:表示异步读取操作的任务，结果为集合中的全部成员。</para>
        /// <para>en-us:A task representing the asynchronous read operation, with all members in the set as the result.</para>
        /// </returns>
        Task<List<T>> ElementsAsync<T>(string key);
        /// <summary>
        /// <para>zh-cn:计算多个 Redis Set 的交集，并反序列化为目标类型集合。</para>
        /// <para>en-us:Computes the intersection of multiple Redis sets and deserializes the result to a collection of the target type.</para>
        /// </summary>
        /// <typeparam name="T">
        /// <para>zh-cn:集合成员类型。</para>
        /// <para>en-us:The set member type.</para>
        /// </typeparam>
        /// <param name="keys">
        /// <para>zh-cn:参与交集计算的 Redis Set 键集合。</para>
        /// <para>en-us:The Redis set keys participating in the intersection operation.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:交集结果成员集合。</para>
        /// <para>en-us:The members in the intersection result.</para>
        /// </returns>
        List<T> Intersect<T>(params string[] keys);
        /// <summary>
        /// <para>zh-cn:计算多个 Redis Set 的交集，并将结果存储到目标键。</para>
        /// <para>en-us:Computes the intersection of multiple Redis sets and stores the result in the destination key.</para>
        /// </summary>
        /// <param name="destination">
        /// <para>zh-cn:用于保存交集结果的目标 Redis Set 键。</para>
        /// <para>en-us:The destination Redis set key used to store the intersection result.</para>
        /// </param>
        /// <param name="keys">
        /// <para>zh-cn:参与交集计算的 Redis Set 键集合。</para>
        /// <para>en-us:The Redis set keys participating in the intersection operation.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:存储到目标集合中的成员数量。</para>
        /// <para>en-us:The number of members stored in the destination set.</para>
        /// </returns>
        long IntersectAndStore(string destination, params string[] keys);
        /// <summary>
        /// <para>zh-cn:异步计算多个 Redis Set 的交集，并将结果存储到目标键。</para>
        /// <para>en-us:Asynchronously computes the intersection of multiple Redis sets and stores the result in the destination key.</para>
        /// </summary>
        /// <param name="destination">
        /// <para>zh-cn:用于保存交集结果的目标 Redis Set 键。</para>
        /// <para>en-us:The destination Redis set key used to store the intersection result.</para>
        /// </param>
        /// <param name="keys">
        /// <para>zh-cn:参与交集计算的 Redis Set 键集合。</para>
        /// <para>en-us:The Redis set keys participating in the intersection operation.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:表示异步存储操作的任务，结果为存储到目标集合中的成员数量。</para>
        /// <para>en-us:A task representing the asynchronous store operation, with the number of members stored in the destination set as the result.</para>
        /// </returns>
        Task<long> IntersectAndStoreAsync(string destination, params string[] keys);
        /// <summary>
        /// <para>zh-cn:异步计算多个 Redis Set 的交集，并反序列化为目标类型集合。</para>
        /// <para>en-us:Asynchronously computes the intersection of multiple Redis sets and deserializes the result to a collection of the target type.</para>
        /// </summary>
        /// <typeparam name="T">
        /// <para>zh-cn:集合成员类型。</para>
        /// <para>en-us:The set member type.</para>
        /// </typeparam>
        /// <param name="keys">
        /// <para>zh-cn:参与交集计算的 Redis Set 键集合。</para>
        /// <para>en-us:The Redis set keys participating in the intersection operation.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:表示异步交集计算的任务，结果为交集成员集合。</para>
        /// <para>en-us:A task representing the asynchronous intersection operation, with the intersection members as the result.</para>
        /// </returns>
        Task<List<T>> IntersectAsync<T>(params string[] keys);
        /// <summary>
        /// <para>zh-cn:获取 Redis Set 的成员数量。</para>
        /// <para>en-us:Gets the number of members in a Redis set.</para>
        /// </summary>
        /// <param name="key">
        /// <para>zh-cn:Redis Set 键。</para>
        /// <para>en-us:The Redis set key.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:集合中的成员数量。</para>
        /// <para>en-us:The number of members in the set.</para>
        /// </returns>
        long Length(string key);
        /// <summary>
        /// <para>zh-cn:异步获取 Redis Set 的成员数量。</para>
        /// <para>en-us:Asynchronously gets the number of members in a Redis set.</para>
        /// </summary>
        /// <param name="key">
        /// <para>zh-cn:Redis Set 键。</para>
        /// <para>en-us:The Redis set key.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:表示异步读取操作的任务，结果为集合中的成员数量。</para>
        /// <para>en-us:A task representing the asynchronous read operation, with the number of members in the set as the result.</para>
        /// </returns>
        Task<long> LengthAsync(string key);
        /// <summary>
        /// <para>zh-cn:从 Redis Set 中随机弹出并移除一个成员。</para>
        /// <para>en-us:Randomly pops and removes one member from a Redis set.</para>
        /// </summary>
        /// <typeparam name="T">
        /// <para>zh-cn:集合成员类型。</para>
        /// <para>en-us:The set member type.</para>
        /// </typeparam>
        /// <param name="key">
        /// <para>zh-cn:Redis Set 键。</para>
        /// <para>en-us:The Redis set key.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:被弹出的成员；当集合为空或键不存在时，返回值由实现决定。</para>
        /// <para>en-us:The popped member; when the set is empty or the key does not exist, the return value is determined by the implementation.</para>
        /// </returns>
        T Pop<T>(string key);
        /// <summary>
        /// <para>zh-cn:异步从 Redis Set 中随机弹出并移除一个成员。</para>
        /// <para>en-us:Asynchronously pops and removes one random member from a Redis set.</para>
        /// </summary>
        /// <typeparam name="T">
        /// <para>zh-cn:集合成员类型。</para>
        /// <para>en-us:The set member type.</para>
        /// </typeparam>
        /// <param name="key">
        /// <para>zh-cn:Redis Set 键。</para>
        /// <para>en-us:The Redis set key.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:表示异步弹出操作的任务，结果为被弹出的成员。</para>
        /// <para>en-us:A task representing the asynchronous pop operation, with the popped member as the result.</para>
        /// </returns>
        Task<T> PopAsync<T>(string key);
        /// <summary>
        /// <para>zh-cn:从 Redis Set 中随机读取一个成员但不移除。</para>
        /// <para>en-us:Randomly reads one member from a Redis set without removing it.</para>
        /// </summary>
        /// <typeparam name="T">
        /// <para>zh-cn:集合成员类型。</para>
        /// <para>en-us:The set member type.</para>
        /// </typeparam>
        /// <param name="key">
        /// <para>zh-cn:Redis Set 键。</para>
        /// <para>en-us:The Redis set key.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:随机读取到的成员；当集合为空或键不存在时，返回值由实现决定。</para>
        /// <para>en-us:The randomly read member; when the set is empty or the key does not exist, the return value is determined by the implementation.</para>
        /// </returns>
        T Random<T>(string key);
        /// <summary>
        /// <para>zh-cn:异步从 Redis Set 中随机读取一个成员但不移除。</para>
        /// <para>en-us:Asynchronously reads one random member from a Redis set without removing it.</para>
        /// </summary>
        /// <typeparam name="T">
        /// <para>zh-cn:集合成员类型。</para>
        /// <para>en-us:The set member type.</para>
        /// </typeparam>
        /// <param name="key">
        /// <para>zh-cn:Redis Set 键。</para>
        /// <para>en-us:The Redis set key.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:表示异步随机读取操作的任务，结果为随机读取到的成员。</para>
        /// <para>en-us:A task representing the asynchronous random read operation, with the randomly read member as the result.</para>
        /// </returns>
        Task<T> RandomAsync<T>(string key);
        /// <summary>
        /// <para>zh-cn:从 Redis Set 中移除一个或多个成员。</para>
        /// <para>en-us:Removes one or more members from a Redis set.</para>
        /// </summary>
        /// <typeparam name="T">
        /// <para>zh-cn:集合成员类型。</para>
        /// <para>en-us:The set member type.</para>
        /// </typeparam>
        /// <param name="key">
        /// <para>zh-cn:Redis Set 键。</para>
        /// <para>en-us:The Redis set key.</para>
        /// </param>
        /// <param name="value">
        /// <para>zh-cn:需要移除的成员。</para>
        /// <para>en-us:The members to remove.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:成功移除的成员数量。</para>
        /// <para>en-us:The number of members successfully removed.</para>
        /// </returns>
        long Remove<T>(string key, params T[] value);
        /// <summary>
        /// <para>zh-cn:异步从 Redis Set 中移除一个或多个成员。</para>
        /// <para>en-us:Asynchronously removes one or more members from a Redis set.</para>
        /// </summary>
        /// <typeparam name="T">
        /// <para>zh-cn:集合成员类型。</para>
        /// <para>en-us:The set member type.</para>
        /// </typeparam>
        /// <param name="key">
        /// <para>zh-cn:Redis Set 键。</para>
        /// <para>en-us:The Redis set key.</para>
        /// </param>
        /// <param name="value">
        /// <para>zh-cn:需要移除的成员。</para>
        /// <para>en-us:The members to remove.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:表示异步移除操作的任务，结果为成功移除的成员数量。</para>
        /// <para>en-us:A task representing the asynchronous remove operation, with the number of members successfully removed as the result.</para>
        /// </returns>
        Task<long> RemoveAsync<T>(string key, params T[] value);
        /// <summary>
        /// <para>zh-cn:计算多个 Redis Set 的并集，并反序列化为目标类型集合。</para>
        /// <para>en-us:Computes the union of multiple Redis sets and deserializes the result to a collection of the target type.</para>
        /// </summary>
        /// <typeparam name="T">
        /// <para>zh-cn:集合成员类型。</para>
        /// <para>en-us:The set member type.</para>
        /// </typeparam>
        /// <param name="keys">
        /// <para>zh-cn:参与并集计算的 Redis Set 键集合。</para>
        /// <para>en-us:The Redis set keys participating in the union operation.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:并集结果成员集合。</para>
        /// <para>en-us:The members in the union result.</para>
        /// </returns>
        List<T> Union<T>(params string[] keys);
        /// <summary>
        /// <para>zh-cn:异步计算多个 Redis Set 的并集，并反序列化为目标类型集合。</para>
        /// <para>en-us:Asynchronously computes the union of multiple Redis sets and deserializes the result to a collection of the target type.</para>
        /// </summary>
        /// <typeparam name="T">
        /// <para>zh-cn:集合成员类型。</para>
        /// <para>en-us:The set member type.</para>
        /// </typeparam>
        /// <param name="keys">
        /// <para>zh-cn:参与并集计算的 Redis Set 键集合。</para>
        /// <para>en-us:The Redis set keys participating in the union operation.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:表示异步并集计算的任务，结果为并集成员集合。</para>
        /// <para>en-us:A task representing the asynchronous union operation, with the union members as the result.</para>
        /// </returns>
        Task<List<T>> UnionAsync<T>(params string[] keys);
        /// <summary>
        /// <para>zh-cn:计算多个 Redis Set 的并集，并将结果存储到目标键。</para>
        /// <para>en-us:Computes the union of multiple Redis sets and stores the result in the destination key.</para>
        /// </summary>
        /// <param name="destination">
        /// <para>zh-cn:用于保存并集结果的目标 Redis Set 键。</para>
        /// <para>en-us:The destination Redis set key used to store the union result.</para>
        /// </param>
        /// <param name="keys">
        /// <para>zh-cn:参与并集计算的 Redis Set 键集合。</para>
        /// <para>en-us:The Redis set keys participating in the union operation.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:存储到目标集合中的成员数量。</para>
        /// <para>en-us:The number of members stored in the destination set.</para>
        /// </returns>
        long UnionAndStore(string destination, params string[] keys);
        /// <summary>
        /// <para>zh-cn:异步计算多个 Redis Set 的并集，并将结果存储到目标键。</para>
        /// <para>en-us:Asynchronously computes the union of multiple Redis sets and stores the result in the destination key.</para>
        /// </summary>
        /// <param name="destination">
        /// <para>zh-cn:用于保存并集结果的目标 Redis Set 键。</para>
        /// <para>en-us:The destination Redis set key used to store the union result.</para>
        /// </param>
        /// <param name="keys">
        /// <para>zh-cn:参与并集计算的 Redis Set 键集合。</para>
        /// <para>en-us:The Redis set keys participating in the union operation.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:表示异步存储操作的任务，结果为存储到目标集合中的成员数量。</para>
        /// <para>en-us:A task representing the asynchronous store operation, with the number of members stored in the destination set as the result.</para>
        /// </returns>
        Task<long> UnionAndStoreAsync(string destination, params string[] keys);
        
    }
    /// <summary>
    /// <para>zh-cn:定义 Redis Sorted Set 数据结构的缓存操作标准，支持带分值成员写入、分值增减、按排名/分值/值范围查询和批量删除。</para>
    /// <para>en-us:Defines the cache operation contract for the Redis sorted set data structure, supporting scored member writes, score increments and decrements, rank/score/value range queries, and batch removal.</para>
    /// </summary>
    public interface ISortedSetCache
    {
        /// <summary>
        /// <para>zh-cn:向指定 Redis Sorted Set 批量添加成员，并为成员设置统一分值。</para>
        /// <para>en-us:Adds members to the specified Redis sorted set in batch and assigns a shared score to them.</para>
        /// </summary>
        /// <typeparam name="T"><para>zh-cn:有序集合成员类型。</para><para>en-us:The sorted set member type.</para></typeparam>
        /// <param name="key"><para>zh-cn:Redis Sorted Set 键。</para><para>en-us:The Redis sorted set key.</para></param>
        /// <param name="value"><para>zh-cn:需要添加的成员集合。</para><para>en-us:The members to add.</para></param>
        /// <param name="score"><para>zh-cn:成员分值；为 `null` 时由实现决定默认分值。</para><para>en-us:The member score. When `null`, the default score is determined by the implementation.</para></param>
        /// <returns><para>zh-cn:成功添加的新成员数量。</para><para>en-us:The number of new members successfully added.</para></returns>
        long Add<T>(string key, List<T> value, double? score = null);
        /// <summary>
        /// <para>zh-cn:向指定 Redis Sorted Set 添加单个成员，并可选设置分值。</para>
        /// <para>en-us:Adds a single member to the specified Redis sorted set and optionally assigns a score.</para>
        /// </summary>
        /// <typeparam name="T"><para>zh-cn:有序集合成员类型。</para><para>en-us:The sorted set member type.</para></typeparam>
        /// <param name="key"><para>zh-cn:Redis Sorted Set 键。</para><para>en-us:The Redis sorted set key.</para></param>
        /// <param name="value"><para>zh-cn:需要添加的成员。</para><para>en-us:The member to add.</para></param>
        /// <param name="score"><para>zh-cn:成员分值；为 `null` 时由实现决定默认分值。</para><para>en-us:The member score. When `null`, the default score is determined by the implementation.</para></param>
        /// <returns><para>zh-cn:如果成员被成功添加或更新则返回 `true`；否则返回 `false`。</para><para>en-us:Returns `true` when the member is successfully added or updated; otherwise returns `false`.</para></returns>
        bool Add<T>(string key, T value, double? score = null);
        /// <summary>
        /// <para>zh-cn:异步向指定 Redis Sorted Set 批量添加成员，并为成员设置统一分值。</para>
        /// <para>en-us:Asynchronously adds members to the specified Redis sorted set in batch and assigns a shared score to them.</para>
        /// </summary>
        /// <typeparam name="T"><para>zh-cn:有序集合成员类型。</para><para>en-us:The sorted set member type.</para></typeparam>
        /// <param name="key"><para>zh-cn:Redis Sorted Set 键。</para><para>en-us:The Redis sorted set key.</para></param>
        /// <param name="value"><para>zh-cn:需要添加的成员集合。</para><para>en-us:The members to add.</para></param>
        /// <param name="score"><para>zh-cn:成员分值；为 `null` 时由实现决定默认分值。</para><para>en-us:The member score. When `null`, the default score is determined by the implementation.</para></param>
        /// <returns><para>zh-cn:表示异步添加操作的任务，结果为成功添加的新成员数量。</para><para>en-us:A task representing the asynchronous add operation, with the number of new members successfully added as the result.</para></returns>
        Task<long> AddAsync<T>(string key, List<T> value, double? score = null);
        /// <summary>
        /// <para>zh-cn:异步向指定 Redis Sorted Set 添加单个成员，并可选设置分值。</para>
        /// <para>en-us:Asynchronously adds a single member to the specified Redis sorted set and optionally assigns a score.</para>
        /// </summary>
        /// <typeparam name="T"><para>zh-cn:有序集合成员类型。</para><para>en-us:The sorted set member type.</para></typeparam>
        /// <param name="key"><para>zh-cn:Redis Sorted Set 键。</para><para>en-us:The Redis sorted set key.</para></param>
        /// <param name="value"><para>zh-cn:需要添加的成员。</para><para>en-us:The member to add.</para></param>
        /// <param name="score"><para>zh-cn:成员分值；为 `null` 时由实现决定默认分值。</para><para>en-us:The member score. When `null`, the default score is determined by the implementation.</para></param>
        /// <returns><para>zh-cn:表示异步添加操作的任务，结果表示成员是否成功添加或更新。</para><para>en-us:A task representing the asynchronous add operation, whose result indicates whether the member was successfully added or updated.</para></returns>
        Task<bool> AddAsync<T>(string key, T value, double? score = null);
        /// <summary>
        /// <para>zh-cn:计算多个 Redis Sorted Set 的交集，并将结果存储到目标键。</para>
        /// <para>en-us:Computes the intersection of multiple Redis sorted sets and stores the result in the destination key.</para>
        /// </summary>
        /// <param name="destination"><para>zh-cn:用于保存交集结果的目标 Redis Sorted Set 键。</para><para>en-us:The destination Redis sorted set key used to store the intersection result.</para></param>
        /// <param name="keys"><para>zh-cn:参与交集计算的 Redis Sorted Set 键集合。</para><para>en-us:The Redis sorted set keys participating in the intersection operation.</para></param>
        /// <returns><para>zh-cn:存储到目标有序集合中的成员数量。</para><para>en-us:The number of members stored in the destination sorted set.</para></returns>
        long CombineIntersectAndStore(string destination, params string[] keys);
        /// <summary>
        /// <para>zh-cn:异步计算多个 Redis Sorted Set 的交集，并将结果存储到目标键。</para>
        /// <para>en-us:Asynchronously computes the intersection of multiple Redis sorted sets and stores the result in the destination key.</para>
        /// </summary>
        /// <param name="destination"><para>zh-cn:用于保存交集结果的目标 Redis Sorted Set 键。</para><para>en-us:The destination Redis sorted set key used to store the intersection result.</para></param>
        /// <param name="keys"><para>zh-cn:参与交集计算的 Redis Sorted Set 键集合。</para><para>en-us:The Redis sorted set keys participating in the intersection operation.</para></param>
        /// <returns><para>zh-cn:表示异步存储操作的任务，结果为存储到目标有序集合中的成员数量。</para><para>en-us:A task representing the asynchronous store operation, with the number of members stored in the destination sorted set as the result.</para></returns>
        Task<long> CombineIntersectAndStoreAsync(string destination, params string[] keys);
        /// <summary>
        /// <para>zh-cn:计算多个 Redis Sorted Set 的并集，并将结果存储到目标键。</para>
        /// <para>en-us:Computes the union of multiple Redis sorted sets and stores the result in the destination key.</para>
        /// </summary>
        /// <param name="destination"><para>zh-cn:用于保存并集结果的目标 Redis Sorted Set 键。</para><para>en-us:The destination Redis sorted set key used to store the union result.</para></param>
        /// <param name="keys"><para>zh-cn:参与并集计算的 Redis Sorted Set 键集合。</para><para>en-us:The Redis sorted set keys participating in the union operation.</para></param>
        /// <returns><para>zh-cn:存储到目标有序集合中的成员数量。</para><para>en-us:The number of members stored in the destination sorted set.</para></returns>
        long CombineUnionAndStore(string destination, params string[] keys);
        /// <summary>
        /// <para>zh-cn:异步计算多个 Redis Sorted Set 的并集，并将结果存储到目标键。</para>
        /// <para>en-us:Asynchronously computes the union of multiple Redis sorted sets and stores the result in the destination key.</para>
        /// </summary>
        /// <param name="destination"><para>zh-cn:用于保存并集结果的目标 Redis Sorted Set 键。</para><para>en-us:The destination Redis sorted set key used to store the union result.</para></param>
        /// <param name="keys"><para>zh-cn:参与并集计算的 Redis Sorted Set 键集合。</para><para>en-us:The Redis sorted set keys participating in the union operation.</para></param>
        /// <returns><para>zh-cn:表示异步存储操作的任务，结果为存储到目标有序集合中的成员数量。</para><para>en-us:A task representing the asynchronous store operation, with the number of members stored in the destination sorted set as the result.</para></returns>
        Task<long> CombineUnionAndStoreAsync(string destination, params string[] keys);
        /// <summary>
        /// <para>zh-cn:减少 Redis Sorted Set 中指定成员的分值。</para>
        /// <para>en-us:Decrements the score of the specified member in a Redis sorted set.</para>
        /// </summary>
        /// <typeparam name="T"><para>zh-cn:有序集合成员类型。</para><para>en-us:The sorted set member type.</para></typeparam>
        /// <param name="key"><para>zh-cn:Redis Sorted Set 键。</para><para>en-us:The Redis sorted set key.</para></param>
        /// <param name="value"><para>zh-cn:需要调整分值的成员。</para><para>en-us:The member whose score should be adjusted.</para></param>
        /// <param name="scores"><para>zh-cn:需要减少的分值。</para><para>en-us:The score amount to decrement.</para></param>
        /// <returns><para>zh-cn:调整后的成员分值。</para><para>en-us:The adjusted member score.</para></returns>
        double Decrement<T>(string key, T value, double scores);
        /// <summary>
        /// <para>zh-cn:异步减少 Redis Sorted Set 中指定成员的分值。</para>
        /// <para>en-us:Asynchronously decrements the score of the specified member in a Redis sorted set.</para>
        /// </summary>
        /// <typeparam name="T"><para>zh-cn:有序集合成员类型。</para><para>en-us:The sorted set member type.</para></typeparam>
        /// <param name="key"><para>zh-cn:Redis Sorted Set 键。</para><para>en-us:The Redis sorted set key.</para></param>
        /// <param name="value"><para>zh-cn:需要调整分值的成员。</para><para>en-us:The member whose score should be adjusted.</para></param>
        /// <param name="scores"><para>zh-cn:需要减少的分值。</para><para>en-us:The score amount to decrement.</para></param>
        /// <returns><para>zh-cn:表示异步分值调整操作的任务，结果为调整后的成员分值。</para><para>en-us:A task representing the asynchronous score adjustment operation, with the adjusted member score as the result.</para></returns>
        Task<double> DecrementAsync<T>(string key, T value, double scores);
        /// <summary>
        /// <para>zh-cn:增加 Redis Sorted Set 中指定成员的分值。</para>
        /// <para>en-us:Increments the score of the specified member in a Redis sorted set.</para>
        /// </summary>
        /// <typeparam name="T"><para>zh-cn:有序集合成员类型。</para><para>en-us:The sorted set member type.</para></typeparam>
        /// <param name="key"><para>zh-cn:Redis Sorted Set 键。</para><para>en-us:The Redis sorted set key.</para></param>
        /// <param name="value"><para>zh-cn:需要调整分值的成员。</para><para>en-us:The member whose score should be adjusted.</para></param>
        /// <param name="scores"><para>zh-cn:需要增加的分值。</para><para>en-us:The score amount to increment.</para></param>
        /// <returns><para>zh-cn:调整后的成员分值。</para><para>en-us:The adjusted member score.</para></returns>
        double Increment<T>(string key, T value, double scores);
        /// <summary>
        /// <para>zh-cn:异步增加 Redis Sorted Set 中指定成员的分值。</para>
        /// <para>en-us:Asynchronously increments the score of the specified member in a Redis sorted set.</para>
        /// </summary>
        /// <typeparam name="T"><para>zh-cn:有序集合成员类型。</para><para>en-us:The sorted set member type.</para></typeparam>
        /// <param name="key"><para>zh-cn:Redis Sorted Set 键。</para><para>en-us:The Redis sorted set key.</para></param>
        /// <param name="value"><para>zh-cn:需要调整分值的成员。</para><para>en-us:The member whose score should be adjusted.</para></param>
        /// <param name="scores"><para>zh-cn:需要增加的分值。</para><para>en-us:The score amount to increment.</para></param>
        /// <returns><para>zh-cn:表示异步分值调整操作的任务，结果为调整后的成员分值。</para><para>en-us:A task representing the asynchronous score adjustment operation, with the adjusted member score as the result.</para></returns>
        Task<double> IncrementAsync<T>(string key, T value, double scores);
        /// <summary>
        /// <para>zh-cn:获取 Redis Sorted Set 的成员数量。</para>
        /// <para>en-us:Gets the number of members in a Redis sorted set.</para>
        /// </summary>
        /// <param name="key"><para>zh-cn:Redis Sorted Set 键。</para><para>en-us:The Redis sorted set key.</para></param>
        /// <returns><para>zh-cn:有序集合中的成员数量。</para><para>en-us:The number of members in the sorted set.</para></returns>
        long Length(string key);
        /// <summary>
        /// <para>zh-cn:异步获取 Redis Sorted Set 的成员数量。</para>
        /// <para>en-us:Asynchronously gets the number of members in a Redis sorted set.</para>
        /// </summary>
        /// <param name="key"><para>zh-cn:Redis Sorted Set 键。</para><para>en-us:The Redis sorted set key.</para></param>
        /// <returns><para>zh-cn:表示异步读取操作的任务，结果为有序集合中的成员数量。</para><para>en-us:A task representing the asynchronous read operation, with the number of members in the sorted set as the result.</para></returns>
        Task<long> LengthAsync(string key);
        /// <summary>
        /// <para>zh-cn:按成员值范围统计 Redis Sorted Set 的成员数量。</para>
        /// <para>en-us:Counts Redis sorted set members by member value range.</para>
        /// </summary>
        /// <typeparam name="T"><para>zh-cn:有序集合成员类型。</para><para>en-us:The sorted set member type.</para></typeparam>
        /// <param name="key"><para>zh-cn:Redis Sorted Set 键。</para><para>en-us:The Redis sorted set key.</para></param>
        /// <param name="startValue"><para>zh-cn:成员值范围起点。</para><para>en-us:The start of the member value range.</para></param>
        /// <param name="endValue"><para>zh-cn:成员值范围终点。</para><para>en-us:The end of the member value range.</para></param>
        /// <returns><para>zh-cn:指定成员值范围内的成员数量。</para><para>en-us:The number of members in the specified member value range.</para></returns>
        long LengthByValue<T>(string key, T startValue, T endValue);
        /// <summary>
        /// <para>zh-cn:异步按成员值范围统计 Redis Sorted Set 的成员数量。</para>
        /// <para>en-us:Asynchronously counts Redis sorted set members by member value range.</para>
        /// </summary>
        /// <typeparam name="T"><para>zh-cn:有序集合成员类型。</para><para>en-us:The sorted set member type.</para></typeparam>
        /// <param name="key"><para>zh-cn:Redis Sorted Set 键。</para><para>en-us:The Redis sorted set key.</para></param>
        /// <param name="startValue"><para>zh-cn:成员值范围起点。</para><para>en-us:The start of the member value range.</para></param>
        /// <param name="endValue"><para>zh-cn:成员值范围终点。</para><para>en-us:The end of the member value range.</para></param>
        /// <returns><para>zh-cn:表示异步统计操作的任务，结果为指定成员值范围内的成员数量。</para><para>en-us:A task representing the asynchronous count operation, with the number of members in the specified value range as the result.</para></returns>
        Task<long> LengthByValueAsync<T>(string key, T startValue, T endValue);
        /// <summary>
        /// <para>zh-cn:获取 Redis Sorted Set 中的最大分值。</para>
        /// <para>en-us:Gets the maximum score in a Redis sorted set.</para>
        /// </summary>
        /// <param name="key"><para>zh-cn:Redis Sorted Set 键。</para><para>en-us:The Redis sorted set key.</para></param>
        /// <returns><para>zh-cn:有序集合中的最大分值。</para><para>en-us:The maximum score in the sorted set.</para></returns>
        double MaxScore(string key);
        /// <summary>
        /// <para>zh-cn:异步获取 Redis Sorted Set 中的最大分值。</para>
        /// <para>en-us:Asynchronously gets the maximum score in a Redis sorted set.</para>
        /// </summary>
        /// <param name="key"><para>zh-cn:Redis Sorted Set 键。</para><para>en-us:The Redis sorted set key.</para></param>
        /// <returns><para>zh-cn:表示异步读取操作的任务，结果为有序集合中的最大分值。</para><para>en-us:A task representing the asynchronous read operation, with the maximum score in the sorted set as the result.</para></returns>
        Task<double> MaxScoreAsync(string key);
        /// <summary>
        /// <para>zh-cn:获取 Redis Sorted Set 中的最小分值。</para>
        /// <para>en-us:Gets the minimum score in a Redis sorted set.</para>
        /// </summary>
        /// <param name="key"><para>zh-cn:Redis Sorted Set 键。</para><para>en-us:The Redis sorted set key.</para></param>
        /// <returns><para>zh-cn:有序集合中的最小分值；集合为空时返回值由实现决定。</para><para>en-us:The minimum score in the sorted set; when the set is empty, the return value is determined by the implementation.</para></returns>
        double? MinScore(string key);
        /// <summary>
        /// <para>zh-cn:异步获取 Redis Sorted Set 中的最小分值。</para>
        /// <para>en-us:Asynchronously gets the minimum score in a Redis sorted set.</para>
        /// </summary>
        /// <param name="key"><para>zh-cn:Redis Sorted Set 键。</para><para>en-us:The Redis sorted set key.</para></param>
        /// <returns><para>zh-cn:表示异步读取操作的任务，结果为有序集合中的最小分值。</para><para>en-us:A task representing the asynchronous read operation, with the minimum score in the sorted set as the result.</para></returns>
        Task<double> MinScoreAsync(string key);
        /// <summary>
        /// <para>zh-cn:按排名范围读取 Redis Sorted Set 成员。</para>
        /// <para>en-us:Reads Redis sorted set members by rank range.</para>
        /// </summary>
        /// <typeparam name="T"><para>zh-cn:有序集合成员类型。</para><para>en-us:The sorted set member type.</para></typeparam>
        /// <param name="key"><para>zh-cn:Redis Sorted Set 键。</para><para>en-us:The Redis sorted set key.</para></param>
        /// <param name="start"><para>zh-cn:排名范围起点。</para><para>en-us:The start rank.</para></param>
        /// <param name="stop"><para>zh-cn:排名范围终点。</para><para>en-us:The stop rank.</para></param>
        /// <param name="desc"><para>zh-cn:是否按降序读取。</para><para>en-us:Whether to read in descending order.</para></param>
        /// <returns><para>zh-cn:指定排名范围内的成员集合。</para><para>en-us:The members in the specified rank range.</para></returns>
        List<T> RangeByRank<T>(string key, long start = 0, long stop = -1, bool desc = false);
        /// <summary>
        /// <para>zh-cn:异步按排名范围读取 Redis Sorted Set 成员。</para>
        /// <para>en-us:Asynchronously reads Redis sorted set members by rank range.</para>
        /// </summary>
        /// <typeparam name="T"><para>zh-cn:有序集合成员类型。</para><para>en-us:The sorted set member type.</para></typeparam>
        /// <param name="key"><para>zh-cn:Redis Sorted Set 键。</para><para>en-us:The Redis sorted set key.</para></param>
        /// <param name="start"><para>zh-cn:排名范围起点。</para><para>en-us:The start rank.</para></param>
        /// <param name="stop"><para>zh-cn:排名范围终点。</para><para>en-us:The stop rank.</para></param>
        /// <param name="desc"><para>zh-cn:是否按降序读取。</para><para>en-us:Whether to read in descending order.</para></param>
        /// <returns><para>zh-cn:表示异步读取操作的任务，结果为指定排名范围内的成员集合。</para><para>en-us:A task representing the asynchronous read operation, with the members in the specified rank range as the result.</para></returns>
        Task<List<T>> RangeByRankAsync<T>(string key, long start = 0, long stop = -1, bool desc = false);
        /// <summary>
        /// <para>zh-cn:按排名范围读取 Redis Sorted Set 成员及其分值。</para>
        /// <para>en-us:Reads Redis sorted set members and their scores by rank range.</para>
        /// </summary>
        /// <typeparam name="T"><para>zh-cn:有序集合成员类型。</para><para>en-us:The sorted set member type.</para></typeparam>
        /// <param name="key"><para>zh-cn:Redis Sorted Set 键。</para><para>en-us:The Redis sorted set key.</para></param>
        /// <param name="start"><para>zh-cn:排名范围起点。</para><para>en-us:The start rank.</para></param>
        /// <param name="stop"><para>zh-cn:排名范围终点。</para><para>en-us:The stop rank.</para></param>
        /// <param name="desc"><para>zh-cn:是否按降序读取。</para><para>en-us:Whether to read in descending order.</para></param>
        /// <returns><para>zh-cn:成员到分值的映射结果。</para><para>en-us:A mapping from members to their scores.</para></returns>
        Dictionary<T, double> RangeByRankWithScores<T>(string key, long start = 0, long stop = -1, bool desc = false) where T : class, new();
        /// <summary>
        /// <para>zh-cn:异步按排名范围读取 Redis Sorted Set 成员及其分值。</para>
        /// <para>en-us:Asynchronously reads Redis sorted set members and their scores by rank range.</para>
        /// </summary>
        /// <typeparam name="T"><para>zh-cn:有序集合成员类型。</para><para>en-us:The sorted set member type.</para></typeparam>
        /// <param name="key"><para>zh-cn:Redis Sorted Set 键。</para><para>en-us:The Redis sorted set key.</para></param>
        /// <param name="start"><para>zh-cn:排名范围起点。</para><para>en-us:The start rank.</para></param>
        /// <param name="stop"><para>zh-cn:排名范围终点。</para><para>en-us:The stop rank.</para></param>
        /// <param name="desc"><para>zh-cn:是否按降序读取。</para><para>en-us:Whether to read in descending order.</para></param>
        /// <returns><para>zh-cn:表示异步读取操作的任务，结果为成员到分值的映射。</para><para>en-us:A task representing the asynchronous read operation, with a mapping from members to scores as the result.</para></returns>
        Task<Dictionary<T, double>> RangeByRankWithScoresAsync<T>(string key, long start = 0, long stop = -1, bool desc = false);
        /// <summary>
        /// <para>zh-cn:按分值范围读取 Redis Sorted Set 成员。</para>
        /// <para>en-us:Reads Redis sorted set members by score range.</para>
        /// </summary>
        /// <typeparam name="T"><para>zh-cn:有序集合成员类型。</para><para>en-us:The sorted set member type.</para></typeparam>
        /// <param name="key"><para>zh-cn:Redis Sorted Set 键。</para><para>en-us:The Redis sorted set key.</para></param>
        /// <param name="start"><para>zh-cn:分值范围起点。</para><para>en-us:The start score.</para></param>
        /// <param name="stop"><para>zh-cn:分值范围终点。</para><para>en-us:The stop score.</para></param>
        /// <param name="desc"><para>zh-cn:是否按降序读取。</para><para>en-us:Whether to read in descending order.</para></param>
        /// <returns><para>zh-cn:指定分值范围内的成员集合。</para><para>en-us:The members in the specified score range.</para></returns>
        List<T> RangeByScore<T>(string key, double start = 0, double stop = -1, bool desc = false);
        /// <summary>
        /// <para>zh-cn:异步按分值范围读取 Redis Sorted Set 成员。</para>
        /// <para>en-us:Asynchronously reads Redis sorted set members by score range.</para>
        /// </summary>
        /// <typeparam name="T"><para>zh-cn:有序集合成员类型。</para><para>en-us:The sorted set member type.</para></typeparam>
        /// <param name="key"><para>zh-cn:Redis Sorted Set 键。</para><para>en-us:The Redis sorted set key.</para></param>
        /// <param name="start"><para>zh-cn:分值范围起点。</para><para>en-us:The start score.</para></param>
        /// <param name="stop"><para>zh-cn:分值范围终点。</para><para>en-us:The stop score.</para></param>
        /// <param name="desc"><para>zh-cn:是否按降序读取。</para><para>en-us:Whether to read in descending order.</para></param>
        /// <returns><para>zh-cn:表示异步读取操作的任务，结果为指定分值范围内的成员集合。</para><para>en-us:A task representing the asynchronous read operation, with the members in the specified score range as the result.</para></returns>
        Task<List<T>> RangeByScoreAsync<T>(string key, double start = 0, double stop = -1, bool desc = false);
        /// <summary>
        /// <para>zh-cn:按分值范围读取 Redis Sorted Set 成员及其分值。</para>
        /// <para>en-us:Reads Redis sorted set members and their scores by score range.</para>
        /// </summary>
        /// <typeparam name="T"><para>zh-cn:有序集合成员类型。</para><para>en-us:The sorted set member type.</para></typeparam>
        /// <param name="key"><para>zh-cn:Redis Sorted Set 键。</para><para>en-us:The Redis sorted set key.</para></param>
        /// <param name="start"><para>zh-cn:分值范围起点。</para><para>en-us:The start score.</para></param>
        /// <param name="stop"><para>zh-cn:分值范围终点。</para><para>en-us:The stop score.</para></param>
        /// <param name="desc"><para>zh-cn:是否按降序读取。</para><para>en-us:Whether to read in descending order.</para></param>
        /// <returns><para>zh-cn:成员到分值的映射结果。</para><para>en-us:A mapping from members to their scores.</para></returns>
        Dictionary<T, double> RangeByScoreWithScores<T>(string key, double start = 0, double stop = -1, bool desc = false);
        /// <summary>
        /// <para>zh-cn:异步按分值范围读取 Redis Sorted Set 成员及其分值。</para>
        /// <para>en-us:Asynchronously reads Redis sorted set members and their scores by score range.</para>
        /// </summary>
        /// <typeparam name="T"><para>zh-cn:有序集合成员类型。</para><para>en-us:The sorted set member type.</para></typeparam>
        /// <param name="key"><para>zh-cn:Redis Sorted Set 键。</para><para>en-us:The Redis sorted set key.</para></param>
        /// <param name="start"><para>zh-cn:分值范围起点。</para><para>en-us:The start score.</para></param>
        /// <param name="stop"><para>zh-cn:分值范围终点。</para><para>en-us:The stop score.</para></param>
        /// <param name="desc"><para>zh-cn:是否按降序读取。</para><para>en-us:Whether to read in descending order.</para></param>
        /// <returns><para>zh-cn:表示异步读取操作的任务，结果为成员到分值的映射。</para><para>en-us:A task representing the asynchronous read operation, with a mapping from members to scores as the result.</para></returns>
        Task<Dictionary<T, double>> RangeByScoreWithScoresAsync<T>(string key, double start = 0, double stop = -1, bool desc = false);
        /// <summary>
        /// <para>zh-cn:按成员值范围读取 Redis Sorted Set 成员。</para>
        /// <para>en-us:Reads Redis sorted set members by member value range.</para>
        /// </summary>
        /// <typeparam name="T"><para>zh-cn:有序集合成员类型。</para><para>en-us:The sorted set member type.</para></typeparam>
        /// <param name="key"><para>zh-cn:Redis Sorted Set 键。</para><para>en-us:The Redis sorted set key.</para></param>
        /// <param name="startValue"><para>zh-cn:成员值范围起点。</para><para>en-us:The start of the member value range.</para></param>
        /// <param name="endValue"><para>zh-cn:成员值范围终点。</para><para>en-us:The end of the member value range.</para></param>
        /// <returns><para>zh-cn:指定成员值范围内的成员集合。</para><para>en-us:The members in the specified member value range.</para></returns>
        List<T> RangeByValue<T>(string key, T startValue, T endValue);
        /// <summary>
        /// <para>zh-cn:异步按成员值范围读取 Redis Sorted Set 成员。</para>
        /// <para>en-us:Asynchronously reads Redis sorted set members by member value range.</para>
        /// </summary>
        /// <typeparam name="T"><para>zh-cn:有序集合成员类型。</para><para>en-us:The sorted set member type.</para></typeparam>
        /// <param name="key"><para>zh-cn:Redis Sorted Set 键。</para><para>en-us:The Redis sorted set key.</para></param>
        /// <param name="startValue"><para>zh-cn:成员值范围起点。</para><para>en-us:The start of the member value range.</para></param>
        /// <param name="endValue"><para>zh-cn:成员值范围终点。</para><para>en-us:The end of the member value range.</para></param>
        /// <returns><para>zh-cn:表示异步读取操作的任务，结果为指定成员值范围内的成员集合。</para><para>en-us:A task representing the asynchronous read operation, with the members in the specified member value range as the result.</para></returns>
        Task<List<T>> RangeByValueAsync<T>(string key, T startValue, T endValue);
        /// <summary>
        /// <para>zh-cn:从 Redis Sorted Set 中移除一个或多个成员。</para>
        /// <para>en-us:Removes one or more members from a Redis sorted set.</para>
        /// </summary>
        /// <typeparam name="T"><para>zh-cn:有序集合成员类型。</para><para>en-us:The sorted set member type.</para></typeparam>
        /// <param name="key"><para>zh-cn:Redis Sorted Set 键。</para><para>en-us:The Redis sorted set key.</para></param>
        /// <param name="value"><para>zh-cn:需要移除的成员。</para><para>en-us:The members to remove.</para></param>
        /// <returns><para>zh-cn:成功移除的成员数量。</para><para>en-us:The number of members successfully removed.</para></returns>
        long Remove<T>(string key, params T[] value);
        /// <summary>
        /// <para>zh-cn:异步从 Redis Sorted Set 中移除一个或多个成员。</para>
        /// <para>en-us:Asynchronously removes one or more members from a Redis sorted set.</para>
        /// </summary>
        /// <typeparam name="T"><para>zh-cn:有序集合成员类型。</para><para>en-us:The sorted set member type.</para></typeparam>
        /// <param name="key"><para>zh-cn:Redis Sorted Set 键。</para><para>en-us:The Redis sorted set key.</para></param>
        /// <param name="value"><para>zh-cn:需要移除的成员。</para><para>en-us:The members to remove.</para></param>
        /// <returns><para>zh-cn:表示异步移除操作的任务，结果为成功移除的成员数量。</para><para>en-us:A task representing the asynchronous remove operation, with the number of members successfully removed as the result.</para></returns>
        Task<long> RemoveAsync<T>(string key, params T[] value);
        /// <summary>
        /// <para>zh-cn:按排名范围从 Redis Sorted Set 中移除成员。</para>
        /// <para>en-us:Removes members from a Redis sorted set by rank range.</para>
        /// </summary>
        /// <param name="key"><para>zh-cn:Redis Sorted Set 键。</para><para>en-us:The Redis sorted set key.</para></param>
        /// <param name="start"><para>zh-cn:排名范围起点。</para><para>en-us:The start rank.</para></param>
        /// <param name="stop"><para>zh-cn:排名范围终点。</para><para>en-us:The stop rank.</para></param>
        /// <returns><para>zh-cn:成功移除的成员数量。</para><para>en-us:The number of members successfully removed.</para></returns>
        long RemoveRangeByRank(string key, long start, long stop);
        /// <summary>
        /// <para>zh-cn:异步按排名范围从 Redis Sorted Set 中移除成员。</para>
        /// <para>en-us:Asynchronously removes members from a Redis sorted set by rank range.</para>
        /// </summary>
        /// <param name="key"><para>zh-cn:Redis Sorted Set 键。</para><para>en-us:The Redis sorted set key.</para></param>
        /// <param name="start"><para>zh-cn:排名范围起点。</para><para>en-us:The start rank.</para></param>
        /// <param name="stop"><para>zh-cn:排名范围终点。</para><para>en-us:The stop rank.</para></param>
        /// <returns><para>zh-cn:表示异步移除操作的任务，结果为成功移除的成员数量。</para><para>en-us:A task representing the asynchronous remove operation, with the number of members successfully removed as the result.</para></returns>
        Task<long> RemoveRangeByRankAsync(string key, long start, long stop);
        /// <summary>
        /// <para>zh-cn:按分值范围从 Redis Sorted Set 中移除成员。</para>
        /// <para>en-us:Removes members from a Redis sorted set by score range.</para>
        /// </summary>
        /// <param name="key"><para>zh-cn:Redis Sorted Set 键。</para><para>en-us:The Redis sorted set key.</para></param>
        /// <param name="start"><para>zh-cn:分值范围起点。</para><para>en-us:The start score.</para></param>
        /// <param name="stop"><para>zh-cn:分值范围终点。</para><para>en-us:The stop score.</para></param>
        /// <returns><para>zh-cn:成功移除的成员数量。</para><para>en-us:The number of members successfully removed.</para></returns>
        long RemoveRangeByScore(string key, double start, double stop);
        /// <summary>
        /// <para>zh-cn:异步按分值范围从 Redis Sorted Set 中移除成员。</para>
        /// <para>en-us:Asynchronously removes members from a Redis sorted set by score range.</para>
        /// </summary>
        /// <param name="key"><para>zh-cn:Redis Sorted Set 键。</para><para>en-us:The Redis sorted set key.</para></param>
        /// <param name="start"><para>zh-cn:分值范围起点。</para><para>en-us:The start score.</para></param>
        /// <param name="stop"><para>zh-cn:分值范围终点。</para><para>en-us:The stop score.</para></param>
        /// <returns><para>zh-cn:表示异步移除操作的任务，结果为成功移除的成员数量。</para><para>en-us:A task representing the asynchronous remove operation, with the number of members successfully removed as the result.</para></returns>
        Task<long> RemoveRangeByScoreAsync(string key, double start, double stop);
        /// <summary>
        /// <para>zh-cn:按成员值范围从 Redis Sorted Set 中移除成员。</para>
        /// <para>en-us:Removes members from a Redis sorted set by member value range.</para>
        /// </summary>
        /// <typeparam name="T"><para>zh-cn:有序集合成员类型。</para><para>en-us:The sorted set member type.</para></typeparam>
        /// <param name="key"><para>zh-cn:Redis Sorted Set 键。</para><para>en-us:The Redis sorted set key.</para></param>
        /// <param name="startValue"><para>zh-cn:成员值范围起点。</para><para>en-us:The start of the member value range.</para></param>
        /// <param name="endValue"><para>zh-cn:成员值范围终点。</para><para>en-us:The end of the member value range.</para></param>
        /// <returns><para>zh-cn:成功移除的成员数量。</para><para>en-us:The number of members successfully removed.</para></returns>
        long RemoveRangeByValue<T>(string key, T startValue, T endValue);
        /// <summary>
        /// <para>zh-cn:异步按成员值范围从 Redis Sorted Set 中移除成员。</para>
        /// <para>en-us:Asynchronously removes members from a Redis sorted set by member value range.</para>
        /// </summary>
        /// <typeparam name="T"><para>zh-cn:有序集合成员类型。</para><para>en-us:The sorted set member type.</para></typeparam>
        /// <param name="key"><para>zh-cn:Redis Sorted Set 键。</para><para>en-us:The Redis sorted set key.</para></param>
        /// <param name="startValue"><para>zh-cn:成员值范围起点。</para><para>en-us:The start of the member value range.</para></param>
        /// <param name="endValue"><para>zh-cn:成员值范围终点。</para><para>en-us:The end of the member value range.</para></param>
        /// <returns><para>zh-cn:表示异步移除操作的任务，结果为成功移除的成员数量。</para><para>en-us:A task representing the asynchronous remove operation, with the number of members successfully removed as the result.</para></returns>
        Task<long> RemoveRangeByValueAsync<T>(string key, T startValue, T endValue);
        /// <summary>
        /// <para>zh-cn:读取 Redis Sorted Set 中指定成员的分值。</para>
        /// <para>en-us:Reads the score of the specified member in a Redis sorted set.</para>
        /// </summary>
        /// <typeparam name="T"><para>zh-cn:有序集合成员类型。</para><para>en-us:The sorted set member type.</para></typeparam>
        /// <param name="key"><para>zh-cn:Redis Sorted Set 键。</para><para>en-us:The Redis sorted set key.</para></param>
        /// <param name="value"><para>zh-cn:需要读取分值的成员。</para><para>en-us:The member whose score should be read.</para></param>
        /// <returns><para>zh-cn:成员分值；成员不存在时返回值由实现决定。</para><para>en-us:The member score; when the member does not exist, the return value is determined by the implementation.</para></returns>
        double? Score<T>(string key, T value);
        /// <summary>
        /// <para>zh-cn:异步读取 Redis Sorted Set 中指定成员的分值。</para>
        /// <para>en-us:Asynchronously reads the score of the specified member in a Redis sorted set.</para>
        /// </summary>
        /// <typeparam name="T"><para>zh-cn:有序集合成员类型。</para><para>en-us:The sorted set member type.</para></typeparam>
        /// <param name="key"><para>zh-cn:Redis Sorted Set 键。</para><para>en-us:The Redis sorted set key.</para></param>
        /// <param name="value"><para>zh-cn:需要读取分值的成员。</para><para>en-us:The member whose score should be read.</para></param>
        /// <returns><para>zh-cn:表示异步读取操作的任务，结果为成员分值。</para><para>en-us:A task representing the asynchronous read operation, with the member score as the result.</para></returns>
        Task<double?> SetScoreAsync<T>(string key, T value);
    }

    /// <summary>
    /// <para>zh-cn:定义 Redis Hash 数据结构的缓存操作标准，支持字段删除、字段存在性判断、字段读取、整表读取、字段名读取和字段写入。</para>
    /// <para>en-us:Defines the cache operation contract for the Redis hash data structure, supporting field deletion, field existence checks, field reads, whole-hash reads, field-name reads, and field writes.</para>
    /// </summary>
    public interface IHashCache
    {
        /// <summary>
        /// <para>zh-cn:从指定 Redis Hash 中删除单个字段。</para>
        /// <para>en-us:Deletes a single field from the specified Redis hash.</para>
        /// </summary>
        /// <param name="Key"><para>zh-cn:Redis Hash 键。</para><para>en-us:The Redis hash key.</para></param>
        /// <param name="HashKey"><para>zh-cn:需要删除的 Hash 字段名。</para><para>en-us:The hash field name to delete.</para></param>
        /// <returns><para>zh-cn:如果字段被删除则返回 `true`；否则返回 `false`。</para><para>en-us:Returns `true` when the field is deleted; otherwise returns `false`.</para></returns>
        bool Delete(string Key, string HashKey);
        /// <summary>
        /// <para>zh-cn:从指定 Redis Hash 中批量删除字段。</para>
        /// <para>en-us:Deletes fields from the specified Redis hash in batch.</para>
        /// </summary>
        /// <param name="Key"><para>zh-cn:Redis Hash 键。</para><para>en-us:The Redis hash key.</para></param>
        /// <param name="HashKeys"><para>zh-cn:需要删除的 Hash 字段名集合。</para><para>en-us:The hash field names to delete.</para></param>
        /// <returns><para>zh-cn:成功删除的字段数量。</para><para>en-us:The number of fields successfully deleted.</para></returns>
        long Delete(string Key, params string[] HashKeys);
        /// <summary>
        /// <para>zh-cn:异步从指定 Redis Hash 中删除单个字段。</para>
        /// <para>en-us:Asynchronously deletes a single field from the specified Redis hash.</para>
        /// </summary>
        /// <param name="Key"><para>zh-cn:Redis Hash 键。</para><para>en-us:The Redis hash key.</para></param>
        /// <param name="HashKey"><para>zh-cn:需要删除的 Hash 字段名。</para><para>en-us:The hash field name to delete.</para></param>
        /// <returns><para>zh-cn:表示异步删除操作的任务，结果表示字段是否被删除。</para><para>en-us:A task representing the asynchronous delete operation, whose result indicates whether the field was deleted.</para></returns>
        Task<bool> DeleteAsync(string Key, string HashKey);
        /// <summary>
        /// <para>zh-cn:异步从指定 Redis Hash 中批量删除字段。</para>
        /// <para>en-us:Asynchronously deletes fields from the specified Redis hash in batch.</para>
        /// </summary>
        /// <param name="Key"><para>zh-cn:Redis Hash 键。</para><para>en-us:The Redis hash key.</para></param>
        /// <param name="HashKeys"><para>zh-cn:需要删除的 Hash 字段名集合。</para><para>en-us:The hash field names to delete.</para></param>
        /// <returns><para>zh-cn:表示异步删除操作的任务，结果为成功删除的字段数量。</para><para>en-us:A task representing the asynchronous delete operation, with the number of fields successfully deleted as the result.</para></returns>
        Task<long> DeleteAsync(string Key, params string[] HashKeys);
        /// <summary>
        /// <para>zh-cn:判断指定 Redis Hash 中是否存在某个字段。</para>
        /// <para>en-us:Determines whether a field exists in the specified Redis hash.</para>
        /// </summary>
        /// <param name="Key"><para>zh-cn:Redis Hash 键。</para><para>en-us:The Redis hash key.</para></param>
        /// <param name="HashKeys"><para>zh-cn:需要判断的 Hash 字段名。</para><para>en-us:The hash field name to check.</para></param>
        /// <returns><para>zh-cn:如果字段存在则返回 `true`；否则返回 `false`。</para><para>en-us:Returns `true` when the field exists; otherwise returns `false`.</para></returns>
        bool Exists(string Key, string HashKeys);
        /// <summary>
        /// <para>zh-cn:异步判断指定 Redis Hash 中是否存在某个字段。</para>
        /// <para>en-us:Asynchronously determines whether a field exists in the specified Redis hash.</para>
        /// </summary>
        /// <param name="Key"><para>zh-cn:Redis Hash 键。</para><para>en-us:The Redis hash key.</para></param>
        /// <param name="HashKeys"><para>zh-cn:需要判断的 Hash 字段名。</para><para>en-us:The hash field name to check.</para></param>
        /// <returns><para>zh-cn:表示异步判断操作的任务，结果表示字段是否存在。</para><para>en-us:A task representing the asynchronous existence check, whose result indicates whether the field exists.</para></returns>
        Task<bool> ExistsAsync(string Key, string HashKeys);
        /// <summary>
        /// <para>zh-cn:读取 Redis Hash 中指定字段的值，并反序列化为目标类型。</para>
        /// <para>en-us:Reads the value of the specified field from a Redis hash and deserializes it to the target type.</para>
        /// </summary>
        /// <typeparam name="T"><para>zh-cn:目标值类型。</para><para>en-us:The target value type.</para></typeparam>
        /// <param name="Key"><para>zh-cn:Redis Hash 键。</para><para>en-us:The Redis hash key.</para></param>
        /// <param name="HashKeys"><para>zh-cn:需要读取的 Hash 字段名。</para><para>en-us:The hash field name to read.</para></param>
        /// <returns><para>zh-cn:反序列化后的字段值；字段不存在时返回值由实现决定。</para><para>en-us:The deserialized field value; when the field does not exist, the return value is determined by the implementation.</para></returns>
        T Get<T>(string Key, string HashKeys);
        /// <summary>
        /// <para>zh-cn:异步读取 Redis Hash 中指定字段的值，并反序列化为目标类型。</para>
        /// <para>en-us:Asynchronously reads the value of the specified field from a Redis hash and deserializes it to the target type.</para>
        /// </summary>
        /// <typeparam name="T"><para>zh-cn:目标值类型。</para><para>en-us:The target value type.</para></typeparam>
        /// <param name="Key"><para>zh-cn:Redis Hash 键。</para><para>en-us:The Redis hash key.</para></param>
        /// <param name="HashKeys"><para>zh-cn:需要读取的 Hash 字段名。</para><para>en-us:The hash field name to read.</para></param>
        /// <returns><para>zh-cn:表示异步读取操作的任务，结果为反序列化后的字段值。</para><para>en-us:A task representing the asynchronous read operation, with the deserialized field value as the result.</para></returns>
        Task<T> GetAsync<T>(string Key, string HashKeys);
        /// <summary>
        /// <para>zh-cn:读取指定 Redis Hash 的所有字段和值，并将字段值反序列化为目标类型。</para>
        /// <para>en-us:Reads all fields and values from the specified Redis hash and deserializes field values to the target type.</para>
        /// </summary>
        /// <typeparam name="T"><para>zh-cn:目标值类型。</para><para>en-us:The target value type.</para></typeparam>
        /// <param name="Key"><para>zh-cn:Redis Hash 键。</para><para>en-us:The Redis hash key.</para></param>
        /// <returns><para>zh-cn:Hash 字段名到反序列化字段值的映射。</para><para>en-us:A mapping from hash field names to deserialized field values.</para></returns>
        Dictionary<string, T> GetAll<T>(string Key);
        /// <summary>
        /// <para>zh-cn:异步读取指定 Redis Hash 的所有字段和值，并将字段值反序列化为目标类型。</para>
        /// <para>en-us:Asynchronously reads all fields and values from the specified Redis hash and deserializes field values to the target type.</para>
        /// </summary>
        /// <typeparam name="T"><para>zh-cn:目标值类型。</para><para>en-us:The target value type.</para></typeparam>
        /// <param name="Key"><para>zh-cn:Redis Hash 键。</para><para>en-us:The Redis hash key.</para></param>
        /// <returns><para>zh-cn:表示异步读取操作的任务，结果为 Hash 字段名到反序列化字段值的映射。</para><para>en-us:A task representing the asynchronous read operation, with a mapping from hash field names to deserialized field values as the result.</para></returns>
        Task<Dictionary<string, T>> GetAllAsync<T>(string Key);
        /// <summary>
        /// <para>zh-cn:读取指定 Redis Hash 中的全部字段名。</para>
        /// <para>en-us:Reads all field names from the specified Redis hash.</para>
        /// </summary>
        /// <param name="Key"><para>zh-cn:Redis Hash 键。</para><para>en-us:The Redis hash key.</para></param>
        /// <returns><para>zh-cn:Hash 字段名数组。</para><para>en-us:An array of hash field names.</para></returns>
        string[] Keys(string Key);
        /// <summary>
        /// <para>zh-cn:异步读取指定 Redis Hash 中的全部字段名。</para>
        /// <para>en-us:Asynchronously reads all field names from the specified Redis hash.</para>
        /// </summary>
        /// <param name="Key"><para>zh-cn:Redis Hash 键。</para><para>en-us:The Redis hash key.</para></param>
        /// <returns><para>zh-cn:表示异步读取操作的任务，结果为 Hash 字段名数组。</para><para>en-us:A task representing the asynchronous read operation, with an array of hash field names as the result.</para></returns>
        Task<string[]> KeysAsync(string Key);
        /// <summary>
        /// <para>zh-cn:向指定 Redis Hash 字段写入对象值。</para>
        /// <para>en-us:Writes an object value to the specified Redis hash field.</para>
        /// </summary>
        /// <typeparam name="T"><para>zh-cn:需要写入的值类型。</para><para>en-us:The value type to write.</para></typeparam>
        /// <param name="Key"><para>zh-cn:Redis Hash 键。</para><para>en-us:The Redis hash key.</para></param>
        /// <param name="HashKeys"><para>zh-cn:需要写入的 Hash 字段名。</para><para>en-us:The hash field name to write.</para></param>
        /// <param name="t"><para>zh-cn:需要保存的字段值。</para><para>en-us:The field value to store.</para></param>
        /// <returns><para>zh-cn:如果写入成功则返回 `true`；否则返回 `false`。</para><para>en-us:Returns `true` when the write succeeds; otherwise returns `false`.</para></returns>
        bool Set<T>(string Key, string HashKeys, T t);
        /// <summary>
        /// <para>zh-cn:异步向指定 Redis Hash 字段写入对象值。</para>
        /// <para>en-us:Asynchronously writes an object value to the specified Redis hash field.</para>
        /// </summary>
        /// <typeparam name="T"><para>zh-cn:需要写入的值类型。</para><para>en-us:The value type to write.</para></typeparam>
        /// <param name="Key"><para>zh-cn:Redis Hash 键。</para><para>en-us:The Redis hash key.</para></param>
        /// <param name="HashKeys"><para>zh-cn:需要写入的 Hash 字段名。</para><para>en-us:The hash field name to write.</para></param>
        /// <param name="t"><para>zh-cn:需要保存的字段值。</para><para>en-us:The field value to store.</para></param>
        /// <returns><para>zh-cn:表示异步写入操作的任务，结果表示写入是否成功。</para><para>en-us:A task representing the asynchronous write operation, whose result indicates whether the write succeeded.</para></returns>
        Task<bool> SetAsync<T>(string Key, string HashKeys, T t);
    }

    /// <summary>
    /// <para>zh-cn:定义 Redis 键级操作标准，支持删除、存在性判断、过期时间、刷新、重命名和阻塞锁执行。</para>
    /// <para>en-us:Defines the Redis key-level operation contract, supporting deletion, existence checks, expiration, flushing, renaming, and blocking-lock execution.</para>
    /// </summary>
    public interface IRedisCacheKeyStandard
    {
        /// <summary>
        /// <para>zh-cn:批量删除 Redis 键。</para>
        /// <para>en-us:Deletes Redis keys in batch.</para>
        /// </summary>
        /// <param name="keys"><para>zh-cn:需要删除的 Redis 键集合。</para><para>en-us:The Redis keys to delete.</para></param>
        /// <returns><para>zh-cn:成功删除的键数量。</para><para>en-us:The number of keys successfully deleted.</para></returns>
        long Delete(params string[] keys);
        /// <summary>
        /// <para>zh-cn:删除单个 Redis 键。</para>
        /// <para>en-us:Deletes a single Redis key.</para>
        /// </summary>
        /// <param name="key"><para>zh-cn:需要删除的 Redis 键。</para><para>en-us:The Redis key to delete.</para></param>
        /// <returns><para>zh-cn:如果键被删除则返回 `true`；否则返回 `false`。</para><para>en-us:Returns `true` when the key is deleted; otherwise returns `false`.</para></returns>
        bool Delete(string key);
        /// <summary>
        /// <para>zh-cn:异步批量删除 Redis 键。</para>
        /// <para>en-us:Asynchronously deletes Redis keys in batch.</para>
        /// </summary>
        /// <param name="keys"><para>zh-cn:需要删除的 Redis 键集合。</para><para>en-us:The Redis keys to delete.</para></param>
        /// <returns><para>zh-cn:表示异步删除操作的任务，结果为成功删除的键数量。</para><para>en-us:A task representing the asynchronous delete operation, with the number of keys successfully deleted as the result.</para></returns>
        Task<long> DeleteAsync(params string[] keys);
        /// <summary>
        /// <para>zh-cn:异步删除单个 Redis 键。</para>
        /// <para>en-us:Asynchronously deletes a single Redis key.</para>
        /// </summary>
        /// <param name="key"><para>zh-cn:需要删除的 Redis 键。</para><para>en-us:The Redis key to delete.</para></param>
        /// <returns><para>zh-cn:表示异步删除操作的任务，结果表示键是否被删除。</para><para>en-us:A task representing the asynchronous delete operation, whose result indicates whether the key was deleted.</para></returns>
        Task<bool> DeleteAsync(string key);
        /// <summary>
        /// <para>zh-cn:判断 Redis 键是否存在。</para>
        /// <para>en-us:Determines whether a Redis key exists.</para>
        /// </summary>
        /// <param name="key"><para>zh-cn:需要判断的 Redis 键。</para><para>en-us:The Redis key to check.</para></param>
        /// <returns><para>zh-cn:如果键存在则返回 `true`；否则返回 `false`。</para><para>en-us:Returns `true` when the key exists; otherwise returns `false`.</para></returns>
        bool Exists(string key);
        /// <summary>
        /// <para>zh-cn:异步判断 Redis 键是否存在。</para>
        /// <para>en-us:Asynchronously determines whether a Redis key exists.</para>
        /// </summary>
        /// <param name="key"><para>zh-cn:需要判断的 Redis 键。</para><para>en-us:The Redis key to check.</para></param>
        /// <returns><para>zh-cn:表示异步判断操作的任务，结果表示键是否存在。</para><para>en-us:A task representing the asynchronous existence check, whose result indicates whether the key exists.</para></returns>
        Task<bool> ExistsAsync(string key);
        /// <summary>
        /// <para>zh-cn:为 Redis 键设置过期时间。</para>
        /// <para>en-us:Sets an expiration time for a Redis key.</para>
        /// </summary>
        /// <param name="key"><para>zh-cn:需要设置过期时间的 Redis 键。</para><para>en-us:The Redis key whose expiration should be set.</para></param>
        /// <param name="expiry"><para>zh-cn:过期时间；为 `null` 时由实现决定具体行为。</para><para>en-us:The expiration time. When `null`, the concrete behavior is determined by the implementation.</para></param>
        /// <returns><para>zh-cn:如果过期时间设置成功则返回 `true`；否则返回 `false`。</para><para>en-us:Returns `true` when the expiration is set successfully; otherwise returns `false`.</para></returns>
        bool Expire(string key, TimeSpan? expiry = null);
        /// <summary>
        /// <para>zh-cn:异步为 Redis 键设置过期时间。</para>
        /// <para>en-us:Asynchronously sets an expiration time for a Redis key.</para>
        /// </summary>
        /// <param name="key"><para>zh-cn:需要设置过期时间的 Redis 键。</para><para>en-us:The Redis key whose expiration should be set.</para></param>
        /// <param name="expiry"><para>zh-cn:过期时间；为 `null` 时由实现决定具体行为。</para><para>en-us:The expiration time. When `null`, the concrete behavior is determined by the implementation.</para></param>
        /// <returns><para>zh-cn:表示异步设置操作的任务，结果表示过期时间是否设置成功。</para><para>en-us:A task representing the asynchronous expiration operation, whose result indicates whether the expiration was set successfully.</para></returns>
        Task<bool> ExpireAsync(string key, TimeSpan? expiry = null);
        /// <summary>
        /// <para>zh-cn:刷新当前 Redis 数据库中的键数据。方法名保留历史拼写 `Fulsh`。</para>
        /// <para>en-us:Flushes key data in the current Redis database. The method name keeps the historical spelling `Fulsh`.</para>
        /// </summary>
        void Fulsh();
        /// <summary>
        /// <para>zh-cn:异步刷新当前 Redis 数据库中的键数据。方法名保留历史拼写 `FulshAsync`。</para>
        /// <para>en-us:Asynchronously flushes key data in the current Redis database. The method name keeps the historical spelling `FulshAsync`.</para>
        /// </summary>
        /// <returns><para>zh-cn:表示异步刷新操作的任务。</para><para>en-us:A task representing the asynchronous flush operation.</para></returns>
        Task FulshAsync();
        /// <summary>
        /// <para>zh-cn:重命名 Redis 键。</para>
        /// <para>en-us:Renames a Redis key.</para>
        /// </summary>
        /// <param name="key"><para>zh-cn:当前 Redis 键。</para><para>en-us:The current Redis key.</para></param>
        /// <param name="newKey"><para>zh-cn:新的 Redis 键名。</para><para>en-us:The new Redis key name.</para></param>
        /// <returns><para>zh-cn:如果重命名成功则返回 `true`；否则返回 `false`。</para><para>en-us:Returns `true` when the rename succeeds; otherwise returns `false`.</para></returns>
        bool Rename(string key, string newKey);
        /// <summary>
        /// <para>zh-cn:异步重命名 Redis 键。</para>
        /// <para>en-us:Asynchronously renames a Redis key.</para>
        /// </summary>
        /// <param name="key"><para>zh-cn:当前 Redis 键。</para><para>en-us:The current Redis key.</para></param>
        /// <param name="newKey"><para>zh-cn:新的 Redis 键名。</para><para>en-us:The new Redis key name.</para></param>
        /// <returns><para>zh-cn:表示异步重命名操作的任务，结果表示重命名是否成功。</para><para>en-us:A task representing the asynchronous rename operation, whose result indicates whether the rename succeeded.</para></returns>
        Task<bool> RenameAsync(string key, string newKey);
        /// <summary>
        /// <para>zh-cn:在指定 Redis 键对应的阻塞锁内执行异步委托。</para>
        /// <para>en-us:Executes an asynchronous delegate inside the blocking lock associated with the specified Redis key.</para>
        /// </summary>
        /// <param name="act"><para>zh-cn:获得锁后需要执行的异步操作。</para><para>en-us:The asynchronous operation to execute after the lock is acquired.</para></param>
        /// <param name="ts"><para>zh-cn:锁持有或等待相关的时间跨度，具体含义由实现决定。</para><para>en-us:The time span related to lock holding or waiting. The exact meaning is determined by the implementation.</para></param>
        /// <param name="key"><para>zh-cn:用于创建阻塞锁的 Redis 键。</para><para>en-us:The Redis key used to create the blocking lock.</para></param>
        /// <param name="count"><para>zh-cn:重试或轮询次数，默认值为 300。</para><para>en-us:The retry or polling count. The default value is 300.</para></param>
        /// <returns><para>zh-cn:表示异步阻塞锁执行的任务，结果表示操作是否成功执行。</para><para>en-us:A task representing the asynchronous blocking-lock execution, whose result indicates whether the operation executed successfully.</para></returns>
        Task<bool> BlockLockTake(Func<Task> act, TimeSpan ts, string key, int count = 300);
    }
}
