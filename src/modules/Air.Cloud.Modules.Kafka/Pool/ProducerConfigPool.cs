using Air.Cloud.Core;
using Air.Cloud.Core.Collections;
using Air.Cloud.Core.Modules.AppPrint;
using Air.Cloud.Core.Modules.StandardPool;
using Air.Cloud.Core.Standard.MessageQueue.Config;

using Confluent.Kafka;

using System.Collections.Concurrent;

namespace Air.Cloud.Modules.Kafka.Pool
{
    public class ProducerConfigPool: IAppPool<ITopicPublishConfig<ProducerConfig>>
    {
        /// <summary>
        /// 对象池存储
        /// </summary>
        private static ConcurrentDictionary<string, ITopicPublishConfig<ProducerConfig>> Pool = new ConcurrentDictionary<string, ITopicPublishConfig<ProducerConfig>>();
        /// <summary>
        /// 锁定的对象
        /// </summary>
        private static ConcurrentList<string> LockPool = new ConcurrentList<string>();
        /// <summary>
        /// 池操作锁
        /// </summary>
        private static object PoolLockTag = new object();

        /// <inheritdoc/>
        public ITopicPublishConfig<ProducerConfig> Get(string Key)
        {
            if (Pool.TryGetValue(Key, out var producer)) return producer;
            return null;
        }
        /// <inheritdoc/>
        public bool Set(ITopicPublishConfig<ProducerConfig> producerConfigModel)
        {
            if (LockPool.Contains(producerConfigModel.TopicName))
            {
                //当前对象已经被锁定 无法进行修改
                AppRealization.Output.Print(new AppPrintInformation()
                {
                    State = true,
                    AdditionalParams = null,
                    Content = "Current es client is locked",
                    Title = "ITopicPublishConfig<ProducerConfig> Notice",
                    Level = AppPrintLevel.Warn
                });
                return false;
            }
            Pool.AddOrUpdate(producerConfigModel.TopicName, producerConfigModel, (key, value) => value);
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
