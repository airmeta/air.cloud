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

using SSS.Modules.Redis;
using SSS.Modules.Redis.Service;

using StackExchange.Redis;

namespace Air.Cloud.Modules.RedisCache.Dependencies
{
    public class RedisCacheDependency : IRedisCacheStandard
    {
        /// <summary>
        /// Redis连接对象
        /// </summary>
        private static ConnectionMultiplexer Connection => RedisConnectorProvider.Instance;

        private readonly IDatabase Redis;

        public RedisCacheDependency()
        {
            this.Redis = Connection.GetDatabase(0);
        }
        public RedisCacheDependency(int DataBaseIndex = 0)
        {
            this.Redis = Connection.GetDatabase(DataBaseIndex);
        }
        public IRedisCacheStandard Change(int DataBaseIndex = 0)
        {
            return new RedisCacheDependency(DataBaseIndex);
        }
        public object GetDatabase() 
        {
            return this.Redis;
        }
        public IHashCache Hash => new HashCache(Redis);

        public IStringCache String => new StringCache(Redis);

        public IListCache List => new ListCache(Redis);

        public ISetCache Set => new SetCache(Redis);

        public ISortedSetCache SortedSet => new SortedSetCache(Redis);

        public IRedisCacheKeyStandard Key => new RedisCacheKeyDependency(Redis);

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
        #endregion
    }
}
