using Air.Cloud.Core;
using Air.Cloud.Core.Collections;
using Air.Cloud.Core.Modules.AppPrint;
using Air.Cloud.Core.Modules.StandardPool;

using Confluent.Kafka;

using System.Collections.Concurrent;

namespace Air.Cloud.Modules.Kafka.Pool
{
    internal class ProducerPool : IAppPool<Tuple<string, IProducer<int, string>>>
    {
        /// <summary>
        /// 对象池存储
        /// </summary>
        private static ConcurrentDictionary<string, Tuple<string,IProducer<int, string>>> Pool = new ConcurrentDictionary<string, Tuple<string, IProducer<int, string>>>();
        /// <summary>
        /// 锁定的对象
        /// </summary>
        private static ConcurrentList<string> LockPool = new ConcurrentList<string>();
        /// <summary>
        /// 池操作锁
        /// </summary>
        private static object PoolLockTag = new object();

        /// <inheritdoc/>
        public Tuple<string, IProducer<int, string>> Get(string Key)
        {
            if (Pool.TryGetValue(Key, out var producer)) return producer;
            return null;
        }
        /// <inheritdoc/>
        public bool Set(Tuple<string, IProducer<int, string>> producer)
        {
            if (LockPool.Contains(producer.Item1))
            {
                //当前对象已经被锁定 无法进行修改
                AppRealization.Output.Print(new AppPrintInformation()
                {
                    State = true,
                    AdditionalParams = null,
                    Content = "Current es client is locked",
                    Title = "KafkaProducerPool Notice",
                    Level = AppPrintLevel.Warning
                });
                return false;
            }
            Pool.AddOrUpdate(producer.Item1, producer, (key, value) => value);
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
    }
}
