using Air.Cloud.Core.Dependencies;
using Air.Cloud.Core.Standard;

using System.Reflection;
using System.Runtime.Loader;

namespace Air.Cloud.Core.Modules.AppAssembly.Builder
{
    /// <summary>
    /// <para>zh-cn:应用程序集构建器标准接口</para>
    /// <para>en-us: Application Assembly Builder Standard Interface</para>
    /// </summary>
    public interface IAppAssemblyBuilder
    {
        /// <summary>
        /// <para>zh-cn:应用程序集构建器对象池</para>
        /// <para>en-us:Application Assembly Builder Object Pool</para>
        /// </summary>
        public static AppAssemblyBuilderPool AppAssemblyBuilderPool = new AppAssemblyBuilderPool(); 
        /// <summary>
        /// <para>zh-cn:主程序集名称</para>
        /// <para>en-us:Main Assembly MainAssemblyName</para>
        /// </summary>
        public AssemblyName MainAssemblyName { get; set; }

        /// <summary>
        ///  <para>zh-cn:程序集文件路径</para>
        ///  <para>en-us:Assembly file path</para>
        /// </summary>
        public string AssemblyFilePath { get; set; }
        /// <summary>
        /// <para>zh-cn:程序集加载上下文</para>
        /// <para>en-us:Assembly Load Context</para>
        /// </summary>
        public static AssemblyLoadContextPool Pool = new AssemblyLoadContextPool();

        /// <summary>
        /// <para>zh-cn:初始化程序集加载上下文</para>
        /// <para>en-us:Initialize Assembly Load Context</para>
        /// </summary>
        /// <returns></returns>
        AssemblyName InitializeAssemblyLoadContext();

        /// <summary>
        /// <para>zh-cn:卸载程序集加载上下文</para>
        /// <para>en-us:Unload Assembly Load Context</para>
        /// </summary>
        /// <returns>
        /// <para>zh-cn:是否卸载成功</para>
        /// <para>en-us:Whether the unload was successful</para>
        /// </returns>
        bool UnloadAssemblyLoadContext();


        /// <summary>
        /// <para>zh-cn:获取程序集加载上下文</para>
        /// <para>en-us:Get Assembly Load Context</para>
        /// </summary>
        /// <returns>
        ///  <para>zh-cn:返回程序集加载上下文</para>
        ///  <para>en-us:Returns the Assembly Load Context</para>
        /// </returns>
        public AssemblyLoadContext GetAssemblyLoadContext();

        /// <summary>
        /// <para>zh-cn:获取已加载的主程序集</para>
        /// <para>en-us:Get Loaded Main Assembly</para>
        /// </summary>
        /// <returns>
        ///  <para>zh-cn:返回已加载的主程序集</para>
        ///  <para>en-us:Returns the loaded main assembly</para>
        /// </returns>
        public Assembly GetLoadedMainAssembly( );

    }
}