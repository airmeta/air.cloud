
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

namespace Air.Cloud.Core.Plugins.InternalAccess
{
    /// <summary>
    /// 内部访问插件接口
    /// </summary>
    public interface IInternalAccessValidPlugin:IPlugin,ISingleton
    {
        /// <summary>
        /// 创建内部访问Token令牌
        /// </summary>
        /// <returns></returns>
        public Tuple<string,string> CreateInternalAccessToken();

        /// <summary>
        /// 验证访问令牌是否有效
        /// </summary>
        /// <param name="Headers"></param>
        /// <returns></returns>
        public bool ValidInternalAccessToken(IDictionary<string,string> Headers);

    }
}
