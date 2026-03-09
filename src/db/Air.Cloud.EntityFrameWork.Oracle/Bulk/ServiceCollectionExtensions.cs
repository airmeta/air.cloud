using Air.Cloud.EntityFrameWork.Oracle.Bulk.Internal;

using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.DependencyInjection;

namespace Air.Cloud.EntityFrameWork.Oracle.Bulk
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddBatchEF_Oracle(this IServiceCollection services)
        {
            return services.AddScoped<IQuerySqlGeneratorFactory, ZackQuerySqlGeneratorFactory_Oracle>();
        }
    }
}
