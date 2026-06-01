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

/*
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
    /// <typeparam name="T">
    /// <para>zh-cn:列表中存储的元素类型。</para>
    /// <para>en-us:The type of elements stored in the list.</para>
    /// </typeparam>
    public class ConcurrentList<T> : IList<T>, IDisposable
    {
        private readonly IList<T> _list = new List<T>();

        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

        private bool _disposed;

        #region  构造函数
        /// <summary>
        /// <para>zh-cn:初始化一个空的线程安全列表。</para>
        /// <para>en-us:Initializes an empty thread-safe list.</para>
        /// </summary>
        public ConcurrentList()
        {

        }

        /// <summary>
        /// <para>zh-cn:使用指定集合初始化线程安全列表，并复制传入集合以避免外部列表实例被共享修改。</para>
        /// <para>en-us:Initializes a thread-safe list with the specified collection and copies the input collection to avoid sharing mutations with the external list instance.</para>
        /// </summary>
        /// <param name="values">
        /// <para>zh-cn:用于初始化列表的元素集合；为 `null` 时创建空列表。</para>
        /// <para>en-us:The element collection used to initialize the list; when `null`, an empty list is created.</para>
        /// </param>
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

        /// <summary>
        /// <para>zh-cn:将指定集合中的元素批量添加到当前线程安全列表末尾。</para>
        /// <para>en-us:Adds the elements from the specified collection to the end of the current thread-safe list.</para>
        /// </summary>
        /// <param name="values">
        /// <para>zh-cn:需要添加的元素集合。</para>
        /// <para>en-us:The element collection to add.</para>
        /// </param>
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

        /// <summary>
        /// <para>zh-cn:将元素添加到线程安全列表末尾。</para>
        /// <para>en-us:Adds an element to the end of the thread-safe list.</para>
        /// </summary>
        /// <param name="item">
        /// <para>zh-cn:需要添加的元素。</para>
        /// <para>en-us:The element to add.</para>
        /// </param>
        public void Add(T item) => ConcurrentAction(l => l.Add(item));


        /// <summary>
        /// <para>zh-cn:从线程安全列表中移除第一次出现的指定元素。</para>
        /// <para>en-us:Removes the first occurrence of the specified element from the thread-safe list.</para>
        /// </summary>
        /// <param name="item">
        /// <para>zh-cn:需要移除的元素。</para>
        /// <para>en-us:The element to remove.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:如果成功移除元素则返回 `true`；否则返回 `false`。</para>
        /// <para>en-us:Returns `true` when the element is successfully removed; otherwise returns `false`.</para>
        /// </returns>
        public bool Remove(T item) => ConcurrentFunc(x => x.EnterWriteLock(), x => x.ExitWriteLock(), l => l.Remove(item));


        /// <summary>
        /// <para>zh-cn:移除线程安全列表中的所有元素。</para>
        /// <para>en-us:Removes all elements from the thread-safe list.</para>
        /// </summary>
        public void Clear() => ConcurrentAction(l => l.Clear());


        /// <summary>
        /// <para>zh-cn:判断线程安全列表是否包含指定元素。</para>
        /// <para>en-us:Determines whether the thread-safe list contains the specified element.</para>
        /// </summary>
        /// <param name="item">
        /// <para>zh-cn:需要判断的元素。</para>
        /// <para>en-us:The element to locate.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:如果列表包含该元素则返回 `true`；否则返回 `false`。</para>
        /// <para>en-us:Returns `true` when the list contains the element; otherwise returns `false`.</para>
        /// </returns>
        public bool Contains(T item) => ConcurrentFunc(l => l.Contains(item));


        /// <summary>
        /// <para>zh-cn:从指定数组索引开始，将线程安全列表的元素复制到数组中。</para>
        /// <para>en-us:Copies the elements of the thread-safe list to an array, starting at the specified array index.</para>
        /// </summary>
        /// <param name="array">
        /// <para>zh-cn:接收复制元素的一维数组。</para>
        /// <para>en-us:The one-dimensional array that receives the copied elements.</para>
        /// </param>
        /// <param name="arrayIndex">
        /// <para>zh-cn:数组中从零开始的复制起始索引。</para>
        /// <para>en-us:The zero-based index in the array at which copying begins.</para>
        /// </param>
        public void CopyTo(T[] array, int arrayIndex) => ConcurrentAction(x => x.EnterReadLock(), x => x.ExitReadLock(), l => l.CopyTo(array, arrayIndex));


        /// <summary>
        /// <para>zh-cn:获取线程安全列表中包含的元素数量。</para>
        /// <para>en-us:Gets the number of elements contained in the thread-safe list.</para>
        /// </summary>
        public int Count => ConcurrentFunc(l => l.Count);


        /// <summary>
        /// <para>zh-cn:获取线程安全列表是否为只读集合。</para>
        /// <para>en-us:Gets whether the thread-safe list is read-only.</para>
        /// </summary>
        public bool IsReadOnly => ConcurrentFunc(l => l.IsReadOnly);


        /// <summary>
        /// <para>zh-cn:查找指定元素在线程安全列表中的索引。</para>
        /// <para>en-us:Searches for the specified element and returns its index in the thread-safe list.</para>
        /// </summary>
        /// <param name="item">
        /// <para>zh-cn:需要查找的元素。</para>
        /// <para>en-us:The element to locate.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:如果找到该元素，则返回其从零开始的索引；否则返回 -1。</para>
        /// <para>en-us:The zero-based index of the element when found; otherwise, -1.</para>
        /// </returns>
        public int IndexOf(T item) => ConcurrentFunc(l => l.IndexOf(item));


        /// <summary>
        /// <para>zh-cn:将元素插入到线程安全列表的指定索引处。</para>
        /// <para>en-us:Inserts an element into the thread-safe list at the specified index.</para>
        /// </summary>
        /// <param name="index">
        /// <para>zh-cn:元素应插入的位置索引。</para>
        /// <para>en-us:The index at which the element should be inserted.</para>
        /// </param>
        /// <param name="item">
        /// <para>zh-cn:需要插入的元素。</para>
        /// <para>en-us:The element to insert.</para>
        /// </param>
        public void Insert(int index, T item) => ConcurrentAction(l => l.Insert(index, item));


        /// <summary>
        /// <para>zh-cn:移除线程安全列表中指定索引处的元素。</para>
        /// <para>en-us:Removes the element at the specified index from the thread-safe list.</para>
        /// </summary>
        /// <param name="index">
        /// <para>zh-cn:需要移除的元素索引。</para>
        /// <para>en-us:The index of the element to remove.</para>
        /// </param>
        public void RemoveAt(int index) => ConcurrentAction(l => l.RemoveAt(index));


        /// <summary>
        /// <para>zh-cn:获取或设置线程安全列表中指定索引处的元素。</para>
        /// <para>en-us:Gets or sets the element at the specified index in the thread-safe list.</para>
        /// </summary>
        /// <param name="index">
        /// <para>zh-cn:需要访问的元素索引。</para>
        /// <para>en-us:The index of the element to access.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:指定索引处的元素。</para>
        /// <para>en-us:The element at the specified index.</para>
        /// </returns>
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

        /// <summary>
        /// <para>zh-cn:释放当前线程安全列表使用的读写锁资源。</para>
        /// <para>en-us:Releases the reader-writer lock resources used by the current thread-safe list.</para>
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// <para>zh-cn:释放当前线程安全列表占用的托管资源。</para>
        /// <para>en-us:Releases the managed resources held by the current thread-safe list.</para>
        /// </summary>
        /// <param name="disposing">
        /// <para>zh-cn:是否释放托管资源。</para>
        /// <para>en-us:Whether managed resources should be released.</para>
        /// </param>
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






