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
namespace Air.Cloud.Core.Plugins.Sensitive
{
    /// <summary>
    /// 人员敏感信息脱敏
    /// </summary>
    public static class PeopleSensitivePurge
    {
        /// <summary>
        /// 姓名敏感处理
        /// </summary>
        /// <param name="fullName">姓名</param>
        /// <returns>脱敏后的姓名</returns>
        public static string NamePurge(this string fullName)
        {
            if (string.IsNullOrEmpty(fullName)) return string.Empty;

            string familyName = fullName[..1];
            string end = fullName.Substring(fullName.Length - 1, 1);
            string name = string.Empty;
            //长度为2
            if (fullName.Length <= 2) name = familyName + "*";
            //长度大于2
            else if (fullName.Length >= 3)
            {
                name = familyName.PadRight(fullName.Length - 1, '*') + end;
            }
            return name;
        }

        /// <summary>
        /// 身份证脱敏
        /// </summary>
        /// <param name="idCardNo">身份证号</param>
        /// <returns>脱敏后的身份证号</returns>
        public static string CardNoPurge(this string idCardNo)
        {
            if (string.IsNullOrEmpty(idCardNo)
                || idCardNo.Length != 15 && idCardNo.Length != 18) return idCardNo;

            string begin = idCardNo[..6];
            string middle = idCardNo.Substring(6, 8);
            string end = idCardNo[14..];

            string card = string.Empty;
            card = begin + "********" + end;
            return card;
        }
    }
}
