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
using Air.Cloud.Core.Standard;

namespace Air.Cloud.Core.Plugins.Http.Provider
{
    [IgnoreScanning]
    public class RestfulResult<T> : IRESTfulResultStandard<T> where T : class, new()
    {
        public RestfulResult()
        {
            Code = 200;
            Succeeded = true;
        }
        /// <summary>状态码</summary>
        public int? Code { get; set; }

        /// <summary>错误信息</summary>
        public object Msg { get; set; }
        public T Data { get; set; }
        public bool Succeeded { get; set; }
        public object Errors { get; set; }
        public object Extras { get; set; }
        public long Timestamp { get; set; }
    }
}
