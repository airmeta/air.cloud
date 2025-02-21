using Air.Cloud.Core;
using Air.Cloud.Core.Standard.Event;
using Air.Cloud.Modules.Kafka.Model;
using Air.Cloud.Modules.Nexus.Publishers;
using Confluent.Kafka;
using System.Threading.Channels;

namespace unit.kafaka.server.entry
{
    public class KafkaEventSourceStorager : IEventSourceStorager
    {
        /// <summary>
        /// 内存通道事件源存储器
        /// </summary>
        private readonly Channel<IEventSource> _channel;
        private readonly ConsumerConfigModel _consumerConfigModel;
        private readonly ProducerConfigModel _producerConfigModel;
        private readonly string _groupId;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="producerConfigModel"></param>
        /// <param name="groupId"></param>
        public KafkaEventSourceStorager(ProducerConfigModel producerConfigModel, ConsumerConfigModel consumerConfigModel, string groupId)
        {
            _producerConfigModel = producerConfigModel;
            _consumerConfigModel = consumerConfigModel;
            _groupId = groupId;
            // 配置通道，设置超出默认容量后进入等待
            var boundedChannelOptions = new BoundedChannelOptions(int.MaxValue)
            {
                FullMode = BoundedChannelFullMode.Wait
            };

            // 创建无限限容量通道
            _channel = Channel.CreateBounded<IEventSource>(boundedChannelOptions);

            AppRealization.Queue.Subscribe<ConsumerConfig, object>(_consumerConfigModel, async (s) =>
            {
                var josn = AppRealization.JSON.Serialize(s);
                IEventSource eventSource = AppRealization.JSON.Deserialize<DefaultEventSource>(josn);
                // 写入存储器
                await _channel.Writer.WriteAsync(eventSource, default);
            }, _groupId);
        }

        /// <summary>
        /// 将事件源写入存储器
        /// </summary>
        /// <param name="eventSource">事件源对象</param>
        /// <param name="cancellationToken">取消任务 Token</param>
        /// <returns><see cref="ValueTask"/></returns>
        public async ValueTask WriteAsync(IEventSource eventSource, CancellationToken cancellationToken)
        {
            // 空检查
            if (eventSource == default)
            {
                throw new ArgumentNullException(nameof(eventSource));
            }
            AppRealization.Queue.Publish<ProducerConfig, DefaultEventSource>(_producerConfigModel, eventSource as DefaultEventSource);
            await Task.CompletedTask;
        }

        /// <summary>
        /// 从存储器中读取一条事件源
        /// </summary>
        /// <param name="cancellationToken">取消任务 Token</param>
        /// <returns>事件源对象</returns>
        public async ValueTask<IEventSource> ReadAsync(CancellationToken cancellationToken)
        {
            // 读取一条事件源
            var eventSource = await _channel.Reader.ReadAsync(cancellationToken);
            return eventSource;
        }
    }
}
