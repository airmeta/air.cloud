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
using Air.Cloud.Core.Enums;

using System.Net;

/*
    通讯标准定义了在微服务架构下的多个API之间的通讯动作
    通讯标准的定义是为了保证在不同的API之间的通讯是无障碍的
    在通讯标准中定义了API之间的通讯协议、通讯数据格式、通讯数据的加密解密等
    通讯标准不对使用何种网络架构进行限制，只对通讯的数据格式与代码层面的规范
    至于你是使用Netty还是Akka.Net还是其他的网络架构都是可以的
    
    该标准将会完全接纳Taxin 的标准,并对其非Http通讯的部分进行补充
    在Taxin的标准中，对于Http通讯的部分已经有了很好的定义，但是对于非Http通讯的部分并没有很好的定义
    在完成该标准后,Taxin将会支持多个API之间的并行计算能力,配合并行计算标准
    整个框架将会完全支持在大数据层面的应用,
        例如: 大规模的数据查询与分析,多个数据中心之间的数据同步,多个数据源之间并行查询与计算等功能
 */
namespace Air.Cloud.Core.Standard.ClairvoyanceStandard
{
    /// <summary>
    /// <para>zh-cn:定义 Clairvoyance 通讯标准，用于描述微服务或多 API 节点之间的客户端注入、上线、下线和消息发送行为。</para>
    /// <para>en-us:Defines the Clairvoyance communication standard, describing client injection, online, offline, and message-sending behaviors between microservices or API nodes.</para>
    /// </summary>
    public  interface IClairvoyanceStandard
    {
        /// <summary>
        /// <para>zh-cn:执行客户端初始化或注入操作，并返回当前通讯客户端信息。</para>
        /// <para>en-us:Executes client initialization or injection and returns the current communication client information.</para>
        /// </summary>
        /// <returns><para>zh-cn:完成初始化后的客户端信息。</para><para>en-us:The client information after initialization.</para></returns>
        public IClairvoyanceClient Inject();
        /// <summary>
        /// <para>zh-cn:将当前客户端标记或注册为在线状态。</para>
        /// <para>en-us:Marks or registers the current client as online.</para>
        /// </summary>
        /// <returns><para>zh-cn:上线后的客户端信息。</para><para>en-us:The client information after going online.</para></returns>
        public IClairvoyanceClient OnLine();
        /// <summary>
        /// <para>zh-cn:将当前客户端标记或注册为下线状态。</para>
        /// <para>en-us:Marks or registers the current client as offline.</para>
        /// </summary>
        /// <returns><para>zh-cn:下线后的客户端信息。</para><para>en-us:The client information after going offline.</para></returns>
        public IClairvoyanceClient UnderLine();

        /// <summary>
        /// <para>zh-cn:向指定客户端发送点对点消息。</para>
        /// <para>en-us:Sends a point-to-point message to the specified client.</para>
        /// </summary>
        /// <param name="message"><para>zh-cn:需要发送的通讯消息。</para><para>en-us:The communication message to send.</para></param>
        /// <param name="instance"><para>zh-cn:消息目标客户端。</para><para>en-us:The target client of the message.</para></param>
        /// <returns><para>zh-cn:表示异步发送操作的任务，结果为目标客户端返回的消息回执。</para><para>en-us:A task representing the asynchronous send operation, with the message callback returned by the target client as the result.</para></returns>
        public Task<IClairvoyanceMessageCallback> SendAsync(IClairvoyanceMessage message, IClairvoyanceClient instance);

        /// <summary>
        /// <para>zh-cn:向指定客户端组发送消息。</para>
        /// <para>en-us:Sends a message to the specified client group.</para>
        /// </summary>
        /// <param name="message"><para>zh-cn:需要发送的通讯消息。</para><para>en-us:The communication message to send.</para></param>
        /// <param name="group"><para>zh-cn:消息目标客户端组。</para><para>en-us:The target client group of the message.</para></param>
        /// <returns><para>zh-cn:表示异步发送操作的任务，结果为目标组返回的消息回执。</para><para>en-us:A task representing the asynchronous send operation, with the message callback returned by the target group as the result.</para></returns>
        public Task<IClairvoyanceMessageCallback> SendAsync(IClairvoyanceMessage message, IClairvoyanceGroup group);

        /// <summary>
        /// <para>zh-cn:向多个指定客户端发送一对多消息。</para>
        /// <para>en-us:Sends a one-to-many message to multiple specified clients.</para>
        /// </summary>
        /// <param name="message"><para>zh-cn:需要发送的通讯消息。</para><para>en-us:The communication message to send.</para></param>
        /// <param name="instances"><para>zh-cn:消息目标客户端集合。</para><para>en-us:The target client collection of the message.</para></param>
        /// <returns><para>zh-cn:表示异步发送操作的任务，结果为各目标客户端返回的消息回执集合。</para><para>en-us:A task representing the asynchronous send operation, with the message callbacks returned by target clients as the result.</para></returns>
        public Task<ICollection<IClairvoyanceMessageCallback>> SendAsync(IClairvoyanceMessage message, IList<IClairvoyanceClient> instances);

    }
    /// <summary>
    /// <para>zh-cn:消息</para>
    /// <para>en-us:Message</para>
    /// </summary>
    public interface IClairvoyanceMessage
    {
        /// <summary>
        /// <para>zh-cn:获取或设置消息唯一标识。</para>
        /// <para>en-us:Gets or sets the unique message identifier.</para>
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// <para>zh-cn:获取或设置消息正文二进制内容。</para>
        /// <para>en-us:Gets or sets the binary message body.</para>
        /// </summary>
        public byte[] Body {  get; set; }
        /// <summary>
        /// <para>zh-cn:获取或设置发送该消息的客户端。</para>
        /// <para>en-us:Gets or sets the client that sends the message.</para>
        /// </summary>
        public IClairvoyanceClient SendClient { get; set; }
    }
    /// <summary>
    /// <para>zh-cn:消息回执</para>
    /// <para>en-us:Message callback</para>
    /// </summary>
    public interface IClairvoyanceMessageCallback
    {
        /// <summary>
        /// <para>zh-cn:获取或设置消息回执唯一标识。</para>
        /// <para>en-us:Gets or sets the unique message callback identifier.</para>
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// <para>zh-cn:获取或设置该回执对应的原始消息标识。</para>
        /// <para>en-us:Gets or sets the original message identifier associated with this callback.</para>
        /// </summary>
        public string MessageId { get; set; }

        /// <summary>
        /// <para>zh-cn:获取或设置回执正文二进制内容。</para>
        /// <para>en-us:Gets or sets the binary callback body.</para>
        /// </summary>
        public byte[] Body { get; set; }

        /// <summary>
        /// <para>zh-cn:获取或设置接收消息并产生回执的客户端。属性名保留历史拼写 `ReciveClient`。</para>
        /// <para>en-us:Gets or sets the client that received the message and produced the callback. The property name keeps the historical spelling `ReciveClient`.</para>
        /// </summary>
        public IClairvoyanceClient ReciveClient { get; set; }
    }

    /// <summary>
    /// <para>zh-cn:组信息</para>
    /// <para>en-us:Clairvoyance group</para>
    /// </summary>
    public interface IClairvoyanceGroup
    {
        /// <summary>
        /// <para>zh-cn:获取或设置客户端组唯一标识。</para>
        /// <para>en-us:Gets or sets the unique client group identifier.</para>
        /// </summary>
        public string Id { get; set; }
    }
    /// <summary>
    /// <para>zh-cn:客户端信息</para>
    /// <para>en-us:Client information</para>
    /// </summary>
    public interface IClairvoyanceClient
    {
        /// <summary>
        /// <para>zh-cn:获取或设置客户端唯一标识。</para>
        /// <para>en-us:Gets or sets the unique client identifier.</para>
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// <para>zh-cn:获取或设置客户端通讯端口。</para>
        /// <para>en-us:Gets or sets the client communication port.</para>
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// <para>zh-cn:获取或设置客户端通讯 IP 地址。</para>
        /// <para>en-us:Gets or sets the client communication IP address.</para>
        /// </summary>
        public IPAddress IPAddress { get; set; }

        /// <summary>
        /// <para>zh-cn:获取或设置客户端当前应用状态。</para>
        /// <para>en-us:Gets or sets the current application state of the client.</para>
        /// </summary>
        public AppStateEnum State { get; set; }

        /// <summary>
        /// <para>zh-cn:获取或设置客户端所属的通讯组。</para>
        /// <para>en-us:Gets or sets the communication group to which the client belongs.</para>
        /// </summary>
        public IClairvoyanceGroup Group { get; set; }
    }
}
