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

using static System.TimeZoneInfo;

namespace Air.Cloud.Core.Extensions
{
    /// <summary>
    /// 类型转换扩展
    /// </summary>
    public static partial class Extensions
    {
        #region 数值转换
        /// <summary>
        /// 转换为整型
        /// </summary>
        /// <param name="data">数据</param>
        public static int ToInt(this object data)
        {
            if (data == null)
                return 0;
            var success = int.TryParse(data.ToString(), out var result);
            if (success)
                return result;
            try
            {
                return Convert.ToInt32(ToDouble(data, 0));
            }
            catch (Exception)
            {
                return 0;
            }
        }

        /// <summary>
        /// 转换为可空整型
        /// </summary>
        /// <param name="data">数据</param>
        public static int? ToIntOrNull(this object data)
        {
            if (data == null)
                return null;
            bool isValid = int.TryParse(data.ToString(), out var result);
            if (isValid)
                return result;
            return null;
        }

        /// <summary>
        /// 转换为双精度浮点数
        /// </summary>
        /// <param name="data">数据</param>
        public static double ToDouble(this object data)
        {
            if (data == null)
                return 0;
            return double.TryParse(data.ToString(), out var result) ? result : 0;
        }

        /// <summary>
        /// 转换为long
        /// </summary>
        /// <param name="data">数据</param>
        public static long ToLong(this object data)
        {
            if (data == null)
                return 0;
            return long.TryParse(data.ToString(), out var result) ? result : 0;
        }

        /// <summary>
        /// 转换为双精度浮点数,并按指定的小数位4舍5入
        /// </summary>
        /// <param name="data">数据</param>
        /// <param name="digits">小数位数</param>
        public static double ToDouble(this object data, int digits)
        {
            return Math.Round(ToDouble(data), digits);
        }

        /// <summary>
        /// 转换为可空双精度浮点数
        /// </summary>
        /// <param name="data">数据</param>
        public static double? ToDoubleOrNull(this object data)
        {
            if (data == null)
                return null;
            bool isValid = double.TryParse(data.ToString(), out var result);
            if (isValid)
                return result;
            return null;
        }

        /// <summary>
        /// 转换为高精度浮点数
        /// </summary>
        /// <param name="data">数据</param>
        public static decimal ToDecimal(this object data)
        {
            if (data == null)
                return 0;
            return decimal.TryParse(data.ToString(), out var result) ? result : 0;
        }

        /// <summary>
        /// 转换为高精度浮点数,并按指定的小数位4舍5入
        /// </summary>
        /// <param name="data">数据</param>
        /// <param name="digits">小数位数</param>
        public static decimal ToDecimal(this  object data, int digits)
        {
            return Math.Round(ToDecimal(data), digits);
        }

        /// <summary>
        /// 转换为可空高精度浮点数
        /// </summary>
        /// <param name="data">数据</param>
        public static decimal? ToDecimalOrNull(this  object data)
        {
            if (data == null)
                return null;
            bool isValid = decimal.TryParse(data.ToString(), out var result);
            if (isValid)
                return result;
            return null;
        }

        /// <summary>
        /// 转换为可空高精度浮点数,并按指定的小数位4舍5入
        /// </summary>
        /// <param name="data">数据</param>
        /// <param name="digits">小数位数</param>
        public static decimal? ToDecimalOrNull(this object data, int digits)
        {
            var result = ToDecimalOrNull(data);
            if (result == null)
                return null;
            return Math.Round(result.Value, digits);
        }

        #endregion

        #region 日期转换
        /// <summary>
        /// 转换为日期
        /// </summary>
        /// <param name="data">数据</param>
        public static DateTime ToDate(this object data)
        {
            if (data == null)
                return DateTime.MinValue;
            return DateTime.TryParse(data.ToString(), out var result) ? result : DateTime.MinValue;
        }

        /// <summary>
        /// 转换为可空日期
        /// </summary>
        /// <param name="data">数据</param>
        public static DateTime? ToDateOrNull(this object data)
        {
            if (data == null)
                return null;
            bool isValid = DateTime.TryParse(data.ToString(), out var result);
            if (isValid)
                return result;
            return null;
        }

        // 时间戳转为格式
        public static DateTime StampToDateTime(string timeStamp)
        {
            DateTime dateTimeStart = DateTimeOffset.FromUnixTimeSeconds(0).LocalDateTime;
            long lTime = long.Parse(timeStamp + "0000000");
            TimeSpan toNow = TimeSpan.FromSeconds(lTime);
            return dateTimeStart.Add(toNow);
        }

        #endregion

        #region 布尔转换
        /// <summary>
        /// 转换为布尔值
        /// </summary>
        /// <param name="data">数据</param>
        public static bool ToBool(this object data)
        {
            if (data == null)
                return false;
            bool? value = GetBool(data);
            if (value != null)
                return value.Value;
            return bool.TryParse(data.ToString(), out var result) && result;
        }

        /// <summary>
        /// 获取布尔值
        /// </summary>
        private static bool? GetBool(this object data)
        {
            switch (data.ToString()?.Trim().ToLower())
            {
                case "0":
                    return false;
                case "1":
                    return true;
                case "是":
                    return true;
                case "否":
                    return false;
                case "yes":
                    return true;
                case "no":
                    return false;
                default:
                    return null;
            }
        }

        /// <summary>
        /// 转换为可空布尔值
        /// </summary>
        /// <param name="data">数据</param>
        public static bool? ToBoolOrNull(this object data)
        {
            if (data == null)
                return null;
            bool? value = GetBool(data);
            if (value != null)
                return value.Value;
            bool isValid = bool.TryParse(data.ToString(), out var result);
            if (isValid)
                return result;
            return null;
        }

        #endregion

        #region 字符串转换
        /// <summary>
        /// 转换为字符串
        /// </summary>
        /// <param name="data">数据</param>
        public static string ToString(this object data)
        {
            return data == null ? string.Empty : data.ToString()?.Trim();
        }
        #endregion

        /// <summary>
        /// 获取字符串的实际字节数
        /// </summary>
        /// <param name="value">要获取长度的字符串</param>
        /// <returns></returns>
        public static long GetByteLength(this string value)
        {
            if (value.Equals(string.Empty))
            { return 0; }
            int stolen = 0;
            ASCIIEncoding strData = new ASCIIEncoding();
            //将字符串转换为ASCII编码的字节数字
            byte[] strBytes = strData.GetBytes(value);
            for (int i = 0; i <= strBytes.Length - 1; i++)
            {
                if (strBytes[i] == 63)  //中文都将编码为ASCII编码63,即"?"号
                { stolen++; }
                stolen++;
            }
            return stolen;
        }


        /// <summary>
        /// 转化为半角字符串（扩展方法）
        /// </summary>
        /// <param name="input">要转化的字符串</param>
        /// <returns>半角字符串</returns>
        public static string ToSbc(this string input)//single byte charactor
        {
            char[] c = input.ToCharArray();
            for (int i = 0; i < c.Length; i++)
            {
                if (c[i] == 12288)//全角空格为12288，半角空格为32
                {
                    c[i] = (char)32;
                    continue;
                }
                if (c[i] > 65280 && c[i] < 65375)//其他字符半角(33-126)与全角(65281-65374)的对应关系是：均相差65248
                    c[i] = (char)(c[i] - 65248);
            }
            return new string(c);
        }

        /// <summary>
        /// 转化为全角字符串（扩展方法）
        /// </summary>
        /// <param name="input">要转化的字符串</param>
        /// <returns>全角字符串</returns>
        public static string ToDbc(this string input)//double byte charactor 
        {
            char[] c = input.ToCharArray();
            for (int i = 0; i < c.Length; i++)
            {
                if (c[i] == 32)
                {
                    c[i] = (char)12288;
                    continue;
                }
                if (c[i] < 127)
                    c[i] = (char)(c[i] + 65248);
            }
            return new string(c);
        }
    }
}
