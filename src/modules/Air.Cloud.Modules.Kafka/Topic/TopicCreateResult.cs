namespace Air.Cloud.Modules.Kafka.Topic
{
    public class TopicCreateResult
    {
        /// <summary>
        /// topic 创建 结果
        /// </summary>
        public bool State { get; set; } = false;
        /// <summary>
        /// 提示信息
        /// </summary>
        public string Message { get; set; } = "创建失败";

        /// <summary>
        /// 创建成功之后的topic信息
        /// </summary>
        public TopicInfo Info { get; set; } = null;
    }
}
