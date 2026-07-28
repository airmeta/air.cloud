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
namespace air.cloud.system.model.Dtos.AccountDtos
{
    public  class AccountAppIdsRDto
    {
        /// <summary>
        /// 应用ID
        /// </summary>
        public string AppId { get; set; }

        /// <summary>
        /// 应用名称
        /// </summary>
        public string AppName { get; set; }

        /// <summary>
        /// 应用重定向地址
        /// </summary>
        public string AppRedirectUrl { get; set; }

        /// <summary>
        /// 应用重定向Key
        /// </summary>
        public string AppRedirectKey { get; set; }


        /// <summary>
        /// 图片地址
        /// </summary>
        public string LogoUrl { get; set; }

        /// <summary>
        /// 应用描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime? CreateTime { get; set; }

    }
}
