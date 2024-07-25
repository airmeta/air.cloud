using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Air.Cloud.Modules.Kafka.Topic
{
    /// <summary>
    ///  主题信息
    /// </summary>
    public class TopicInfo
    {
        #region  主题基础信息
        /// <summary>
        /// 主题名称
        /// </summary>
        public string TopicName { get; set; }

        /// <summary>
        /// 分区数量
        /// </summary>
        public int NumPartitions { get; set; }

        /// <summary>
        ///  副本数量
        /// </summary>
        public int NumReplications { get; set; }
        /// <summary>
        /// 连接信息
        /// </summary>
        public string BoorStrapServers { get; set; }
        #endregion

        #region 模板信息

        /// <summary>
        /// 创建模板信息 所有信息
        /// </summary>
        public TopicTemplateInfo TemplateInfo { get; set; }

        #endregion

        public TopicInfo() { }

        public TopicInfo(TopicTemplateInfo templateInfo)
        {

            TopicName = templateInfo.Topic.Name;
            NumPartitions = templateInfo.Topic.NumPartitions;
            NumReplications = templateInfo.Topic.ReplicationFactor;
            BoorStrapServers = templateInfo.Config.BootstrapServers;
            TemplateInfo = templateInfo;
        }
    }
}
