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
using KellermanSoftware.CompareNetObjects.TypeComparers;

using Mapster;

using Microsoft.Extensions.Options;

using Air.Cloud.Core.Plugins.IdGenerator;

namespace Air.Cloud.Core.Util
{
    /// <summary>
    /// 常用公共类
    /// </summary>
    public class CommonHelper
    {
        public static string CId = "11111111111111111111111111111111";


        #region 自动生成日期编号

        /// <summary>
        /// 自动生成编号  201008251145409865
        /// </summary>
        /// <returns></returns>
        public static string CreateNo(string fomat = "yyyyMMddHHmmss")
        {
            Random random = new Random();
            string strRandom = random.Next(1000, 10000).ToString(); //生成编号
            string code = DateTime.Now.ToString(fomat) + strRandom;//形如
            return code;
        }

        #endregion 自动生成日期编号


        /// <summary>
        /// 保留小数
        /// </summary>
        /// <param name="value">值</param>
        /// <param name="digit">保留的位数</param>
        /// <returns></returns>
        public static decimal ChinaRound(decimal value, int digit)
        {
            return Math.Round(value, digit, MidpointRounding.AwayFromZero);
        }

    }

    public class CoordinateDto
    {
        public CoordinateDto()
        {
        }

        public CoordinateDto(double longitude, double latitude)
        {
            this.longitude = longitude;
            this.latitude = latitude;
        }

        /**
         * 经度
         */
        private double longitude;

        /**
         * 纬度
         */
        private double latitude;

        public double getLongitude { get => longitude; set => longitude = value; }
        public double getLatitude { get => latitude; set => latitude = value; }
    }
}