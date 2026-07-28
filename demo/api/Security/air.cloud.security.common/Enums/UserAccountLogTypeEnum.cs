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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace air.cloud.security.common.Enums
{
    /// <summary>
    /// <para>zh-cn:用户账户日志类型枚举</para>
    /// <para>en-us:User Account Log Type Enum</para>
    /// </summary>
    public enum UserAccountLogTypeEnum
    {
        用户登录=0,
        用户登出=1,
        密码修改=2,
        账户锁定=3,
        账户解锁= 4,
        密码重置= 5,
        登入应用 = 6
    }
}
