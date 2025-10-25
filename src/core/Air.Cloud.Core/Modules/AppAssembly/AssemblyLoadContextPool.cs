using Air.Cloud.Core.Collections;
using Air.Cloud.Core.Modules.AppAssembly.Model;
using Air.Cloud.Core.Modules.StandardPool;

using System.Collections.Concurrent;
using System.Runtime.Loader;

namespace Air.Cloud.Core.Modules.AppAssembly
{
    /// <summary>
    /// <para>zh-cn:程序集加载上下文池</para>
    /// <para>en-us:Assembly Load Context Pool</para>
    /// </summary>
    public class AssemblyLoadContextPool : IAppPool<AssemblyLoadContextInformation>
    {
        /// <summary>
        /// 对象池存储
        /// </summary>
        private static ConcurrentDictionary<string, AssemblyLoadContextInformation> Pool = new ConcurrentDictionary<string, AssemblyLoadContextInformation>();
        /// <summary>
        /// 锁定的对象
        /// </summary>
        private static ConcurrentList<string> LockPool = new ConcurrentList<string>();
        /// <summary>
        /// 池操作锁
        /// </summary>
        private static object PoolLockTag = new object();

        /// <inheritdoc/>
        public AssemblyLoadContextInformation Get(string Key)
        {
            if (Pool.TryGetValue(Key, out var standard)) return standard;
            return null;
        }
        /// <inheritdoc/>
        public bool Set(AssemblyLoadContextInformation standard)
        {
            if (LockPool.Contains(standard.Name))
            {
                //当前对象已经被锁定 无法进行修改
                AppRealization.Output.Print(new AppPrintInformation()
                {
                    State = true,
                    AdditionalParams = null,
                    Content = "Current AssemblyLoadContext is locked",
                    Title = "AssemblyLoadContextPool Notice",
                    Level = AppPrintLevel.Warn
                });
                return false;
            }
            Pool.AddOrUpdate(standard.Name, standard, (key, value) => value);
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
        ///  <para>zh-cn:唯一键 </para>
        ///  <para>en-us:Unique key</para>
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
