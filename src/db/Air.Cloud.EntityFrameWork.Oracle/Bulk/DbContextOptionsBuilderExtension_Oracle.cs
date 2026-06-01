using Air.Cloud.EntityFrameWork.Oracle.Bulk.Internal;

using Microsoft.EntityFrameworkCore.Query;

namespace Air.Cloud.EntityFrameWork.Oracle.Bulk
{
    /// <summary>
    /// <para>zh-cn:提供用于启用 Oracle 批量 EF 查询 SQL 生成器的 DbContextOptionsBuilder 扩展方法。</para>
    /// <para>en-us:Provides DbContextOptionsBuilder extension methods for enabling the Oracle batch EF query SQL generator.</para>
    /// </summary>
    public static class DbContextOptionsBuilderExtension_Oracle
    {
        /// <summary>
        /// <para>zh-cn:将当前 DbContextOptionsBuilder 的查询 SQL 生成器替换为 Oracle 批量操作实现。</para>
        /// <para>en-us:Replaces the query SQL generator of the current DbContextOptionsBuilder with the Oracle batch operation implementation.</para>
        /// </summary>
        /// <param name="optBuilder">
        /// <para>zh-cn:需要配置 Oracle 批量操作支持的 DbContextOptionsBuilder。</para>
        /// <para>en-us:The DbContextOptionsBuilder that should be configured with Oracle batch operation support.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:完成 Oracle 批量操作服务替换后的 DbContextOptionsBuilder。</para>
        /// <para>en-us:The DbContextOptionsBuilder after replacing the Oracle batch operation service.</para>
        /// </returns>
        public static DbContextOptionsBuilder UseBatchEF_Oracle(this DbContextOptionsBuilder optBuilder)
        {
            optBuilder.ReplaceService<IQuerySqlGeneratorFactory, ZackQuerySqlGeneratorFactory_Oracle>();
            return optBuilder;
        }
    }
}
