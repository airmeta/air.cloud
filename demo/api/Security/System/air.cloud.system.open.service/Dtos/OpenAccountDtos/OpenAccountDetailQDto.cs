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
namespace air.cloud.system.open.service.Dtos.OpenAccountDtos
{
    /// <summary>
    /// <para>zh-cn:开放账户详情查询请求参数DTO</para>
    /// <para>en-us:Open Account Detail Query Request Parameter DTO</para>
    /// </summary>
    public class OpenAccountDetailQDto
    {
        /// <summary>
        /// <para>zh-cn:待核票据(使用应用私钥加密)</para>
        /// <para>en-us:Ticket to be verified(使用应用私钥加密)</para>
        /// </summary>
        public string Ticket { get; set; }

    }
}
