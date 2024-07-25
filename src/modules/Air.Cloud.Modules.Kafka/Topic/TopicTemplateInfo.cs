using Confluent.Kafka;
using Confluent.Kafka.Admin;

namespace Air.Cloud.Modules.Kafka.Topic
{
    /// <summary>
    /// topic模板配置
    /// </summary>
    public class TopicTemplateInfo
    {
        /// <summary>
        /// 主题信息
        /// </summary>
        public TopicSpecification Topic { get; set; } = new TopicSpecification();

        /// <summary>
        /// 连接信息
        /// </summary>
        public AdminClientConfig Config { get; set; } = new AdminClientConfig();
    }
}
