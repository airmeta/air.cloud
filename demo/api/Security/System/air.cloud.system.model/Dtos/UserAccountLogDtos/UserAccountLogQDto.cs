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
using air.cloud.security.common.Base.Dtos;

using System;

namespace air.cloud.system.model.Dtos.UserAccountLogDtos
{
    /// <summary>
    /// <para>zh-cn:用户账户日志保存传输对象</para>
    /// <para>en-us:User account log save DTO</para>
    /// </summary>
    public class UserAccountLogQDto :BaseQDto
    {
        /// <summary>
        /// <para>zh-cn:用户编号</para>
        /// <para>en-us:User Id</para>
        /// </summary>
        public string UserId { get; set; }

    }
}