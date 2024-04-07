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
namespace Air.Cloud.Core.Provider
{
    /// <summary>
    /// 返回值约定
    /// </summary>
    public interface IResultProvider
    {
        /// <summary>
        /// 成功状态
        /// </summary>
        public bool Success { get; set; }
        /// <summary>
        /// 消息内容
        /// </summary>
        public string Message { get; set; }
        /// <summary>
        /// 错误消息
        /// </summary>
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// 系统默认返回值
    /// </summary>
    public class AppResult : IResultProvider
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string ErrorMessage { get; set; }
    }
}
