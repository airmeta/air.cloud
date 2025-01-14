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
using System.Security.Cryptography;
using System.Text;

namespace Air.Cloud.Core.Plugins.Security.HmacSHA256
{
    public static class HmacSHA256Helper
    {

        /// <summary>
        /// 加密算法HmacSHA256
        /// </summary>
        /// <param name="secret"></param>
        /// <param name="signKey"></param>
        /// <returns></returns>
        public static string HmacSHA256Encrypt(string secret, string signKey)
        {
            string signRet = string.Empty;
            using (HMACSHA256 mac = new HMACSHA256(Encoding.UTF8.GetBytes(signKey)))
            {
                byte[] hash = mac.ComputeHash(Encoding.UTF8.GetBytes(secret));
                signRet = Convert.ToBase64String(hash);
            }
            return signRet;
        }
    }
}
