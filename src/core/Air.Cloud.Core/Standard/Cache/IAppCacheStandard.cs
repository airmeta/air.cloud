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
using Air.Cloud.Core.Standard.Dependencies;

using Microsoft.Extensions.Caching.Memory;

namespace Air.Cloud.Core.Standard.Cache
{
    /// <summary>
    /// 缓存约定
    /// </summary>
    public interface IAppCacheStandard : IStandard
    {
        /// <summary>
        /// 设置缓存内容
        /// </summary>
        /// <param name="Key">缓存键</param>
        /// <param name="Content">缓存值</param>
        /// <param name="timeSpan">有效期(不设置则为永久)</param>
        /// <returns></returns>
        public bool SetCache(string Key, string Content, TimeSpan? timeSpan=null);

        /// <summary>
        /// 获取缓存内容
        /// </summary>
        /// <param name="Key">键</param>
        /// <returns>缓存的实际内容</returns>
        public string GetCache(string Key);

        /// <summary>
        /// 设置对象缓存
        /// </summary>
        /// <param name="Key">缓存键</param>
        /// <param name="t">缓存值</param>
        /// <param name="timeSpan">(不设置则为永久)</param>
        /// <returns></returns>
        public bool SetCache<T>(string Key, T t, TimeSpan? timeSpan = null) where T:class,new();

        /// <summary>
        /// 获取缓存
        /// </summary>
        /// <param name="Key"></param>
        /// <returns></returns>
        public T GetCache<T>(string Key) where T : class, new();
    }

    public class DefaultAppCache : IAppCacheStandard,ITransient
    {
        private readonly IMemoryCache _memoryCache;
        public DefaultAppCache(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
            if (_memoryCache == null)
            {
                AppStandardRealization.PrintStandard.Print(new
                {
                    Title = "errors",
                    Type = "Information",
                    Content = "系统内未配置默认缓存实现",
                    State = true
                });
            }
        }
        public string GetCache(string Key)
        {
            return _memoryCache.Get<string>(Key);
        }

        public T GetCache<T>(string Key) where T : class, new()
        {
            return _memoryCache.Get<T>(Key);
        }

        public bool SetCache(string Key, string Content, TimeSpan? timeSpan = null)
        {
            if (timeSpan.HasValue)
            {
                _memoryCache.Set(Key, Content, timeSpan.Value);
            }
            else
            {
                _memoryCache.Set(Key, Content);
            }

            return true;
        }

        public bool SetCache<T>(string Key, T t, TimeSpan? timeSpan = null) where T : class, new()
        {
            if (timeSpan.HasValue)
            {
                _memoryCache.Set(Key, t, timeSpan.Value);
            }
            else
            {
                _memoryCache.Set(Key, t);
            }

            return true;
        }
    }
}
