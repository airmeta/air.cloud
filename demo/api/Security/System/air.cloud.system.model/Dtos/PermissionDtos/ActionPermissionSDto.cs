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
    public class ActionPermissionSDto
    {

        public string Id { get; set; }
        /// <summary>
        /// 权限标识
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// <para>权限描述</para>
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 服务名
        /// </summary>
        public string ServiceName { get; set; }
        /// <summary>
        /// 应用程序信息
        /// </summary>
        public string AppId
        {
            get; set;
        }
    }
}
