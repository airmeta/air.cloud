using System.Reflection;
using System.Runtime.Loader;

namespace Air.Cloud.Core.Reflection
{

    public class DynamicAssemblyLoaderUtil
    {
        public List<object> List { get; set; }
        private AssemblyLoadContext context;
        public List<object> Loads(List<string> Modules, string type, string Directory = "DLL")
        {
            context = new AssemblyLoadContext("SSS.Cloud.Core");
            context.Resolving += Context_Resolving;
            List = new List<object>();
            foreach (var item in Modules)
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Directory, item+".dll");
                if (File.Exists(path))
                {
                    try
                    {
                        using (var stream = File.OpenRead(path))
                        {
                            Assembly assembly = context.LoadFromStream(stream);

                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{ex.Message},{ex.StackTrace}");
                    }
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
            string expectedPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DLL", assemblyName.Name + ".dll"); ;
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
                    Console.WriteLine($"加载节点{expectedPath}发生异常：{ex.Message},{ex.StackTrace}");
                }
            }
            else
            {
                Console.WriteLine($"依赖文件不存在：{expectedPath}");
            }
            return null;
        }
    }
}

