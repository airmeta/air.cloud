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
namespace Air.Cloud.Core.Modules.DynamicApp.Model
{
    /// <summary>
    /// <para>zh-cn:模组设置</para>
    /// <para>en-us: Mod Settings</para>
    /// </summary>
    public class ModSettings
    {
        /// <summary>
        /// 是否启用定时任务
        /// </summary>
        public bool EnableJob { get; set; }
        /// <summary>
        /// 是否启用GRPC服务
        /// </summary>
        public bool EnableGRPC { get; set; }
        /// <summary>
        /// 是否启用接口服务
        /// </summary>
        public bool EnableService { get; set; }
        /// <summary>
        /// 是否启用模组的配置文件加载
        /// </summary>
        public bool EnableModSettingLoad { get; set; }
    }
}
