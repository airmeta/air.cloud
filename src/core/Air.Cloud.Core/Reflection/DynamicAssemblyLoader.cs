using System.Reflection;
using System.Runtime.Loader;

namespace Air.Cloud.Core.Reflection
{
    /// <summary>
    /// <para>zh-cn:动态程序集加载器</para>
    /// <para>en-us: Load Dynamic Assembly  </para>
    /// </summary>
    public class DynamicAssemblyLoader
    {
        /// <summary>
        /// <para>zh-cn:上下文名称</para>
        /// <para>en-us:Context name</para>
        /// </summary>
        private readonly string AssemblyContextName = string.Empty;
        /// <summary>
        /// <para>zh-cn:上下文名称</para>
        /// <para>en-us:Context name</para>
        /// </summary>
        private readonly string AssemblyContextDirectory = string.Empty;
        /// <summary>
        /// <para>zh-cn:程序集名称序列化</para>
        /// <para>en-us:Assembly name format</para>
        /// </summary>
        private readonly string AssemblyNameFormat = "{0}.dll";
        /// <summary>
        /// <para>zh-cn:上下文信息</para>
        /// <para>en-us:AssemblyLoadContext</para>
        /// </summary>
        private readonly AssemblyLoadContext context;
        /// <summary>
        /// <para>zh-cn:程序集全地址</para>
        /// <para>en-us:Assembly full path</para>
        /// </summary>
        /// <param name="AssemblyName">
        /// <para>zh-cn:程序集名称</para>
        /// <para>en-us:Assembly name</para>
        /// </param>
        private string AssemblyFullPath(string AssemblyName) => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, AssemblyContextDirectory, string.Format(AssemblyNameFormat, AssemblyName));
        /// <summary>
        /// <para>zh-cn:程序集信息</para>
        /// <para>en-us:Assemblys</para>
        /// </summary>
        private List<Assembly> List { get; set; }
        /// <summary>
        /// <para>zh-cn:初始化程序集加载器</para>
        /// <para>en-us: Initialize assembly loader </para>
        /// </summary>
        /// <param name="assemblyContextName">
        /// <para>zh-cn:上下文名称</para>
        /// <para>en-us:Context name</para>
        /// </param>
        /// <param name="assemblyContextDirectory">
        /// <para>zh-cn:上下文目录</para>
        /// <para>en-us:Context directory</para>
        /// </param>
        /// <param name="assemblyLoadContext">
        /// <para>zh-cn:上下文</para>
        /// <para>en-us:Assembly load context</para>
        /// </param>
        public DynamicAssemblyLoader(string assemblyContextName = "Air.Cloud.Core.Plugins", string assemblyContextDirectory = "DLL",AssemblyLoadContext  assemblyLoadContext=null)
        {
            this.AssemblyContextName = assemblyContextName;
            this.AssemblyContextDirectory = assemblyContextDirectory;
            if (assemblyLoadContext == null)
            {
                this.context = new AssemblyLoadContext(AssemblyContextName);
                this.context.Resolving += Context_Resolving;
            }
            else
            {
                this.context = assemblyLoadContext;
            }
            this.List = new List<Assembly>();
        }
        /// <summary>
        /// <para>zh-cn:加载程序集</para>
        /// <para>en-us:Load</para>
        /// </summary>
        /// <param name="AssemblyNames">
        /// <para>zh-cn:程序集信息</para>
        /// <para>en-us:Assembly name</para>
        /// </param>
        /// <returns></returns>
        public List<Assembly> Load(List<string> AssemblyNames)
        {
            foreach (var item in AssemblyNames)
            {
                string path = AssemblyFullPath(item);
                if (!File.Exists(path)) continue;
                try
                {
                    using (var stream = File.OpenRead(path))
                    {
                        Assembly assembly = context.LoadFromStream(stream);
                        List.Add(assembly);
                    }
                }
                catch (Exception ex)
                {
                    AppRealization.Output.Print(new AppPrintInformation()
                    {
                        Title= "动态加载程序集时出现异常",
                        Content= ex.ToString(),
                        State=true,
                        AdditionalParams=new Dictionary<string, object> {
                             {"error",ex }
                        },
                        Level=AppPrintInformation.AppPrintLevel.Error
                    });
                }
            }
            return List;
        }
        /// <summary>
        /// 加载依赖文件
        /// </summary>
        /// <param name="context"></param>
        /// <param name="assemblyName"></param>
        /// <returns></returns>
        private Assembly Context_Resolving(AssemblyLoadContext context, AssemblyName assemblyName)
        {
            string path = AssemblyFullPath(assemblyName.Name);
            if (!File.Exists(path)) return null;
            try
            {
                using (var stream = File.OpenRead(path))
                {
                    return context.LoadFromStream(stream);
                }
            }
            catch (Exception ex)
            {
                AppRealization.Output.Print(new AppPrintInformation()
                {
                    Title = "动态加载程序集依赖时出现异常",
                    Content = ex.ToString(),
                    State = true,
                    AdditionalParams = new Dictionary<string, object> {
                             {"error",ex }
                        },
                    Level = AppPrintInformation.AppPrintLevel.Error
                });
            }
            return null;
        }
    }
}

