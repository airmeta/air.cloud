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
namespace Air.Cloud.Core.Standard
{
    public interface IRESTfulResultStandard<T> : IStandard where T : class, new()
    {
        /// <summary>
        /// 状态码
        /// </summary>
        public int? Code { get; set; }

        /// <summary>
        /// 数据
        /// </summary>
        public T Data { get; set; }

        /// <summary>
        /// 执行成功
        /// </summary>
        public bool Succeeded { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public object Errors { get; set; }

        /// <summary>
        /// 附加数据
        /// </summary>
        public object Extras { get; set; }

        /// <summary>
        /// 时间戳
        /// </summary>
        public long Timestamp { get; set; }

    }

    public interface IRESTfulResultStandard: IStandard
    {
        /// <summary>
        /// 状态码
        /// </summary>
        public int? Code { get; set; }

        /// <summary>
        /// 执行成功
        /// </summary>
        public bool Succeeded { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public object Errors { get; set; }

        /// <summary>
        /// 附加数据
        /// </summary>
        public object Extras { get; set; }

        /// <summary>
        /// 时间戳
        /// </summary>
        public long Timestamp { get; set; }

    }
}
