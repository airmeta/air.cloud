namespace Air.Cloud.Core
{
    /// <summary>
    /// <para>zh-cn:提供应用生命周期钩子入口，用于承载启动、配置加载成功和停止等生命周期扩展逻辑。</para>
    /// <para>en-us:Provides application lifetime hook entry points used to host lifecycle extension logic such as start, configuration-load success, and stop.</para>
    /// </summary>
    public class AppLifetime
    {
        /// <summary>
        /// <para>zh-cn:初始化 `AppLifetime` 类型。</para>
        /// <para>en-us:Initializes the `AppLifetime` type.</para>
        /// </summary>
        static AppLifetime()
        {
        }

        /// <summary>
        /// <para>zh-cn:触发应用启动生命周期逻辑。</para>
        /// <para>en-us:Triggers application start lifecycle logic.</para>
        /// </summary>
        public static void Start()
        {
        }

        /// <summary>
        /// <para>zh-cn:触发配置加载成功后的生命周期逻辑。</para>
        /// <para>en-us:Triggers lifecycle logic after configuration has been loaded successfully.</para>
        /// </summary>
        public void ConfigurationLoadSuccess()
        {
            // 配置加载成功逻辑
        }

        /// <summary>
        /// <para>zh-cn:触发应用停止生命周期逻辑。</para>
        /// <para>en-us:Triggers application stop lifecycle logic.</para>
        /// </summary>
        public void Stop()
        {
        }
    }
}
