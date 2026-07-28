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
namespace air.cloud.system.model.Dtos.UserDtos
{
    public  class UserGroupSDto
    {
        public string Id { get; set; }
        /// <summary>
        /// 用户组名称
        /// </summary>
        public string GroupName { get; set; }


        /// <summary>
        /// 用户组描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 应用程序信息
        /// </summary>
        public string AppId { get; set; }

    }
}
