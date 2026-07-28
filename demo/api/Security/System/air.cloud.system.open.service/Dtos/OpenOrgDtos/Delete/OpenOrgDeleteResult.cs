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
namespace air.cloud.system.open.service.Dtos.OpenOrgDtos.Delete
{
    /// <summary>
    /// <para>zh-cn:开放组织删除结果</para>
    /// <para>en-us:Open Organization Delete Result</para>
    /// </summary>
    public class OpenOrgDeleteResult
    {
        /// <summary>
        /// <para>zh-cn:是否删除成功</para>
        /// <para>en-us:Whether Deletion was Successful</para>
        /// </summary>
        public bool IsDelete { get; set; }
        /// <summary>
        /// <para>zh-cn:被删除的开放组织ID</para>
        /// <para>en-us:ID of the Deleted Open Organization</para>
        /// </summary>
        public string Id { get; set; }

    }
}
