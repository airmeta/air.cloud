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
namespace air.cloud.system.open.service.Dtos.OpenOrgDtos.Update
{
    /// <summary>
    /// <para>zh-cn:开放组织更新结果</para>
    /// <para>en-us:Open Organization Update Result</para>
    /// </summary>
    public class OpenOrgUpdateResult
    {
        /// <summary>
        /// <para>zh-cn:是否更新成功</para>
        /// <para>en-us:Is Update Successful</para>
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// <para>zh-cn:开放组织标识</para>
        /// <para>en-us:Open Organization Identifier</para>
        /// </summary>
        public string Id { get; set; }
    }
}
