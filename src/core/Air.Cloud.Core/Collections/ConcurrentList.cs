using System.Collections;

/**
    由于C# 自带的 ConcurrentBag 没有删除特定元素的方法，所以本框架采集了CSDN中的一个ConcurrentList的实现
    原文链接: https://blog.csdn.net/weixin_43542114/article/details/142952583
    作者: 望天House
    说明:
        该类为CSDN作者的原创作品,已获得该作者的授权,如有侵权请联系本框架作者删除
 */

namespace Air.Cloud.Core.Collections
{
    public class ConcurrentList<T> : IList<T>
    {
        private readonly IList<T> _list = new List<T>();

        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

        #region  构造函数
        public ConcurrentList()
        {

        }

        public ConcurrentList(List<T> values)
        {
            _list = values;
        }

        #endregion

        private void ConcurrentAction(Action<IList<T>> action)
        {
            ConcurrentAction(x => x.EnterWriteLock(), x => x.ExitWriteLock(), action);
        }

        private void ConcurrentAction(Action<ReaderWriterLockSlim> enter
              , Action<ReaderWriterLockSlim> exit, Action<IList<T>> action)
        {
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


        public bool Remove(T item) => ConcurrentFunc(l => l.Remove(item));


        public void Clear() => ConcurrentAction(l => l.Clear());


        public bool Contains(T item) => ConcurrentFunc(l => l.Contains(item));


        public void CopyTo(T[] array, int arrayIndex) => ConcurrentAction(l => l.CopyTo(array, arrayIndex));


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
            return new ConcurrentEnumerator<T>(_list, _lock);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return new ConcurrentEnumerator<T>(_list, _lock);
        }
    }
    class ConcurrentEnumerator<T> : System.Collections.Generic.IEnumerator<T>
    {
        private readonly ReaderWriterLockSlim _lock;

        private readonly IEnumerator<T> _enumerator;

        internal ConcurrentEnumerator(IEnumerable<T> target, ReaderWriterLockSlim lockSlim)
        {
            _lock = lockSlim;
            _lock.EnterReadLock();
            _enumerator = target.GetEnumerator();
        }

        public T Current => _enumerator.Current;

        public bool MoveNext()
        {
            return _enumerator.MoveNext();
        }
        public void Reset()
        {
            _enumerator.Reset();
        }
        public void Dispose()
        {
            _lock.ExitReadLock();
        }
        object IEnumerator.Current => Current;
    }
}






