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
using Air.Cloud.Core.App;

namespace air.cloud.security.common
{
    public class RsaKeyConst
    {
        /// <summary>
        /// 私钥
        /// </summary>
        public static string PRIVATE_KEY => AppCore.Configuration["AppSettings:PrivateKey"];
        /// <summary>
        /// 公钥
        /// </summary>
        public static string PUBLIC_KEY => AppCore.Configuration["AppSettings:PublicKey"];
    }
}
