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
using Air.Cloud.Core.Plugins;
using Air.Cloud.Core.Util;

namespace Air.Cloud.Core.Plugins.Map
{
    /// <summary>
    /// 地图插件
    /// </summary>
    public class MapPlugin : IPlugin
    {
        /// <summary>
        /// 地球半径
        /// </summary>
        private const double EarthRadius = 6378.137;

        private static double xPi = 3.14159265358979324 * 3000.0 / 180.0;
        //private static readonly int MULTIPLE_LEVEL = 1000000;

        /// <summary>
        /// 计算两个坐标点之间的距离
        /// </summary>
        /// <param name="firstLatitude">第一个坐标的纬度</param>
        /// <param name="firstLongitude">第一个坐标的经度</param>
        /// <param name="secondLatitude">第二个坐标的纬度</param>
        /// <param name="secondLongitude">第二个坐标的经度</param>
        /// <returns>返回两点之间的距离，单位：公里/千米</returns>
        public static double GetDistance(double firstLatitude, double firstLongitude, double secondLatitude, double secondLongitude)
        {
            var firstRadLat = Rad(firstLatitude);
            var firstRadLng = Rad(firstLongitude);
            var secondRadLat = Rad(secondLatitude);
            var secondRadLng = Rad(secondLongitude);

            var a = firstRadLat - secondRadLat;
            var b = firstRadLng - secondRadLng;
            var cal = 2 * Math.Asin(Math.Sqrt(Math.Pow(Math.Sin(a / 2), 2) + Math.Cos(firstRadLat)
                * Math.Cos(secondRadLat) * Math.Pow(Math.Sin(b / 2), 2))) * EarthRadius;
            var result = Math.Round(cal * 10000) / 10000;
            return result;
        }

        /// <summary>
        /// 计算两个坐标点之间的距离
        /// </summary>
        /// <param name="firstPoint">第一个坐标点的（纬度,经度）</param>
        /// <param name="secondPoint">第二个坐标点的（纬度,经度）</param>
        /// <returns>返回两点之间的距离，单位：公里/千米</returns>
        public static double GetPointDistance(string firstPoint, string secondPoint)
        {
            var firstArray = firstPoint.Split(',');
            var secondArray = secondPoint.Split(',');
            var firstLatitude = Convert.ToDouble(firstArray[0].Trim());
            var firstLongitude = Convert.ToDouble(firstArray[1].Trim());
            var secondLatitude = Convert.ToDouble(secondArray[0].Trim());
            var secondLongitude = Convert.ToDouble(secondArray[1].Trim());
            return GetDistance(firstLatitude, firstLongitude, secondLatitude, secondLongitude);
        }

        /// <summary>
        /// 经纬度转化成弧度
        /// </summary>
        /// <param name="d"></param>
        /// <returns></returns>
        private static double Rad(double d)
        {
            return d * Math.PI / 180d;
        }

        /// <summary>
        ///  计算文本相似度函数(适用于短文本)
        /// </summary>
        /// <param name="textX">文本</param>
        /// <param name="textY">文本</param>
        /// <param name="isCase">是否忽略大小写</param>
        /// <returns></returns>
        public static double ComputeTextSame(string textX, string textY, bool isCase = false)
        {
            if (textX.Length <= 0 || textY.Length <= 0)
            {
                return 0;
            }
            if (!isCase)
            {
                textX = textX.ToLower();
                textY = textY.ToLower();
            }
            int[,] dp = new int[Math.Max(textX.Length, textY.Length) + 1, Math.Max(textX.Length, textY.Length) + 1];
            for (int x = 0; x < textX.Length; x++)
            {
                for (int y = 0; y < textY.Length; y++)
                {
                    if (textX[x] == textY[y])
                    {
                        dp[x + 1, y + 1] = dp[x, y] + 1;
                    }
                    else
                    {
                        dp[x + 1, y + 1] = Math.Max(dp[x, y + 1], dp[x + 1, y]);
                    }
                }
            }
            return Math.Round((double)dp[textX.Length, textY.Length] / Math.Max(textX.Length, textY.Length) * 100, 2);
        }

        /**
         * 将火星坐标转变成百度坐标
         *
         * @param marsCoordinate 火星坐标（高德、腾讯地图坐标等）
         * @return 百度坐标
         */

        public static CoordinateDto MarsToBaidu(CoordinateDto marsCoordinate)
        {
            double x = marsCoordinate.getLongitude;
            double y = marsCoordinate.getLatitude;
            double z = Math.Sqrt(x * x + y * y) + 0.00002 * Math.Sin(y * xPi);
            double theta = Math.Atan2(y, x) + 0.000003 * Math.Cos(x * xPi);
            return new CoordinateDto(dataDigit(6, z * Math.Cos(theta) + 0.0065),
                dataDigit(6, z * Math.Sin(theta) + 0.006));
        }

        /**
         * 将百度坐标转变成火星坐标
         *
         * @param baiduCoordinate 百度坐标（百度地图坐标）
         * @return 火星坐标(高德、腾讯地图等)
         */

        public static CoordinateDto BaiduToMars(CoordinateDto baiduCoordinate)
        {
            double x = baiduCoordinate.getLongitude - 0.0065;
            double y = baiduCoordinate.getLatitude - 0.006;
            double z = Math.Sqrt(x * x + y * y) - 0.00002 * Math.Sin(y * xPi);
            double theta = Math.Atan2(y, x) - 0.000003 * Math.Cos(x * xPi);
            return new CoordinateDto(dataDigit(6, z * Math.Cos(theta)), dataDigit(6, z * Math.Sin(theta)));
        }

        public static CoordinateDto BaiduToMars(double longitude, double latitude)
        {
            CoordinateDto baiduCoordinate = new CoordinateDto(longitude, latitude);
            return BaiduToMars(baiduCoordinate);
        }
        /**
        * 对double类型数据保留小数点后多少位
        *
        * @param digit 位数
        * @param input 输入
        * @return 保留小数位后的数
        */

        internal static double dataDigit(int digit, double input)
        {
            return Math.Round(input, digit, MidpointRounding.AwayFromZero);
        }
    }
}
