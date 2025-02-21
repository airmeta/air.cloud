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
    /// 消息订阅标准
    /// </summary>
    /// <typeparam name="TMessage">
    ///  Message type
    /// </typeparam>
    public interface IMessageQueueSubscribeStandard<TMessage> : IStandard
     where TMessage : class
    {
        /// <summary>
        /// <para>zh-cn:订阅方法名称</para>
        /// <para>en-us:Subscribe method name</para>
        /// </summary>
        public const string SUBSCRIBE_METHOD = "Subscribe";
        /// <summary>
        /// <para>zh-cn:订阅消息处理</para>
        /// <para>en-us:Subscribe queue</para>
        /// </summary>

        /// <param name="message">
        ///  <para>zh-cn:订阅到的消息</para>
        ///  <para>en-us:Message</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:消息回复(可为null)</para>
        /// <para>en-us: Recive message(can be null)</para>
        /// </returns>
        public object Subscribe(TMessage message);
    }
}
