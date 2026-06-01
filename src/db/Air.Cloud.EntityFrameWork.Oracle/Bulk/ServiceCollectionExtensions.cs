using Air.Cloud.EntityFrameWork.Oracle.Bulk.Internal;

using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.DependencyInjection;

namespace Air.Cloud.EntityFrameWork.Oracle.Bulk
{
    /// <summary>
    /// <para>zh-cn:提供 Oracle 批量 EF 服务注册扩展方法。</para>
    /// <para>en-us:Provides extension methods for registering Oracle batch EF services.</para>
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// <para>zh-cn:向服务集合注册 Oracle 批量操作所需的查询 SQL 生成器。</para>
        /// <para>en-us:Registers the query SQL generator required by Oracle batch operations into the service collection.</para>
        /// </summary>
        /// <param name="services">
        /// <para>zh-cn:应用服务集合。</para>
        /// <para>en-us:The application service collection.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:完成 Oracle 批量操作服务注册后的服务集合。</para>
        /// <para>en-us:The service collection after registering Oracle batch operation services.</para>
        /// </returns>
        public static IServiceCollection AddBatchEF_Oracle(this IServiceCollection services)
        {
            return services.AddScoped<IQuerySqlGeneratorFactory, ZackQuerySqlGeneratorFactory_Oracle>();
        }
    }
}
