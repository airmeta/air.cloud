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
using Air.Cloud.Core.Standard.Cache.Redis;
using Air.Cloud.Modules.RedisCache.Internal;
using Air.Cloud.Modules.RedisCache.Provider;

using StackExchange.Redis;

namespace Air.Cloud.Modules.RedisCache.Dependencies
{
    public class RedisCacheDependency : IRedisCacheStandard
    {
        /// <summary>
        /// <para>zh-cn:Redis连接对象</para>
        /// <para>en-us:Redis connection object</para>
        /// </summary>
        private static ConnectionMultiplexer Connection => RedisConnectorProvider.Instance;

        public RedisCacheDependency()
        {
            this.Redis = Connection.GetDatabase(0);
        }
        /// <summary>
        /// <para>zh-cn:Redis缓存依赖</para>
        /// <para>en-us:Redis cache dependency</para>
        /// </summary>
        /// <param name="DataBaseIndex">
        /// <para>zh-cn:Redis数据库索引</para>
        /// <para>en-us:Redis database index</para>
        /// </param>
        public RedisCacheDependency(int DataBaseIndex = 0)
        {
            this.Redis = Connection.GetDatabase(DataBaseIndex);
        }
        /// <summary>
        /// <para>zh-cn:更改Redis数据库索引</para>
        /// <para>en-us:Change Redis database index</para>
        /// </summary>
        /// <param name="DataBaseIndex">
        /// <para>zh-cn:Redis数据库索引</para>
        /// <para>en-us:Redis database index</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:Redis缓存依赖</para>
        /// <para>en-us:Redis cache dependency</para>
        /// </returns>
        public IRedisCacheStandard Change(int DataBaseIndex = 0)
        {
            return new RedisCacheDependency(DataBaseIndex);
        }
        /// <summary>
        /// <para>zh-cn:获取Redis数据库对象</para>
        /// <para>en-us:Get Redis database object</para>
        /// </summary>
        /// <returns>
        /// <para>zh-cn:Redis数据库对象</para>
        /// <para>en-us:Redis database object</para>
        /// </returns>
        public object GetDatabase() 
        {
            return this.Redis;
        }

        /// <summary>
        /// <para>zh-cn:数据库连接对象</para>
        /// <para>en-us:Database connection object</para>
        /// </summary>
        private readonly IDatabase Redis;

        #region  HashCache
        /// <summary>
        /// <para>zh-cn:Hash对象操作</para>
        /// <para>en-us:Hash object operation</para>
        /// </summary>
        public IHashCache Hash => GetHashCache();
        /// <summary>
        /// <para>zh-cn:Hash对象操作</para>
        /// <para>en-us:Hash object operation</para>
        /// </summary>
        private IHashCache HashCache = null;
        /// <summary>
        /// <para>zh-cn:获取Hash对象操作</para>
        /// <para>en-us:Get Hash object operation</para>
        /// </summary>
        /// <returns>
        /// <para>zh-cn:Hash对象操作</para>
        /// <para>en-us:Hash object operation</para>
        /// </returns>
        private IHashCache GetHashCache()
        {
            if(HashCache==null)
            {
                HashCache = new HashCache(Redis);
            }
            return HashCache;
        }
        #endregion

        #region StringCache
        /// <summary>
        /// <para>zh-cn:String对象缓存</para>
        /// <para>en-us:String object cache</para>
        /// </summary>
        public IStringCache String => GetStringCache();
        /// <summary>
        /// <para>zh-cn:String对象缓存</para>
        /// <para>en-us:String object cache</para>
        /// </summary>
        private IStringCache StringCache = null;
        /// <summary>
        /// <para>zh-cn:获取String对象缓存</para>
        /// <para>en-us:Get String object cache</para>
        /// </summary>
        /// <returns>
        /// <para>zh-cn:String对象缓存</para>
        /// <para>en-us:String object cache</para>
        /// </returns>
        private IStringCache GetStringCache()
        {
            if (StringCache == null)
            {
                StringCache = new StringCache(Redis);
            }
            return StringCache;
        }
        #endregion

        #region  ListCache
        /// <summary>
        /// <para>zh-cn:集合对象操作</para>
        /// <para>en-us:List object operation</para>
        /// </summary>
        public IListCache List => GetListCache();
        /// <summary>
        /// <para>zh-cn:集合对象操作</para>
        /// <para>en-us:List object operation</para>
        /// </summary>
        private IListCache ListCache = null;
        /// <summary>
        /// <para>zh-cn:获取集合对象操作</para>
        /// <para>en-us:Get List object operation</para>
        /// </summary>
        /// <returns>
        /// <para>zh-cn:集合对象操作</para>
        /// <para>en-us:List object operation</para>
        /// </returns>
        private IListCache GetListCache()
        {
            if (ListCache == null)
            {
                ListCache = new ListCache(Redis);
            }
            return ListCache;
        }
        #endregion

        #region SetCache
        /// <summary>
        /// <para>zh-cn:Set对象操作</para>
        /// <para>en-us:Set object operation</para>
        /// </summary>
        public ISetCache Set => GetSetCache();
        /// <summary>
        /// <para>zh-cn:Set对象操作</para>
        /// <para>en-us:Set object operation</para>
        /// </summary>
        private ISetCache PSetCache = null;
        /// <summary>
        /// <para>zh-cn:获取Set对象操作</para>
        /// <para>en-us:Get "Set" object operation</para>
        /// </summary>
        /// <returns>
        /// <para>zh-cn:Set对象操作</para>
        /// <para>en-us:Set object operation</para>
        /// </returns>
        private ISetCache GetSetCache()
        {
            if (PSetCache == null)
            {
                PSetCache = new SetCache(Redis);
            }
            return PSetCache;
        }

        #endregion

        #region SortedSetCache
        /// <summary>
        ///  <para>zh-cn:有序集合对象操作</para>
        ///  <para>en-us:Sorted set object operation</para>
        /// </summary>
        public ISortedSetCache SortedSet => GetSortedSetCache();

        /// <summary>
        ///  <para>zh-cn:有序集合对象操作</para>
        ///  <para>en-us:Sorted set object operation</para>
        /// </summary>
        private ISortedSetCache PSortedSetCache = null;
        /// <summary>
        /// <para>zh-cn:获取Redis缓存有序集合对象</para>
        /// <para>en-us:Get Redis cache sorted set object</para>
        /// </summary>
        /// <returns>
        ///  <para>zh-cn:有序集合对象操作</para>
        ///  <para>en-us:Sorted set object operation</para>
        /// </returns>
        private ISortedSetCache GetSortedSetCache()
        {
            if (PSortedSetCache == null)
            {
                PSortedSetCache = new SortedSetCache(Redis);
            }
            return PSortedSetCache;
        }
        #endregion

        #region KeyCache
        /// <summary>
        ///  <para>zh-cn:RedisKey缓存标准实现</para>
        ///  <para>en-us:RedisKey cache standard implementation</para>
        /// </summary>
        public IRedisCacheKeyStandard Key => GetRedisCacheKey();
        /// <summary>
        ///  <para>zh-cn:RedisKey缓存标准实现</para>
        ///  <para>en-us:RedisKey cache standard implementation</para>
        /// </summary>
        private IRedisCacheKeyStandard PKey = null;
        /// <summary>
        /// <para>zh-cn:获取Redis缓存Key对象</para>
        /// <para>en-us:Get Redis cache Key object</para>
        /// </summary>
        /// <returns>
        ///  <para>zh-cn:RedisKey缓存标准实现</para>
        ///  <para>en-us:RedisKey cache standard implementation</para>
        /// </returns>
        private IRedisCacheKeyStandard GetRedisCacheKey()
        {
            if (PKey == null)
            {
                PKey = new RedisCacheKeyDependency(Redis);
            }
            return PKey;
        }
        #endregion

        #region  方法
        public string? GetCache(string Key)
        {
            return String.Get(Key);
        }

        public bool SetCache(string Key, string Content, TimeSpan? timeSpan = null)
        {
            return String.Set(Key,Content,timeSpan);
        }

        public bool SetCache<T>(string Key, T t, TimeSpan? timeSpan = null) where T : class, new()
        {
            return String.Set(Key, t, timeSpan);
        }

        public T GetCache<T>(string Key) where T : class, new()
        {
            return String.Get<T>(Key);
        }

        public bool RemoveCache(string Key)
        {
            return this.Key.Delete(Key);
        }

        #endregion
    }
}
