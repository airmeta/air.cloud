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
using Air.Cloud.Core.Modules.AppPrint;

using System.Collections;

/**
    由于C# 自带的 ConcurrentBag 没有删除特定元素的方法，所以本框架采集了CSDN中的一个ConcurrentList的实现,并基于此版本上进行了优化与调节
    原文链接: https://blog.csdn.net/weixin_43542114/article/details/142952583
    作者: 望天House
    说明:
        该类为CSDN作者的原创作品,已获得该作者的授权,如有侵权请联系本框架作者删除
 */

namespace Air.Cloud.Core.Collections
{
    /// <summary>
    /// <para>zh-cn:线程安全列表，提供基于读写锁的并发访问能力。</para>
    /// <para>en-us:Thread-safe list that provides concurrent access based on reader-writer locks.</para>
    /// </summary>
    /// <remarks>
    /// <para>zh-cn:从当前版本开始，枚举器使用“快照枚举”策略：在创建枚举器时复制当前列表快照，后续枚举基于快照而非实时集合。</para>
    /// <para>en-us:Starting from this version, enumeration uses a snapshot strategy: a copy of the current list is taken when the enumerator is created, and subsequent enumeration is based on that snapshot instead of the live collection.</para>
    /// <para>zh-cn:影响：1) 枚举期间不会长期持有读锁，降低对写入线程的阻塞；2) 枚举结果不包含快照创建后的新增/删除；3) 会有一次额外内存复制开销。</para>
    /// <para>en-us:Impact: 1) No long-held read lock during enumeration, reducing writer blocking; 2) Enumeration does not include additions/removals after snapshot creation; 3) There is a one-time extra memory copy cost.</para>
    /// </remarks>
    public class ConcurrentList<T> : IList<T>, IDisposable
    {
        private readonly IList<T> _list = new List<T>();

        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

        private bool _disposed;

        #region  构造函数
        public ConcurrentList()
        {

        }

        public ConcurrentList(List<T> values)
        {
            _list = values == null ? new List<T>() : new List<T>(values);
        }

        #endregion

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                AppRealization.Output.Print(new AppPrintInformation(
                    title: "ConcurrentList 对象已释放",
                    content: $"检测到对已释放实例 {nameof(ConcurrentList<T>)} 的访问",
                    level: AppPrintLevel.Warn));
                throw new ObjectDisposedException(nameof(ConcurrentList<T>));
            }
        }

        private void ConcurrentAction(Action<IList<T>> action)
        {
            ConcurrentAction(x => x.EnterWriteLock(), x => x.ExitWriteLock(), action);
        }

        private void ConcurrentAction(Action<ReaderWriterLockSlim> enter
              , Action<ReaderWriterLockSlim> exit, Action<IList<T>> action)
        {
            ThrowIfDisposed();
            try
            {
                enter(_lock);
                action(_list);
            }
            finally
            {
                exit(_lock);
            }
        }

        private TResult ConcurrentFunc<TResult>(Func<IList<T>, TResult> func)
              => ConcurrentFunc(x => x.EnterReadLock(), x => x.ExitReadLock(), func);

        private TResult ConcurrentFunc<TResult>(Action<ReaderWriterLockSlim> enter
              , Action<ReaderWriterLockSlim> exit, Func<IList<T>, TResult> func)
        {
            ThrowIfDisposed();
            try
            {
                enter(_lock);
                return func(_list);
            }
            finally
            {
                exit(_lock);
            }
        }

        public void AddRange(IEnumerable<T> values)
        {
            ConcurrentAction(l =>
            {
                foreach (var value in values)
                {
                    l.Add(value);
                }
            });
        }

        public void Add(T item) => ConcurrentAction(l => l.Add(item));


        public bool Remove(T item) => ConcurrentFunc(x => x.EnterWriteLock(), x => x.ExitWriteLock(), l => l.Remove(item));


        public void Clear() => ConcurrentAction(l => l.Clear());


        public bool Contains(T item) => ConcurrentFunc(l => l.Contains(item));


        public void CopyTo(T[] array, int arrayIndex) => ConcurrentAction(x => x.EnterReadLock(), x => x.ExitReadLock(), l => l.CopyTo(array, arrayIndex));


        public int Count => ConcurrentFunc(l => l.Count);


        public bool IsReadOnly => ConcurrentFunc(l => l.IsReadOnly);


        public int IndexOf(T item) => ConcurrentFunc(l => l.IndexOf(item));


        public void Insert(int index, T item) => ConcurrentAction(l => l.Insert(index, item));


        public void RemoveAt(int index) => ConcurrentAction(l => l.RemoveAt(index));


        public T this[int index]
        {
            get => ConcurrentFunc(l => l[index]);
            set => ConcurrentAction(l => l[index] = value);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            var snapshot = ConcurrentFunc(l => l.ToList());
            return snapshot.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<T>)this).GetEnumerator();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _lock.Dispose();
            }

            _disposed = true;
        }
    }
}






