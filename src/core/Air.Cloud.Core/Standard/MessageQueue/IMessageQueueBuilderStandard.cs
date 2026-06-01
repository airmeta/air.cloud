/*
 * Copyright (c) 2024-2030 星曳数据
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 *
 * This file is provided under the Mozilla Public License Version 2.0,
 * and the "NO WARRANTY" clause of the MPL is hereby expressly
 * acknowledged.
 */
namespace Air.Cloud.Core.Standard.MessageQueue
{
    /// <summary>
    /// <para>zh-cn:定义消息队列构建器标准，用于生成消费者组等消息队列运行时标识。</para>
    /// <para>en-us:Defines the message-queue builder contract used to generate runtime identifiers such as consumer groups.</para>
    /// </summary>
    public interface IMessageQueueBuilderStandard
    {
        /// <summary>
        /// <para>zh-cn:消费者组格式化模板集合。</para>
        /// <para>en-us:The consumer group formatting template collection.</para>
        /// </summary>
        public static string[] GroupFormatter = new string[]{
            "{GroupId}_{VerisonDistinguish}_{EnvironmentDistinguish}",
            "{GroupId}_{EnvironmentDistinguish}",
            "{GroupId}_{VerisonDistinguish}",
        };
        /// <summary>
        /// <para>zh-cn:构建消息队列消费者组标识。</para>
        /// <para>en-us:Builds a message-queue consumer group identifier.</para>
        /// </summary>
        /// <param name="GroupId">
        /// <para>zh-cn:基础消费者组标识；未提供时默认使用当前应用名称，开发环境下可能使用临时标识。</para>
        /// <para>en-us:The base consumer group identifier. When not provided, the current application name is used by default, and development environments may use a temporary identifier.</para>
        /// </param>
        /// <param name="EnvironmentDistinguish">
        /// <para>zh-cn:是否将环境信息纳入消费者组区分。</para>
        /// <para>en-us:Whether environment information should be included to distinguish the consumer group.</para>
        /// </param>
        /// <param name="VerisonDistinguish">
        /// <para>zh-cn:是否将版本信息纳入消费者组区分。参数名保留历史拼写。</para>
        /// <para>en-us:Whether version information should be included to distinguish the consumer group. The parameter name keeps the historical spelling.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:生成后的消费者组标识。</para>
        /// <para>en-us:The generated consumer group identifier.</para>
        /// </returns>
        public string BuildGroupId(
            string GroupId=null,
            bool EnvironmentDistinguish=true,
            bool VerisonDistinguish = false);

    }

    /// <summary>
    /// <para>zh-cn:提供默认消息队列构建器实现。</para>
    /// <para>en-us:Provides the default message-queue builder implementation.</para>
    /// </summary>
    public class MessageQueueBuilderStandard : IMessageQueueBuilderStandard
    {
        /// <summary>
        /// <para>zh-cn:构建消息队列消费者组标识。</para>
        /// <para>en-us:Builds a message-queue consumer group identifier.</para>
        /// </summary>
        /// <param name="GroupId"><para>zh-cn:基础消费者组标识。</para><para>en-us:The base consumer group identifier.</para></param>
        /// <param name="EnvironmentDistinguish"><para>zh-cn:是否区分环境。</para><para>en-us:Whether to distinguish by environment.</para></param>
        /// <param name="VerisonDistinguish"><para>zh-cn:是否区分版本。参数名保留历史拼写。</para><para>en-us:Whether to distinguish by version. The parameter name keeps the historical spelling.</para></param>
        /// <returns><para>zh-cn:生成后的消费者组标识。</para><para>en-us:The generated consumer group identifier.</para></returns>
        public string BuildGroupId(string GroupId = null, bool EnvironmentDistinguish = true, bool VerisonDistinguish = false)
        {
            //if (AppEnvironment.IsDevelopment&&)
            //{

            //}
            GroupId = GroupId ?? (AppEnvironment.IsDevelopment ? Guid.NewGuid().ToString() : AppConst.ApplicationName);
            return GroupId;
        }
    }

}
