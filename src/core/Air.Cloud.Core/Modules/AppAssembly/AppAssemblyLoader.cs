using Air.Cloud.Core.App.Startups;
using Air.Cloud.Core.Enhance;
using Air.Cloud.Core.Plugins;
using Air.Cloud.Core.Standard;
using Air.Cloud.Core.Standard.DataBase.Model;
using Air.Cloud.Core.Standard.DynamicServer;

using Microsoft.Extensions.DependencyModel;

using System.Reflection;
using System.Runtime.Loader;

namespace Air.Cloud.Core.Modules.AppAssembly
{
    /// <summary>
    /// <para>zh-cn:应用程序集加载器</para>
    /// <para>en-us:Application Assembly Loader</para>
    /// </summary>
    public class AppAssemblyLoader
    {
        /// <summary>
        /// 获取应用有效程序集
        /// </summary>
        /// <returns>IEnumerable</returns>
        internal static IEnumerable<AssemblyName> GetAssemblies()
        {
            // 排除数据库迁移程序集 这里可以换成内存动态生成一个Database.Migrations程序集
            var ExcludeAssemblyNames = new string[] { "Database.Migrations" };
            IEnumerable<AssemblyName> scanAssemblies = DependencyContext.Default.RuntimeLibraries
             .Where(u =>
                    (u.Type == "project" && !ExcludeAssemblyNames.Any(j => u.Name.EndsWith(j))) ||
                    (u.Type == "package" && (u.Name.StartsWith(nameof(Air)) || AppCore.Settings.SupportPackageNamePrefixs.Any(p => u.Name.StartsWith(p)))) ||
                    (AppCore.Settings.EnabledReferenceAssemblyScan == true && u.Type == "reference"))    // 判断是否启用引用程序集扫描
             .Select(u => new AssemblyName(u.Name));
            return scanAssemblies;
        }

        /// <summary>
        /// 加载程序集中的所有类型
        /// </summary>
        /// <param name="ass">程序集名称</param>
        /// <returns></returns>
        internal static IEnumerable<Type> GetTypes(AssemblyName ass)
        {
            IEnumerable<Type> types = new List<Type>();
            try
            {
                types = AssemblyLoadContext.Default.LoadFromAssemblyName(ass).GetTypes().Where(t =>
                {
                    var instances = t.GetInterfaces();
                    if ((instances.Contains(typeof(IPlugin))
                    || instances.Contains(typeof(IModule))
                    || instances.Contains(typeof(IEnhance))
                    || instances.Contains(typeof(IStandard))
                    || instances.Contains(typeof(IDynamicService))
                    || instances.Contains(typeof(IPrivateEntity))
                    ) && t.IsPublic && !t.IsDefined(typeof(IgnoreScanningAttribute), false))
                        return true;
                    if (t.IsDefined(typeof(NeedScanningAttribute)))
                        return true;
                    if (t.BaseType == typeof(AppStartup))
                        return true;
                    return false;
                }).ToList();
            }
            catch
            {
                Console.WriteLine($"构建类库分析器时失败,失败类库:[{ass.FullName}]");
            }
            return types;
        }
    }
}
