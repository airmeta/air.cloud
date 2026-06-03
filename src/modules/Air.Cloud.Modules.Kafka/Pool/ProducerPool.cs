using Air.Cloud.Core;
using Air.Cloud.Core.Collections;
using Air.Cloud.Core.Modules.AppPrint;

using System.Collections.Concurrent;

namespace Air.Cloud.Modules.Kafka.Pool
{
    internal class ProducerPool
    {
        /// <summary>
        /// <para>zh-cn:对象池存储，缓存 Key 由 TopicName 与消息 Key 类型共同组成。</para>
        /// <para>en-us:Object pool storage. The cache key is composed from TopicName and the message key type.</para>
        /// </summary>
        private static ConcurrentDictionary<string, object> Pool = new ConcurrentDictionary<string, object>();

        /// <summary>
        /// <para>zh-cn:锁定的对象 Key 集合。</para>
        /// <para>en-us:The locked object key collection.</para>
        /// </summary>
        private static ConcurrentList<string> LockPool = new ConcurrentList<string>();

        /// <summary>
        /// <para>zh-cn:池操作锁。</para>
        /// <para>en-us:Pool operation lock.</para>
        /// </summary>
        private static object PoolLockTag = new object();

        /// <summary>
        /// <para>zh-cn:根据缓存 Key 获取 Kafka 生产者实例。</para>
        /// <para>en-us:Gets a Kafka producer instance by the cache key.</para>
        /// </summary>
        /// <param name="Key">
        /// <para>zh-cn:缓存 Key。</para>
        /// <para>en-us:The cache key.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:Kafka 生产者实例。</para>
        /// <para>en-us:The Kafka producer instance.</para>
        /// </returns>
        public object Get(string Key)
        {
            if (Pool.TryGetValue(Key, out var producer)) return producer;
            return null;
        }

        /// <summary>
        /// <para>zh-cn:设置 Kafka 生产者实例。</para>
        /// <para>en-us:Sets a Kafka producer instance.</para>
        /// </summary>
        /// <param name="Key">
        /// <para>zh-cn:缓存 Key。</para>
        /// <para>en-us:The cache key.</para>
        /// </param>
        /// <param name="producer">
        /// <para>zh-cn:Kafka 生产者实例。</para>
        /// <para>en-us:The Kafka producer instance.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:是否设置成功。</para>
        /// <para>en-us:Whether the producer was set successfully.</para>
        /// </returns>
        public bool Set(string Key, object producer)
        {
            if (LockPool.Contains(Key))
            {
                //当前对象已经被锁定 无法进行修改
                AppRealization.Output.Print(new AppPrintInformation()
                {
                    State = true,
                    AdditionalParams = null,
                    Content = "Current es client is locked",
                    Title = "KafkaProducerPool Notice",
                    Level = AppPrintLevel.Warn
                });
                return false;
            }
            Pool.AddOrUpdate(Key, producer, (key, value) => value);
            return true;
        }

        /// <summary>
        /// <para>zh-cn:从生产者池中移除指定缓存 Key 的生产者实例。</para>
        /// <para>en-us:Removes the producer instance with the specified cache key from the producer pool.</para>
        /// </summary>
        /// <param name="Key">
        /// <para>zh-cn:缓存 Key。</para>
        /// <para>en-us:The cache key.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:是否移除成功。</para>
        /// <para>en-us:Whether the producer was removed successfully.</para>
        /// </returns>
        public bool Remove(string Key)
        {
            return Pool.TryRemove(Key, out var standard);
        }

        /// <summary>
        /// <para>zh-cn:清空生产者池。</para>
        /// <para>en-us:Clears the producer pool.</para>
        /// </summary>
        public void Clear()
        {
            lock (PoolLockTag)
            {
                Pool.Clear();
                LockPool.Clear();
            }
        }
    }
}
