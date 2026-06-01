using Air.Cloud.Core;
using Air.Cloud.Core.App;
using Air.Cloud.Core.Modules.AppPrint;
using Air.Cloud.Core.Standard.DataBase.Model;
using Air.Cloud.Core.Standard.DataBase.Repositories;
using Air.Cloud.Core.Standard.DynamicServer;
using Air.Cloud.Core.Standard.DynamicServer.Extensions;
using Air.Cloud.Modules.MongoDB.Attributes;
using Air.Cloud.Modules.MongoDB.Implantations;

using Microsoft.Extensions.DependencyInjection;

using System.Reflection;
using System.Runtime.Loader;

namespace Air.Cloud.Modules.MongoDB.Extensions
{
    /// <summary>
    /// <para>zh-cn:MongoDB 服务注册扩展，扫描 MongoDB 实体并注册对应的 NoSQL 仓储实现。</para>
    /// <para>en-us:MongoDB service registration extensions that scan MongoDB entities and register the corresponding NoSQL repository implementations.</para>
    /// </summary>
    public static class MongoDBInjectExtensions
    {
        /// <summary>
        /// <para>zh-cn:注册MongoDB服务</para>
        /// <para>en-us:Inject MongoDB repository services</para>
        /// </summary>
        /// <param name="services">服务集合</param>
        public static IServiceCollection AddMongoDB(this IServiceCollection services)
        {
            foreach (var item in AppCore.Assemblies.ToList())
            {
                try
                {
                    var allEntities = AssemblyLoadContext.Default.LoadFromAssemblyName(item).GetTypes()
                        .Where(s => s.IsClass
                                    && !s.IsAbstract
                                    && s.GetInterfaces().Contains(typeof(INoSqlEntity))
                                    && s.GetCustomAttribute<MongoCollectionAttribute>() != null)
                        .ToList();

                    foreach (var t in allEntities)
                    {
                        Type genericType = typeof(MongoNoSqlRepository<>);
                        Type constructedType = genericType.MakeGenericType(t);
                        AppCore.SetService(
                            typeof(INoSqlRepository<>).MakeGenericType(t),
                            constructedType,
                            DependencyInjectionServiceCollectionExtensions.TryGetServiceLifetime(typeof(ITransient)));
                    }
                }
                catch (Exception ex)
                {
                    AppRealization.Output.Print(new AppPrintInformation
                    {
                        Title = "domain-errors",
                        Level = AppPrintLevel.Error,
                        Content = $"注册MongoDB仓储异常,异常信息:{ex.Message}",
                        State = true
                    });
                }
            }

            return services;
        }
    }
}
