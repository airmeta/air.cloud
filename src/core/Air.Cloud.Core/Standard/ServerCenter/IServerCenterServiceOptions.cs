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

namespace Air.Cloud.Core.Standard.ServerCenter
{
    /// <summary>
    /// 服务中心的服务详细信息接口
    /// 用来约束服务中心标准接口的传入参数
    /// </summary>
    public interface IServerCenterServiceOptions
    {
        /// <summary>
        /// 服务地址 
        /// </summary>
        public string ServiceAddress { get; set; }
        /// <summary>
        /// 服务名称
        /// </summary>
        public string ServiceName { get; set; }
        /// <summary>
        /// 服务唯一编号
        /// </summary>
        public string ServiceKey { get; set; }
        /// <summary>
        /// 服务参数
        /// </summary>
        public string[] ServiceValues { get; set; }
    }
}
