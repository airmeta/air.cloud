/*
 * Copyright (c) 2024 星曳数据
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 *
 * This file is provided under the Mozilla Public License Version 2.0,
 * and the "NO WARRANTY" clause of the MPL is hereby expressly
 * acknowledged.
 */
using Air.Cloud.Core.App;
using Air.Cloud.Modules.Kafka.Config;
using Air.Cloud.Modules.Kafka.Topic;

using Confluent.Kafka;
using Confluent.Kafka.Admin;

namespace Air.Cloud.Modules.Kafka.Helper
{
    /// <summary>
    /// kafka 管理器
    /// </summary>
    public class KafkaManager 
    {
        private KafkaSettingsOptions Options => AppCore.GetOptions<KafkaSettingsOptions>();
        #region 创建topic
        /// <summary>
        /// 创建Topic
        /// </summary>
        /// <param name="topicName">topic名称</param>
        /// <param name="template">模板信息</param>
        /// <returns></returns>
        public async Task<TopicCreateResult> CreateTopic(string topicName, TopicTemplateInfo template)
        {
            template = template == null ? Options.TopicTemplateInfo : template;
            using (var adminClient = new AdminClientBuilder(template.Config).Build())
            {
                try
                {
                    if (!string.IsNullOrEmpty(topicName))
                    {
                        template.Topic.Name = topicName;
                    }
                    await adminClient.CreateTopicsAsync(new TopicSpecification[]
                    {
                        template.Topic
                    },
                    new CreateTopicsOptions()
                    {
                        ValidateOnly = true
                    });
                    //后置检查 topic信息 如果能查到这个topic 表示创建成功 查不到表示创建失败 
                    Metadata data = GetTopic(template.Topic.Name, template.Config, 10);
                    return new TopicCreateResult()
                    {
                        State = data.Topics.Count > 0,
                        Message = data.Topics.Count > 0 ? data.Topics[0].Error.Reason : "创建失败!",
                        Info = new TopicInfo(template)
                    };
                }
                catch (CreateTopicsException e)
                {
                    return new TopicCreateResult()
                    {
                        State = false,
                        Message = e.Message
                    };
                }
            }
        }

        #endregion

        #region  topic 信息查询
        /// <summary>
        /// 获取所有的Topic信息,节点信息
        /// </summary>
        /// <remarks>
        ///  通过这个方法可以获取到kafka的节点信息 以及 所有topic信息
        /// </remarks>
        /// <param name="config">连接信息</param>
        public Metadata GetAllTopic(AdminClientConfig config, int seconds = 10)
        {
            using (var adminClient = new AdminClientBuilder(config).Build())
            {
                try
                {
                    return adminClient.GetMetadata(new TimeSpan(0, 0, seconds));
                }
                catch (CreateTopicsException)
                {
                    return null;
                }
            }
        }
        /// <summary>
        /// 获取指定名称的topic
        /// </summary>
        /// <param name="config"></param>
        public Metadata GetTopic(string topicName, AdminClientConfig config, int seconds = 10)
        {
            using (var adminClient = new AdminClientBuilder(config).Build())
            {
                try
                {
                    return adminClient.GetMetadata(topicName, new TimeSpan(0, 0, seconds));
                }
                catch (CreateTopicsException)
                {
                    return null;
                }
            }
        }
        #endregion
    }
}
