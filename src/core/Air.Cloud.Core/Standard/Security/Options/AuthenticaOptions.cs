using Air.Cloud.Core.App.Options;

namespace Air.Cloud.Core.Standard.Security.Options
{
    [ConfigurationInfo("AuthenticaSettings")]
    public  class AuthenticaOptions
    {
        /// <summary>
        /// <para>zh-cn:服务端地址</para>
        /// <para>en-us:Server address</para>
        /// </summary>
        public string ServerAddress { get; set; }

        /// <summary>
        /// <para>zh-cn:服务端地址</para>
        /// <para>en-us:Server address</para>
        /// </summary>
        /// <remarks>
        /// <para>zh-cn:如果配置文件中没有配置服务端地址，则使用网关地址</para>
        /// <para>en-us:If the server address is not configured in the configuration file, the gateway address is used</para>
        /// </remarks>
        public string GetServerAddress() => string.IsNullOrEmpty(ServerAddress) ? AppConfiguration.GetConfig<AppSettingsOptions>().GateWayAddress : ServerAddress;
        public string PushRoute { get; set; }="/authenticaton/endpoint/push";

        public int StoreIntervalMillis{get;set; }= 60000;

        /// <summary>
        /// <para>zh-cn:重试间隔时间，单位毫秒</para>
        /// <para>en-us:Retry interval time in milliseconds</para>
        /// </summary>
        public int RetryIntervalMillis{get;set; }= 10000;

    }
}
