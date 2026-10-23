using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Storage;

using Oracle.EntityFrameworkCore.Infrastructure.Internal;

namespace Air.Cloud.EntityFrameWork.Oracle.Bulk.Internal
{
    class ZackQuerySqlGeneratorFactory_Oracle : IQuerySqlGeneratorFactory
    {
        private ISqlGenerationHelper _sqlGenerationHelper;
        private readonly QuerySqlGeneratorDependencies _dependencies;
        private readonly IRelationalTypeMappingSource _typeMappingSource;

        private readonly IOracleOptions _oracleOptions;

        /// <summary>
        /// <para>zh-cn:Oracle 批量 SQL 生成成员。</para>
        /// <para>en-us:Oracle batch SQL generation member.</para>
        /// </summary>
        public ZackQuerySqlGeneratorFactory_Oracle(QuerySqlGeneratorDependencies dependencies,
            ISqlGenerationHelper sqlGenerationHelper, IOracleOptions oracleOptions,
            IRelationalTypeMappingSource typeMappingSource)
        {
            this._dependencies = dependencies;
            this._sqlGenerationHelper = sqlGenerationHelper;
            this._oracleOptions = oracleOptions;
            this._typeMappingSource = typeMappingSource;
        }
        /// <summary>
        /// <para>zh-cn:Oracle 批量 SQL 生成成员。</para>
        /// <para>en-us:Oracle batch SQL generation member.</para>
        /// </summary>
        public QuerySqlGenerator Create()
        {
            return new ZackQuerySqlGenerator_Oracle(this._dependencies, this._oracleOptions, this._sqlGenerationHelper, this._typeMappingSource);
        }
    }
}
