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
using Org.Apache.Rocketmq;

namespace Air.Cloud.Modules.RocketMQ.Model
{
    /// <summary>
    /// <para>zh-cn:RocketMQ SimpleConsumer 配置，包含消费者组、过滤表达式、批量拉取数量、不可见时间和长轮询等待时间。</para>
    /// <para>en-us:RocketMQ SimpleConsumer configuration that includes consumer group, filter expression, batch size, invisible duration, and long-polling await duration.</para>
    /// </summary>
    public class RocketMQConsumerConfig : RocketMQClientConfig
    {
        /// <summary>
        /// <para>zh-cn:消费者组；调用 Subscribe 时传入的 GroupId 优先级更高。</para>
        /// <para>en-us:Consumer group. The GroupId passed to Subscribe has higher priority.</para>
        /// </summary>
        public string ConsumerGroup { get; set; }

        /// <summary>
        /// <para>zh-cn:订阅过滤表达式；默认 * 表示订阅全部消息。</para>
        /// <para>en-us:Subscription filter expression. The default * subscribes all messages.</para>
        /// </summary>
        public string FilterExpression { get; set; } = "*";

        /// <summary>
        /// <para>zh-cn:过滤表达式类型；默认按 Tag 过滤。</para>
        /// <para>en-us:Filter expression type. The default filters by tag.</para>
        /// </summary>
        public ExpressionType ExpressionType { get; set; } = ExpressionType.Tag;

        /// <summary>
        /// <para>zh-cn:每次拉取消息数量；默认 16。</para>
        /// <para>en-us:Number of messages received in one pull. The default is 16.</para>
        /// </summary>
        public int BatchSize { get; set; } = 16;

        /// <summary>
        /// <para>zh-cn:消息不可见时间；未确认消息超过该时间后可被再次投递。</para>
        /// <para>en-us:Message invisible duration. Unacknowledged messages may be redelivered after this duration.</para>
        /// </summary>
        public TimeSpan InvisibleDuration { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// <para>zh-cn:长轮询等待时间；默认 15 秒。</para>
        /// <para>en-us:Long-polling await duration. The default is 15 seconds.</para>
        /// </summary>
        public TimeSpan AwaitDuration { get; set; } = TimeSpan.FromSeconds(15);
    }
}
