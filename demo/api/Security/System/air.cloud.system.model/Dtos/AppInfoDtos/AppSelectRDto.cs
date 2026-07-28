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
using air.cloud.security.common.Enums;

namespace air.cloud.system.model.Dtos.AppInfoDtos
{
    /// <summary>
    /// 应用下拉选择返回Dto
    /// </summary>
    public class AppSelectRDto
    {
        /// <summary>
        /// 应用Id
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// 应用名称
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// 应用描述
        /// </summary>  
        public string Description { get; set; }

        /// <summary>
        /// <para>zh-cn:是否启用</para>
        /// <para>en-us:Whether to enable</para>
        /// </summary>
        public IsOrNotEnum IsEnable { get; set; }
    }
}
