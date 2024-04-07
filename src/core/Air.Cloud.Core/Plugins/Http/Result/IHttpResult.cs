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
namespace Air.Cloud.Core.Plugins.Http.Result
{
    /// <summary>
    /// 请求结果接口
    /// </summary>
    public interface IHttpResult<T> : IHttpResultBase
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool IsSuccess { get { return string.IsNullOrEmpty(Exception); } }

        /// <summary>
        /// 异常信息
        /// </summary>
        public string Exception { get; set; }

        /// <summary>
        /// 结果数据
        /// </summary>
        public T Result { get; set; }
    }
}
