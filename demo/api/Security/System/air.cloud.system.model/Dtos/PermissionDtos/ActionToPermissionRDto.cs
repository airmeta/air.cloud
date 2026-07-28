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
namespace air.cloud.system.model.Dtos.PermissionDtos
{
    public  class ActionToPermissionRDto
    {
        /// <summary>
        /// 应用程序编号
        /// </summary>
        public string AppId { get; set; }

        /// <summary>
        /// 同步成功数量
        /// </summary>
        public int AsyncSuccessCount { get; set; }

        /// <summary>
        /// 同步失败数量
        /// </summary>
        public int AsyncFailureCount { get; set; }

        /// <summary>
        /// 是否同步成功
        /// </summary>
        /// <returns></returns>
        public bool IsAllSuccess()
        {
            return AsyncFailureCount == 0;
        }

    }
}
