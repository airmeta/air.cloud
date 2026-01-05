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
namespace air.gateway.Options
{
    public  class ProxySetting
    {
        /// <summary>
        /// 网关地址
        /// </summary>
        public string GateWayAddress { get; set; }
        /// <summary>
        /// 路由信息
        /// </summary>
        public List<ProxyRoute> Routes { get; set; }
    }

    public  class ProxyRoute
    {
        public string GateWayAddress { get; set; }
        /// <summary>
        /// 代理地址
        /// </summary>
        public string ProxyPath { get; set; }
        /// <summary>
        /// 目标地址
        /// </summary>
        public string TargetPath { get; set; }
    }

}
