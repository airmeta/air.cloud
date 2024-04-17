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
namespace Air.Cloud.Core.Plugins.Map
{

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
