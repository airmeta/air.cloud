using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Air.Cloud.Modules.RedisCache.Options
{
    /// <summary>
    /// Redis配置项
    /// </summary>
    public  class RedisSettingsOptions
    {
        /// <summary>
        /// Redis连接字符串
        /// </summary>
        public string? ConnectionString { get; set; }
    }
}
