using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;

using Oracle.EntityFrameworkCore.Infrastructure.Internal;
using Oracle.EntityFrameworkCore.Query.Sql.Internal;
using Oracle.ManagedDataAccess.Client;

using System.Data.Common;
using System.Linq.Expressions;

using Zack.EFCore.Batch.Internal;

namespace Air.Cloud.EntityFrameWork.Oracle.Bulk.Internal
{
    class ZackQuerySqlGenerator_Oracle : OracleQuerySqlGenerator, IZackQuerySqlGenerator
    {
        /// <summary>
        /// <para>zh-cn:SELECT 语句的列投影 SQL。</para>
        /// <para>en-us:The column projection SQL for the SELECT statement.</para>
        /// </summary>
        private List<string> _projectionSQL = new List<string>();

        /// <summary>
        /// <para>zh-cn:批量 EF 模式下用于控制 SQL 生成行为的标志。</para>
        /// <para>en-us:Flag that controls SQL-generation behavior for batch EF mode.</para>
        /// </summary>
        public bool IsForBatchEF { get; set; }

        /// <summary>
        /// <para>zh-cn:Oracle 批量 SQL 生成成员。</para>
        /// <para>en-us:Oracle batch SQL generation member.</para>
        /// </summary>
        public List<string> ProjectionSQL
        {
            get
            {
                return this._projectionSQL;
            }
        }

        /// <summary>
        /// <para>zh-cn:生成的 WHERE 条件 SQL。</para>
        /// <para>en-us:The generated WHERE-clause SQL.</para>
        /// </summary>
        public string PredicateSQL
        {
            get;
            set;
        }

        private ISqlGenerationHelper _sqlGenerationHelper;
        /// <summary>
        /// <para>zh-cn:Oracle 批量 SQL 生成成员。</para>
        /// <para>en-us:Oracle batch SQL generation member.</para>
        /// </summary>
        public ZackQuerySqlGenerator_Oracle(QuerySqlGeneratorDependencies dependencies, IOracleOptions _oracleOptions,
            ISqlGenerationHelper sqlGenerationHelper, IRelationalTypeMappingSource typeMappingSource)
            : base(dependencies, typeMappingSource, _oracleOptions.OracleSQLCompatibility)
        {
            this._sqlGenerationHelper = sqlGenerationHelper;
            this.IsForBatchEF = false;
        }


        /// <summary>
        /// <para>zh-cn:Oracle 批量 SQL 生成成员。</para>
        /// <para>en-us:Oracle batch SQL generation member.</para>
        /// </summary>
        protected override Expression VisitSelect(SelectExpression selectExpression)
        {
            if (!IsForBatchEF)
            {
                return base.VisitSelect(selectExpression);
            }
            return SqlGeneratorUtils.VisitSelect(this, this._sqlGenerationHelper, selectExpression);
        }

        /// <summary>
        /// <para>zh-cn:Oracle 批量 SQL 生成成员。</para>
        /// <para>en-us:Oracle batch SQL generation member.</para>
        /// </summary>
        protected override Expression VisitColumn(ColumnExpression columnExpression)
        {
            if (IsForBatchEF)
            {
                Sql.Append(_sqlGenerationHelper.DelimitIdentifier(columnExpression.Name));
                return columnExpression;
            }
            else
            {
                return base.VisitColumn(columnExpression);
            }
        }

        /// <summary>
        /// <para>zh-cn:Oracle 批量 SQL 生成成员。</para>
        /// <para>en-us:Oracle batch SQL generation member.</para>
        /// </summary>
        protected override Expression VisitTable(TableExpression tableExpression)
        {
            if (IsForBatchEF)
            {
                Sql.Append(_sqlGenerationHelper.DelimitIdentifier(tableExpression.Name));
                return tableExpression;
            }
            else
            {
                return base.VisitTable(tableExpression);
            }
        }

        /// <summary>
        /// <para>zh-cn:Oracle 批量 SQL 生成成员。</para>
        /// <para>en-us:Oracle batch SQL generation member.</para>
        /// </summary>
        public IRelationalCommandBuilder P_Sql => this.Sql;
        /// <summary>
        /// <para>zh-cn:Oracle 批量 SQL 生成成员。</para>
        /// <para>en-us:Oracle batch SQL generation member.</para>
        /// </summary>
        public string P_AliasSeparator => this.AliasSeparator;


        /// <summary>
        /// <para>zh-cn:Oracle 批量 SQL 生成成员。</para>
        /// <para>en-us:Oracle batch SQL generation member.</para>
        /// </summary>
        public void P_GenerateSetOperation(SetOperationBase setOperation) => this.GenerateSetOperation(setOperation);

        /// <summary>
        /// <para>zh-cn:Oracle 批量 SQL 生成成员。</para>
        /// <para>en-us:Oracle batch SQL generation member.</para>
        /// </summary>
        public void P_GenerateTop(SelectExpression selectExpression) => this.GenerateTop(selectExpression);

        /// <summary>
        /// <para>zh-cn:Oracle 批量 SQL 生成成员。</para>
        /// <para>en-us:Oracle batch SQL generation member.</para>
        /// </summary>
        public void P_GeneratePseudoFromClause()=> this.GeneratePseudoFromClause();

        /// <summary>
        /// <para>zh-cn:Oracle 批量 SQL 生成成员。</para>
        /// <para>en-us:Oracle batch SQL generation member.</para>
        /// </summary>
        public void P_GenerateOrderings(SelectExpression selectExpression) => this.GenerateOrderings(selectExpression);

        /// <summary>
        /// <para>zh-cn:Oracle 批量 SQL 生成成员。</para>
        /// <para>en-us:Oracle batch SQL generation member.</para>
        /// </summary>
        public void P_GenerateLimitOffset(SelectExpression selectExpression) => this.GenerateLimitOffset(selectExpression);

        /// <summary>
        /// <para>zh-cn:Oracle 批量 SQL 生成成员。</para>
        /// <para>en-us:Oracle batch SQL generation member.</para>
        /// </summary>
        public DbParameter CreateParameter(string parameterName, object? parameterValue = null)=> new OracleParameter(parameterName, parameterValue);

        /// <summary>
        /// <para>zh-cn:Oracle 批量 SQL 生成成员。</para>
        /// <para>en-us:Oracle batch SQL generation member.</para>
        /// </summary>
        public IRelationalCommand GetCommand(SelectExpression selectExpression)
        {
            // 1. 直接调用 Visit。
            // 基类会自动将 SQL 生成到 this.Sql (只读属性) 指向的 Builder 中。
            // 由于 Generator 通常是瞬态实例，this.Sql 初始状态是空的，无需手动清空。
            Visit(selectExpression);

            // 2. 调用 Build() 方法获取最终结果
            return Sql.Build();
        }
    }
}
