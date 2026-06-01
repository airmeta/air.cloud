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
    /// <para>zh-cn:表示地图坐标数据，包含经度和纬度。</para>
    /// <para>en-us:Represents map coordinate data, including longitude and latitude.</para>
    /// </summary>
    public class CoordinateDto
    {
        /// <summary>
        /// <para>zh-cn:初始化空的坐标数据实例。</para>
        /// <para>en-us:Initializes an empty coordinate data instance.</para>
        /// </summary>
        public CoordinateDto()
        {
        }

        /// <summary>
        /// <para>zh-cn:使用指定经度和纬度初始化坐标数据实例。</para>
        /// <para>en-us:Initializes a coordinate data instance with the specified longitude and latitude.</para>
        /// </summary>
        /// <param name="longitude">
        /// <para>zh-cn:经度。</para>
        /// <para>en-us:The longitude.</para>
        /// </param>
        /// <param name="latitude">
        /// <para>zh-cn:纬度。</para>
        /// <para>en-us:The latitude.</para>
        /// </param>
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

        /// <summary>
        /// <para>zh-cn:获取或设置经度。</para>
        /// <para>en-us:Gets or sets the longitude.</para>
        /// </summary>
        public double getLongitude { get => longitude; set => longitude = value; }
        /// <summary>
        /// <para>zh-cn:获取或设置纬度。</para>
        /// <para>en-us:Gets or sets the latitude.</para>
        /// </summary>
        public double getLatitude { get => latitude; set => latitude = value; }
    }
}
