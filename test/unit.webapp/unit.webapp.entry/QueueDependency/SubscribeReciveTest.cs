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

namespace unit.webapp.entry.QueueDependency
{
    [AppQueueDescriptor("fcj_workflow_callback","testGroup")]
    public class WorkflowAuditSubscribeReciveDependency : IMessageQueueSubscribeStandard<WorkflowPublishNews>
    {
        public object Subscribe(WorkflowPublishNews message)
        {
            AppRealization.Output.Print("接收到消息的订阅回执", AppRealization.JSON.Serialize(message));
            return message;
        }
    }
}
