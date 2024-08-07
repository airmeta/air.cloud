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
using Air.Cloud.Core.Enums;

using System.Net;

/**
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
    /// <para>zh-cn:通讯标准</para>
    /// </summary>
    public  interface IClairvoyanceStandard
    {
        /// <summary>
        /// <para>zh-cn:初始化操作</para>
        /// <para>en-us:Client inject</para>
        /// </summary>
        /// <returns></returns>
        public IClairvoyanceClient Inject();
        /// <summary>
        /// <para>zh-cn:上线操作</para>
        /// <para>en-us:Client online</para>
        /// </summary>
        /// <returns></returns>
        public IClairvoyanceClient OnLine();
        /// <summary>
        /// <para>zh-cn:下线操作</para>
        /// <para>en-us:Client underline</para>
        /// </summary>
        /// <returns></returns>
        public IClairvoyanceClient UnderLine();

        /// <summary>
        /// 点对点
        /// </summary>
        /// <returns></returns>
        public Task<IClairvoyanceMessageCallback> SendAsync(IClairvoyanceMessage message, IClairvoyanceClient instance);

        /// <summary>
        /// 点对组
        /// </summary>
        /// <returns></returns>
        public Task<IClairvoyanceMessageCallback> SendAsync(IClairvoyanceMessage message, IClairvoyanceGroup group);

        /// <summary>
        /// 一对多
        /// </summary>
        /// <param name="message"></param>
        /// <param name="instances"></param>
        /// <returns></returns>
        public Task<ICollection<IClairvoyanceMessageCallback>> SendAsync(IClairvoyanceMessage message, IList<IClairvoyanceClient> instances);

    }
    /// <summary>
    /// <para>zh-cn:消息</para>
    /// <para>en-us:Message</para>
    /// </summary>
    public interface IClairvoyanceMessage
    {
        public string Id { get; set; }
        public byte[] Body {  get; set; }
        public IClairvoyanceClient SendClient { get; set; }
    }
    /// <summary>
    /// <para>zh-cn:消息回执</para>
    /// <para>en-us:Message callback</para>
    /// </summary>
    public interface IClairvoyanceMessageCallback
    {
        public string Id { get; set; }

        public string MessageId { get; set; }

        public byte[] Body { get; set; }

        public IClairvoyanceClient ReciveClient { get; set; }
    }

    /// <summary>
    /// <para>zh-cn:组信息</para>
    /// <para>en-us:Clairvoyance group</para>
    /// </summary>
    public interface IClairvoyanceGroup
    {
        public string Id { get; set; }
    }
    /// <summary>
    /// <para>zh-cn:客户端信息</para>
    /// <para>en-us:Client information</para>
    /// </summary>
    public interface IClairvoyanceClient
    {
        public string Id { get; set; }
        public int Port { get; set; }

        public IPAddress IPAddress { get; set; }

        public AppStateEnum State { get; set; }

        /// <summary>
        /// 客户端组
        /// </summary>
        public IClairvoyanceGroup Group { get; set; }
    }
}
