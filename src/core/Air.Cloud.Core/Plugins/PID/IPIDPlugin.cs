using Air.Cloud.Core.Dependencies;

namespace Air.Cloud.Core.Plugins.PID
{
    /// <summary>
    /// PID 插件
    /// </summary>
    public interface IPIDPlugin:IPlugin,ISingleton
    {

        public static string PID_FILE_PATH = "start.pid";
        public static string StartPath = AppDomain.CurrentDomain.SetupInformation.ApplicationBase + "/" + PID_FILE_PATH;

        /// <summary>
        /// 获取PID
        /// </summary>
        /// <returns></returns>
        public string Get();

        /// <summary>
        /// 设置PID
        /// </summary>
        /// <param name="PID">PID内容 允许你手动指定</param>
        /// <returns></returns>
        public string Set(string PID = null);
    }
}
