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
namespace Air.Cloud.Core.Standard.MessageQueue.Attributes
{
    /// <summary>
    /// <para>zh-cn:应用程序队列信息描述</para>
    /// <para>en-us:Application queue information descriptor</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class AppQueueDescriptorAttribute:Attribute
    {
        /// <summary>
        /// <para>zh-cn:订阅队列</para>
        /// <para>en-us:Subscribe queue name</para>
        /// </summary>
        public string SubscribeQueue { get; set; }

        /// <summary>
        /// <para>zh-cn:回复队列</para>
        /// <para>en-us:Recive queue name</para>
        /// </summary>
        public string ReciveQueue { get; set; } = null;

        /// <summary>
        /// <para>zh-cn:组编号</para>
        /// <para>en-us:Group id</para>
        /// </summary>
        public string GroupId { get; set; }
        /// <summary>
        /// <para>zh-cn:构造函数</para>
        /// <para>en-us:Constractor</para>
        /// </summary>
        /// <param name="SubscribeQueue">
        /// <para>zh-cn:订阅队列</para>
        /// <para>en-us:Subscribe queue name</para>
        /// </param>
        /// <param name="ReciveQueue">
        /// <para>zh-cn:回复队列(可为空)</para>
        /// <para>en-us:Recive queue name(can be null)</para>
        /// </param>
        /// <param name="GroupId">
        /// <para>zh-cn:组编号</para>
        /// <para>en-us:GroupId</para>
        /// </param>
        public AppQueueDescriptorAttribute(string SubscribeQueue, string ReciveQueue=null,string GroupId=null)
        {
            this.SubscribeQueue = SubscribeQueue;
            this.ReciveQueue = ReciveQueue;
            this.GroupId = GroupId ?? (AppEnvironment.IsDevelopment ? Guid.NewGuid().ToString() : AppConst.ApplicationName);
        }
    }
}
