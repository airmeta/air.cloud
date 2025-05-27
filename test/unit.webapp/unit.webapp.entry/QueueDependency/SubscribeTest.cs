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
using Air.Cloud.Core;
using Air.Cloud.Core.Standard.MessageQueue;
using Air.Cloud.Core.Standard.MessageQueue.Attributes;

using System.ComponentModel;

namespace unit.webapp.entry.QueueDependency
{
    [AppQueueDescriptor("fcj_workflow_audit", "fcj_workflow_callback","testGroup")]
    public class WorkflowAuditSubscribeDependency : IMessageQueueSubscribeStandard<WorkflowPublishNews>
    {
        public object Subscribe(WorkflowPublishNews message)
        {
            AppRealization.Output.Print("订阅到消息", AppRealization.JSON.Serialize(message));
            return message;
        }
    }
    /// <summary>
    /// 消息处理状态
    /// </summary>
    public enum WorkflowPublishNewsState
    {
        未发出 = 0,
        已发出,
        处理时异常,
        已处理
    }
    /// <summary>
    /// 业务回调消息表
    /// </summary>
    public class WorkflowPublishNews: IMessageContentStandard
    {
        public string Id { get; set; }
        /// <summary>
        /// 回调数据
        /// </summary>
        /// <remarks>
        /// CONTENT N   CLOB Y           回调数据
        /// </remarks>
        [Description("回调数据")]
        public string Content { get; set; }

        /// <summary>
        /// 处理状态
        /// </summary>
        /// <remarks>
        /// STATE   N NUMBER  Y 处理状态  未发出=0, 已发出,处理时异常,已处理
        /// </remarks>
        [Description("处理状态")]
        public WorkflowPublishNewsState State { get; set; }

        /// <summary>
        /// 业务编号
        /// </summary>
        /// <remarks>
        ///  BUSINESS_NO N VARCHAR2(50)    Y 业务编号
        /// </remarks>
        [Description("业务编号")]
        public string BusinessNo { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        /// <remarks>
        ///  CREATE_TIME N   NUMBER Y           序号
        /// </remarks>
        [Description("创建时间")]
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 处理时间
        /// </summary>
        /// <remarks>
        ///  RECIVE_TIME N   NUMBER Y           序号
        /// </remarks>
        [Description("处理时间")]
        public DateTime ReciveTime { get; set; }
        /// <summary>
        /// 重试次数
        /// </summary>
        /// <remarks>
        /// REDO    N NUMBER  Y 重试次数
        /// </remarks>
        [Description("重试次数")]
        public int RedoCount { get; set; }

        /// <summary>
        /// 异常提示
        /// </summary>
        [Description("异常提示")]
        public string ErrorMessage { get; set; }
        /// <summary>
        /// 异常栈
        /// </summary>
        [Description("异常栈")]
        public string Stack { get; set; }
    }
}
