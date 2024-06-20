using Air.Cloud.Core.App;
using Air.Cloud.Core;
using Air.Cloud.Modules.SkyWalking.Const;
using Air.Cloud.Modules.SkyWalking.Options;

using Microsoft.Extensions.DependencyInjection;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Air.Cloud.Modules.SkyWalking.Extensions
{
    public static  class SkyWalkingModuleExtensions
    {

        public static IServiceCollection  AddSkyWalking(this IServiceCollection services)
        {
            string ConfigFilePath = $"{AppConst.ApplicationPath}{SkyApmConst.SKYWALKING_CONFIG_NAME}";
            var Options = AppRealization.JSON.Deserialize<SkyApmOptions>(File.ReadAllText(ConfigFilePath));
            Options.ServiceName = AppConst.ApplicationInstanceName;
            if (File.Exists(ConfigFilePath)) File.Delete(ConfigFilePath);
            File.WriteAllText(ConfigFilePath, AppRealization.JSON.Serialize(Options));
            return services;
        }
    }
}
