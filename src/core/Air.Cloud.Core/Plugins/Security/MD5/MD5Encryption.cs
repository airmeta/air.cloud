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
using System.Text;

namespace Air.Cloud.Core.Plugins.Security.MD5
{
    /// <summary>
    /// MD5各种长度加密字符、验证MD5等操作辅助类
    /// </summary>
    public class MD5Encryption
    {
        /// <summary>
        /// 获得32位的MD5加密
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string GetMd5By32(string input, bool upper = false)
        {
            var md5 = System.Security.Cryptography.MD5.Create();
            var data = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
            var sb = new StringBuilder();
            foreach (var t in data)
            {
                sb.Append(t.ToString("x2"));
            }
            return !upper ? sb.ToString() : sb.ToString().ToUpper();
        }

        /// <summary>
        /// 获得16位的MD5加密
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string GetMd5By16(string input, bool upper = false)
        {
            return GetMd5By32(input, upper).Substring(8, 16);
        }

        /// <summary>
        /// 获得8位的MD5加密
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string GetMd5By8(string input, bool upper = false)
        {
            return GetMd5By32(input, upper).Substring(8, 8);
        }

        /// <summary>
        /// 获得4位的MD5加密
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string GetMd5By4(string input, bool upper = false)
        {
            return GetMd5By32(input, upper).Substring(8, 4);
        }
    }
}
