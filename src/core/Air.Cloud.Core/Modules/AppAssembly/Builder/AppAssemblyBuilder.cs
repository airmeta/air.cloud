using Air.Cloud.Core.Modules.DynamicApp;
using Air.Cloud.Core.Modules.DynamicApp.Extensions;
using Air.Cloud.Core.Modules.DynamicApp.Model;

using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.Loader;

namespace Air.Cloud.Core.Modules.AppAssembly.Builder
{
    /// <summary>
    /// <para>zh-cn:应用程序集构建器实现</para>
    /// <para>en-us: Application Assembly Builder Implementation</para>
    /// </summary>
    public class AppAssemblyBuilder :IAppAssemblyBuilder
    {
        /// <summary>
        /// <para>zh-cn:主程序集名称</para>
        /// <para>en-us: Get the assembly name</para>
        /// </summary>
        public AssemblyName MainAssemblyName { get ; set; }
        /// <summary>
        /// <para>zh-cn:程序集文件路径</para>
        /// <para>en-us: Assembly file path</para>
        /// </summary>
        public string AssemblyFilePath { get; set; }

        /// <summary>
        /// <para>zh-cn:获取程序集所在目录</para>
        /// <para>en-us: Get the directory of the assembly</para>
        /// </summary>
        public string AssemblyDirectoryPath
        {
            get
            {
                return Path.GetDirectoryName(AssemblyFilePath);
            }
        }

        public string ConfigFilePath
        {
            get
            {
                string Dir = Path.GetDirectoryName(AssemblyDirectoryPath);
                string filePath = Path.Combine(Dir, IDynamicAppStoreStandard.MOD_CONFIG_FILE_NAME);
                return filePath;
            }
        }
        /// <inheritdoc/>
        public AssemblyName InitializeAssemblyLoadContext()
        {
            string AssemblyFileName = Path.GetFileName(AssemblyFilePath);
            if (IAppAssemblyBuilder.Pool.IsExists(AssemblyFileName))
            {
                AppRealization.Output.Print(new AppPrintInformation()
                {
                    State = true,
                    AdditionalParams = null,
                    Content = $"AssemblyLoadContext {AssemblyFileName} is exists",
                    Title = "AssemblyLoadContextBuilder Notice",
                    Level = AppPrintLevel.Warn
                });
                MainAssemblyName=AssemblyLoadContext.GetAssemblyName(AssemblyFilePath);
                return MainAssemblyName;
            }
            AssemblyName AssemblyName = null;
            AssemblyLoadContext context = new AssemblyLoadContext(AssemblyFileName);
            context.Resolving += Context_Resolving;
            if (File.Exists(AssemblyFilePath))
            {
                //获取AssemblyFilePath文件所在的文件夹路径  并加载文件夹下所有文件
                //废弃版本(读取了文件夹下所有dll 但是在这里会出现问题,后续将会在研究 服务管理器 的功能中单独完成 涉及到一些底层的程序集加载问题 )
                //var Assemblies = Directory.GetFiles(AssemblyDirectoryPath).Where(s =>
                //{
                //    return Path.GetFileName(s).ToLower() != AssemblyFilePath.ToLower() && s.ToLower().EndsWith(".dll");
                //}).ToList();

                ModInformation information=DynamicModUtil.ParseModInfo(ConfigFilePath);

                string directory = Path.GetDirectoryName(AssemblyFilePath);
                foreach (var item in information.Assemblies)
                {
                    _ = TryLoadNormalAssembly($"{Path.Combine(directory, item.Name)}.{IDynamicAppStoreStandard.ASSEMBLY_EXTENSIONS} ", context);
                }
                AssemblyName = TryLoadMainAssembly(AssemblyFileName, AssemblyName, context);
            }
            this.MainAssemblyName = AssemblyName;
            return MainAssemblyName;
        }

        /// <summary>
        /// <para>zh-cn:尝试加载主程序集</para>
        /// <para>en-us: Try to load the main assembly</para>
        /// </summary>
        /// <param name="AssemblyFileName">
        ///  <para>zh-cn:程序集文件名称</para>
        ///  <para>en-us: Assembly file name</para>
        /// </param>
        /// <param name="AssemblyName">
        ///  <para>zh-cn:程序集名称</para>
        ///  <para>en-us: Assembly name</para>
        /// </param>
        /// <param name="context">
        ///  <para>zh-cn:程序集加载上下文</para>
        ///  <para>en-us: Assembly load context</para>
        /// </param>
        /// <returns>
        ///  <para>zh-cn:返回程序集名称</para>
        ///  <para>en-us: Returns the assembly name</para>
        /// </returns>
        private AssemblyName TryLoadMainAssembly(string AssemblyFileName, AssemblyName AssemblyName, AssemblyLoadContext context)
        {
            try
            {
                using (var stream = File.OpenRead(AssemblyFilePath))
                {
                    Assembly assembly = context.LoadFromStream(stream);
                    AssemblyName = assembly.GetName();
                    IAppAssemblyBuilder.Pool.Set(new Model.AssemblyLoadContextInformation()
                    {
                        AssemblyName = AssemblyName,
                        Assembly = assembly,
                        Name = AssemblyName.Name,
                        AssemblyPath = AssemblyFilePath,
                        Context = context,
                        LoadTime = DateTime.Now
                    });
                    AppRealization.Output.Print(new AppPrintInformation()
                    {
                        State = true,
                        AdditionalParams = null,
                        Content = $"AssemblyLoadContext {AssemblyFileName} load success",
                        Title = "AssemblyLoadContextBuilder Notice",
                        Level = AppPrintLevel.Information
                    });
                }
            }
            catch (Exception ex)
            {
                AppRealization.Output.Print(new AppPrintInformation()
                {
                    State = true,
                    AdditionalParams = new Dictionary<string, object>()
                        {
                            {"Message",ex.Message },
                            {"StackTrace",ex.StackTrace }
                        },
                    Content = $"AssemblyLoadContext {AssemblyFileName} has error",
                    Title = "AssemblyLoadContextBuilder Notice",
                    Level = AppPrintLevel.Error
                });
            }

            return AssemblyName;
        }

        private bool TryLoadNormalAssembly(string AssemblyFilePath, AssemblyLoadContext context)
        {
            string AssemblyFilePathName = Path.GetFileName(AssemblyFilePath);
            try
            {
                using (var stream = File.OpenRead(AssemblyFilePath))
                {
                    Assembly assembly = context.LoadFromStream(stream);
                    AppRealization.Output.Print(new AppPrintInformation()
                    {
                        State = true,
                        AdditionalParams = null,
                        Content = $"AssemblyLoadContext {AssemblyFilePathName} load success",
                        Title = "AssemblyLoadContextBuilder Notice",
                        Level = AppPrintLevel.Information
                    });
                }
            }
            catch (Exception ex)
            {
                AppRealization.Output.Print(new AppPrintInformation()
                {
                    State = true,
                    AdditionalParams = new Dictionary<string, object>()
                        {
                            {"Message",ex.Message },
                            {"StackTrace",ex.StackTrace }
                        },
                    Content = $"AssemblyLoadContext {AssemblyFilePathName} has error",
                    Title = "AssemblyLoadContextBuilder Notice",
                    Level = AppPrintLevel.Error
                });
                return false;
            }

            return true;
        }


        /// <inheritdoc/>
        public bool UnloadAssemblyLoadContext()
        {
            var contextInfo = IAppAssemblyBuilder.Pool.Get(MainAssemblyName.Name);
            if (contextInfo == null) return false;
            try
            {
                contextInfo.Context.Unload();
                IAppAssemblyBuilder.Pool.Remove(MainAssemblyName.Name);
                AppRealization.Output.Print(new AppPrintInformation()
                {
                    State = true,
                    AdditionalParams = null,
                    Content = $"AssemblyLoadContext {MainAssemblyName.Name} unload success",
                    Title = "AssemblyLoadContextBuilder Notice",
                    Level = AppPrintLevel.Information
                });
                return true;
            }
            catch (Exception ex)
            {
                AppRealization.Output.Print(new AppPrintInformation()
                {
                    State = false,
                    AdditionalParams = null,
                    Content = $"AssemblyLoadContext {MainAssemblyName.Name} unload fail,exception:{ex.Message},{ex.StackTrace}",
                    Title = "AssemblyLoadContextBuilder Notice",
                    Level = AppPrintLevel.Error
                });
                return false;
            }
        }

        /// <summary>
        /// 加载依赖文件
        /// </summary>
        /// <param name="context"></param>
        /// <param name="assemblyName"></param>
        /// <returns></returns>
        private Assembly Context_Resolving(AssemblyLoadContext context, AssemblyName assemblyName)
        {
            string expectedPath = Path.Combine(Path.GetDirectoryName(AssemblyFilePath), $"{assemblyName.Name}.dll");

            if (File.Exists(expectedPath))
            {
                try
                {
                    using (var stream = File.OpenRead(expectedPath))
                    {
                        return context.LoadFromStream(stream);
                    }
                }
                catch (Exception ex)
                {
                    AppRealization.Output.Print(new AppPrintInformation()
                    {
                        State = true,
                        AdditionalParams = new Dictionary<string, object>()
                        {
                            {"Message",ex.Message },
                            {"StackTrace",ex.StackTrace }
                        },
                        Content = $"AssemblyLoadContext load child  {Path.GetFileName(expectedPath)} has error",
                        Title = "AssemblyLoadContextBuilder Notice",
                        Level = AppPrintLevel.Error
                    });
                }
            }
            else
            {
                AppRealization.Output.Print(new AppPrintInformation()
                {
                    State = true,
                    AdditionalParams = null,
                    Content = $"The file {Path.GetFileName(expectedPath)} not exits ",
                    Title = "AssemblyLoadContextBuilder Notice",
                    Level = AppPrintLevel.Error
                });
            }
            return null;
        }
        /// <inheritdoc/>
        public AssemblyLoadContext GetAssemblyLoadContext()
        {
            var item= IAppAssemblyBuilder.Pool.Get(MainAssemblyName.Name);

            return item != null ? item.Context: null;
        }
        /// <inheritdoc/>
        public Assembly GetLoadedMainAssembly()
        {
            var item = IAppAssemblyBuilder.Pool.Get(MainAssemblyName.Name);

            return item != null ? item.Assembly : null;
        }
    }
}
