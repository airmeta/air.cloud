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
namespace Air.Cloud.Core.Enums
{
    /// <summary>
    /// 运行环境枚举  自定义配置则使用Other 会自动读取对应后缀名的配置文件
    /// </summary>
    public enum EnvironmentEnums
    {
        Development,
        Production,
        Test,
        Other
    }
}
