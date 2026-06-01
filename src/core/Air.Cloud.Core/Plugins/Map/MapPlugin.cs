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
namespace Air.Cloud.Core.Plugins.Map
{
    /// <summary>
    /// <para>zh-cn:提供地图相关工具方法，包括坐标距离计算、短文本相似度计算以及百度坐标和火星坐标转换。</para>
    /// <para>en-us:Provides map-related utility methods, including coordinate distance calculation, short-text similarity calculation, and conversion between Baidu coordinates and Mars coordinates.</para>
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
        /// <para>zh-cn:计算两个经纬度坐标点之间的球面距离。</para>
        /// <para>en-us:Calculates the spherical distance between two latitude/longitude coordinate points.</para>
        /// </summary>
        /// <param name="firstLatitude"><para>zh-cn:第一个坐标点的纬度。</para><para>en-us:The latitude of the first coordinate point.</para></param>
        /// <param name="firstLongitude"><para>zh-cn:第一个坐标点的经度。</para><para>en-us:The longitude of the first coordinate point.</para></param>
        /// <param name="secondLatitude"><para>zh-cn:第二个坐标点的纬度。</para><para>en-us:The latitude of the second coordinate point.</para></param>
        /// <param name="secondLongitude"><para>zh-cn:第二个坐标点的经度。</para><para>en-us:The longitude of the second coordinate point.</para></param>
        /// <returns><para>zh-cn:两个坐标点之间的距离，单位为公里。</para><para>en-us:The distance between the two coordinate points, in kilometers.</para></returns>
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
        /// <para>zh-cn:根据字符串格式坐标计算两个坐标点之间的距离。</para>
        /// <para>en-us:Calculates the distance between two coordinate points from string-formatted coordinates.</para>
        /// </summary>
        /// <param name="firstPoint"><para>zh-cn:第一个坐标点，格式为“纬度,经度”。</para><para>en-us:The first coordinate point in the format "latitude,longitude".</para></param>
        /// <param name="secondPoint"><para>zh-cn:第二个坐标点，格式为“纬度,经度”。</para><para>en-us:The second coordinate point in the format "latitude,longitude".</para></param>
        /// <returns><para>zh-cn:两个坐标点之间的距离，单位为公里。</para><para>en-us:The distance between the two coordinate points, in kilometers.</para></returns>
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
        /// <para>zh-cn:计算两个短文本之间的相似度百分比。</para>
        /// <para>en-us:Calculates the similarity percentage between two short text values.</para>
        /// </summary>
        /// <param name="textX"><para>zh-cn:第一个文本。</para><para>en-us:The first text.</para></param>
        /// <param name="textY"><para>zh-cn:第二个文本。</para><para>en-us:The second text.</para></param>
        /// <param name="isCase"><para>zh-cn:是否区分大小写；为 `false` 时会先转换为小写再比较。</para><para>en-us:Whether comparison is case-sensitive; when `false`, both texts are converted to lowercase before comparison.</para></param>
        /// <returns><para>zh-cn:文本相似度百分比，范围通常为 0 到 100。</para><para>en-us:The text similarity percentage, usually ranging from 0 to 100.</para></returns>
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

        /// <summary>
        /// <para>zh-cn:将火星坐标转换为百度坐标。</para>
        /// <para>en-us:Converts Mars coordinates to Baidu coordinates.</para>
        /// </summary>
        /// <param name="marsCoordinate"><para>zh-cn:火星坐标，例如高德或腾讯地图坐标。</para><para>en-us:The Mars coordinate, such as coordinates used by Amap or Tencent Maps.</para></param>
        /// <returns><para>zh-cn:转换后的百度坐标。</para><para>en-us:The converted Baidu coordinate.</para></returns>
        public static CoordinateDto MarsToBaidu(CoordinateDto marsCoordinate)
        {
            double x = marsCoordinate.getLongitude;
            double y = marsCoordinate.getLatitude;
            double z = Math.Sqrt(x * x + y * y) + 0.00002 * Math.Sin(y * xPi);
            double theta = Math.Atan2(y, x) + 0.000003 * Math.Cos(x * xPi);
            return new CoordinateDto(dataDigit(6, z * Math.Cos(theta) + 0.0065),
                dataDigit(6, z * Math.Sin(theta) + 0.006));
        }

        /// <summary>
        /// <para>zh-cn:将百度坐标转换为火星坐标。</para>
        /// <para>en-us:Converts Baidu coordinates to Mars coordinates.</para>
        /// </summary>
        /// <param name="baiduCoordinate"><para>zh-cn:百度地图坐标。</para><para>en-us:The Baidu map coordinate.</para></param>
        /// <returns><para>zh-cn:转换后的火星坐标，例如高德或腾讯地图坐标。</para><para>en-us:The converted Mars coordinate, such as coordinates used by Amap or Tencent Maps.</para></returns>
        public static CoordinateDto BaiduToMars(CoordinateDto baiduCoordinate)
        {
            double x = baiduCoordinate.getLongitude - 0.0065;
            double y = baiduCoordinate.getLatitude - 0.006;
            double z = Math.Sqrt(x * x + y * y) - 0.00002 * Math.Sin(y * xPi);
            double theta = Math.Atan2(y, x) - 0.000003 * Math.Cos(x * xPi);
            return new CoordinateDto(dataDigit(6, z * Math.Cos(theta)), dataDigit(6, z * Math.Sin(theta)));
        }

        /// <summary>
        /// <para>zh-cn:根据百度坐标经纬度转换为火星坐标。</para>
        /// <para>en-us:Converts Baidu coordinate longitude and latitude to Mars coordinates.</para>
        /// </summary>
        /// <param name="longitude"><para>zh-cn:百度坐标经度。</para><para>en-us:The Baidu coordinate longitude.</para></param>
        /// <param name="latitude"><para>zh-cn:百度坐标纬度。</para><para>en-us:The Baidu coordinate latitude.</para></param>
        /// <returns><para>zh-cn:转换后的火星坐标。</para><para>en-us:The converted Mars coordinate.</para></returns>
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
