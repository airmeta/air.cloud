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
namespace Air.Cloud.Core.Standard.CICD.Model
{
    /// <summary>
    /// 程序包信息
    /// </summary>
    public interface IPackageInfo
    {
        /// <summary>
        /// 程序包名称
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 版本号
        /// </summary>
        public string Version { get; set; }
        /// <summary>
        /// 描述
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// 程序包存储地址
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// 获取程序包信息
        /// </summary>
        /// <returns></returns>
        public Stream GetPackage();
    }
}
