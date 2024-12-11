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
 */using Air.Cloud.Core.Collections;
using Air.Cloud.Core.Modules.StandardPool;

using System.Collections.Concurrent;

namespace Air.Cloud.Core.Standard.SchedulerStandard.Pool
{
    /// <summary>
    /// <para>zh-cn:调度池</para>
    /// <para>en-us:Scheduler Pool</para>
    /// </summary>
    /// <typeparam name="TSchedulerOptions"></typeparam>
    public class SchedulerPool<TSchedulerOptions> : IAppPool<ISchedulerStandard<TSchedulerOptions>>
        where TSchedulerOptions : class, ISchedulerStandardOptions
    {
        /// <summary>
        /// 对象池存储
        /// </summary>
        private static ConcurrentDictionary<string, ISchedulerStandard<TSchedulerOptions>> Pool = new ConcurrentDictionary<string, ISchedulerStandard<TSchedulerOptions>>();
        /// <summary>
        /// 锁定的对象
        /// </summary>
        private static ConcurrentList<string> LockPool = new ConcurrentList<string>();
        /// <summary>
        /// 池操作锁
        /// </summary>
        private static object PoolLockTag = new object();

        /// <inheritdoc/>
        public ISchedulerStandard<TSchedulerOptions> Get(string Key)
        {
            if (Pool.TryGetValue(Key, out var standard)) return standard;
            return null;
        }
        /// <inheritdoc/>
        public bool Set(ISchedulerStandard<TSchedulerOptions> standard)
        {
            if (LockPool.Contains(standard.Options.Id))
            {
                //当前对象已经被锁定 无法进行修改
                AppRealization.Output.Print(new AppPrintInformation()
                {
                    State = true,
                    AdditionalParams = null,
                    Content = "Current scheduler object is locked",
                    Title = "SchedulerPool Notice",
                    Level = AppPrintInformation.AppPrintLevel.Warning
                });
                return false;
            }
            Pool.AddOrUpdate(standard.Options.Id, standard, (key, value) => value);
            return true;
        }

        /// <inheritdoc/>
        public bool Remove(string Key)
        {
            return Pool.TryRemove(Key, out var standard);
        }

        /// <inheritdoc/>
        public void Clear()
        {
            lock (PoolLockTag)
            {
                Pool.Clear();
                LockPool.Clear();
            }
        }


        #region 特殊方法
        /// <summary>
        /// 锁定对象
        /// </summary>
        /// <param name="Key"></param>
        /// <returns>
        /// <para>zh-cn:结果</para>
        /// <para>en-us:Result</para>
        /// </returns>
        public bool Lock(string Key)
        {
            lock (PoolLockTag)
            {
                LockPool.Add(Key);
            }
            return true;
        }
        /// <summary>
        /// <para>zh-cn:解锁元素</para>
        /// <para>en-us:Unlock element</para>
        /// </summary>
        /// <param name="Key">
        /// 
        /// </param>
        /// <returns>
        /// <para>zh-cn:结果</para>
        /// <para>en-us:Result</para>
        /// </returns>
        public bool Unlock(string Key)
        {
            lock (PoolLockTag)
            {
                return LockPool.Remove(Key);
            }
        }
        #endregion
    }
}
