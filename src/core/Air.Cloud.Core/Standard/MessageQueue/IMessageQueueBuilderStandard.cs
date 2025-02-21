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
    /// <para>zh-cn:消息队列工厂</para>
    /// <para>en-us:Message queue builder</para>
    /// </summary>
    public interface IMessageQueueBuilderStandard
    {
        public static string[] GroupFormatter = new string[]{
            "{GroupId}_{VerisonDistinguish}_{EnvironmentDistinguish}",
            "{GroupId}_{EnvironmentDistinguish}",
            "{GroupId}_{VerisonDistinguish}",
        };
        /// <summary>
        /// 
        /// </summary>
        /// <param name="GroupId">
        ///  <para>zh-cn:默认会以当前程序入口的类库名作为GroupId</para>
        /// </param>
        /// <param name="EnvironmentDistinguish">
        ///  
        /// </param>
        /// <param name="VerisonDistinguish"></param>
        /// <returns></returns>
        public string BuildGroupId(
            string GroupId=null,
            bool EnvironmentDistinguish=true,
            bool VerisonDistinguish = false);

    }

    //定义一个IMessageQueueBuilderStandard的实现类,实现BuildGroupId方法
    public class MessageQueueBuilderStandard : IMessageQueueBuilderStandard
    {
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
