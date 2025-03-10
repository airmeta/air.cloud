using Air.Cloud.Core;
using Air.Cloud.Core.App;
using Air.Cloud.Core.Dependencies;
using Air.Cloud.Core.Standard.DataBase.Model;
using Air.Cloud.Core.Standard.DataBase.Repositories;
using Air.Cloud.Core.Standard.DynamicServer.Extensions;
using Air.Cloud.Core.Standard.Print;
using Air.Cloud.DataBase.ElasticSearch.Attributes;
using Air.Cloud.DataBase.ElasticSearch.Connections;
using Air.Cloud.DataBase.ElasticSearch.Implantations;

using Microsoft.Extensions.DependencyInjection;

using System.Reflection;
using System.Runtime.Loader;

namespace Air.Cloud.DataBase.ElasticSearch.Extensions
{
    public static class ElasticSearchInjectExtensions
    {

        /// <summary>
        /// <para>zh-cn:注册调度任务</para>
        /// <para>en-us: Inject app scheduler</para>
        /// </summary>
        /// <param name="services">
        /// 服务集合
        /// </param>
        public static void AddElasticSearch(this IServiceCollection services) 
        {
            foreach (var item in AppCore.Assemblies.ToList())
            {
                try
                {
                    var AllEntities = AssemblyLoadContext.Default.LoadFromAssemblyName(item).GetTypes()
                            .Where(s => s.IsClass && s.GetInterfaces()
                                        .Contains(typeof(INoSqlEntity))
                                   )
                            .ToList();
                    foreach (var t in AllEntities)
                    {
                        ElasticSearchIndexAttribute? Index = t.GetCustomAttribute<ElasticSearchIndexAttribute>();
                        if (Index == null)
                        {
                            AppRealization.Output.Print(new AppPrintInformation
                            {
                                Title = "domain-errors",
                                Level = AppPrintInformation.AppPrintLevel.Error,
                                Content = $"未正确配置索引信息,[{t.FullName}]缺少[ElasticSearchIndex]特性",
                                State = true
                            });
                            continue;
                        }
                        Type genericType = typeof(ESNoSqlRepository<>);
                        Type constructedType = genericType.MakeGenericType(t);
                        AppCore.SetService(
                            typeof(INoSqlRepository<>).MakeGenericType(t), 
                            constructedType, 
                            DependencyInjectionServiceCollectionExtensions.TryGetServiceLifetime(typeof(ITransient)));
                        //注册连接池
                        ElasticSearchConnection.Pool.Set(new ElasticClientPoolElement(t));
                    }
                }
                catch (Exception ex)
                {
                    AppRealization.Output.Print(new AppPrintInformation
                    {
                        Title = "domain-errors",
                        Level = AppPrintInformation.AppPrintLevel.Error,
                        Content = $"注册ElasticSearch连接池异常,异常信息:{ex.Message}",
                        State = true
                    });
                }
            }
        }
    }
}
