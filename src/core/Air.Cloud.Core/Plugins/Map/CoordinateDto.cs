using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
