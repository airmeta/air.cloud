﻿/*
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
using System.Net;

namespace Air.Cloud.Core.Standard.MessageQueue.Model
{
    /// <summary>
    /// 消息队列节点信息
    /// </summary>
    public interface IMessageQueueBroker
    {
        public string BrokerName { get; set; }

        public string BootStrapServer { get; set; }

        public IPAddress IPAddress { get; set; }
    }
}
