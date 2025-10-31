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
using Air.Cloud.Core.Standard.DynamicServer;

namespace Air.Cloud.Core.Standard.JSON
{
    /// <summary>
    /// JSON 序列化器标准接口
    /// </summary>
    public  interface IJsonSerializerStandard: IStandard,ISingleton
    {
        /// <summary>
        /// 序列化对象
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        string Serialize(object value);

        /// <summary>
        /// 反序列化字符串
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="json"></param>
        /// <returns></returns>
        T Deserialize<T>(string json);
    }
}
