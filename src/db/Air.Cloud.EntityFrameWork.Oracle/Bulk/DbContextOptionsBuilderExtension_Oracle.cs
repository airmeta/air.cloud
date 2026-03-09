using Air.Cloud.EntityFrameWork.Oracle.Bulk.Internal;

using Microsoft.EntityFrameworkCore.Query;

namespace Air.Cloud.EntityFrameWork.Oracle.Bulk
{
    public static class DbContextOptionsBuilderExtension_Oracle
    {
        public static DbContextOptionsBuilder UseBatchEF_Oracle(this DbContextOptionsBuilder optBuilder)
        {
            optBuilder.ReplaceService<IQuerySqlGeneratorFactory, ZackQuerySqlGeneratorFactory_Oracle>();
            return optBuilder;
        }
    }
}
