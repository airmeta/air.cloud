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
namespace Air.Cloud.Modules.RocketMQ.Model
{
    /// <summary>
    /// <para>zh-cn:RocketMQ 生产者配置，包含客户端基础配置、发送最大尝试次数、标签、顺序消息组和用户属性。</para>
    /// <para>en-us:RocketMQ producer configuration that includes base client settings, maximum send attempts, tag, FIFO message group, and user properties.</para>
    /// </summary>
    public class RocketMQProducerConfig : RocketMQClientConfig
    {
        /// <summary>
        /// <para>zh-cn:发送最大尝试次数；默认 3。</para>
        /// <para>en-us:Maximum send attempts. The default is 3.</para>
        /// </summary>
        public int MaxAttempts { get; set; } = 3;

        /// <summary>
        /// <para>zh-cn:消息标签；为空时不设置 Tag。</para>
        /// <para>en-us:Message tag. When empty, no tag is set.</para>
        /// </summary>
        public string Tag { get; set; }

        /// <summary>
        /// <para>zh-cn:顺序消息组；设置后消息按 RocketMQ FIFO 语义发送。</para>
        /// <para>en-us:FIFO message group. When set, messages are sent with RocketMQ FIFO semantics.</para>
        /// </summary>
        public string MessageGroup { get; set; }

        /// <summary>
        /// <para>zh-cn:消息用户属性，发布时逐项写入 RocketMQ Message。</para>
        /// <para>en-us:Message user properties written to the RocketMQ message during publishing.</para>
        /// </summary>
        public Dictionary<string, string> Properties { get; set; }
    }
}
