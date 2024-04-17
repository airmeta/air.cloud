using Microsoft.Extensions.Configuration;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Air.Cloud.Modules.Consul.Util
{
    public interface IConfigurationLoader
    {
        /// <summary>
        /// 加载远程配置文件
        /// </summary>
        /// <param name="key"></param>
        /// <param name="FileName"></param>
        /// <returns></returns>
        (IConfiguration, IConfiguration) LoadRemoteConfiguration(string RemoteUrl = null, string key = null, string FileName = null);
    }
}
