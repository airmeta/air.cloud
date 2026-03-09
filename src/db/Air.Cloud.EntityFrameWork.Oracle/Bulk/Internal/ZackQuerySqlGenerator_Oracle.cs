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
        /// columns of the select statement
        /// </summary>
        private List<string> _projectionSQL = new List<string>();

        /// <summary>
        /// if IsForSingleTable=true, ZackQuerySqlGenerator will change the default behavior to capture PredicateSQL and so on.
        /// if IsForSingleTable=false, ZackQuerySqlGenerator will use all the implementations of base class
        /// </summary>
        public bool IsForBatchEF { get; set; }

        public List<string> ProjectionSQL
        {
            get
            {
                return this._projectionSQL;
            }
        }

        /// <summary>
        /// the where clause
        /// </summary>
        public string PredicateSQL
        {
            get;
            set;
        }

        private ISqlGenerationHelper _sqlGenerationHelper;
        public ZackQuerySqlGenerator_Oracle(QuerySqlGeneratorDependencies dependencies, IOracleOptions _oracleOptions, ISqlGenerationHelper sqlGenerationHelper)
            : base(dependencies, _oracleOptions.OracleSQLCompatibility)
        {
            this._sqlGenerationHelper = sqlGenerationHelper;
            this.IsForBatchEF = false;
        }


        protected override Expression VisitSelect(SelectExpression selectExpression)
        {
            if (!IsForBatchEF)
            {
                return base.VisitSelect(selectExpression);
            }
            return SqlGeneratorUtils.VisitSelect(this, this._sqlGenerationHelper, selectExpression);
        }

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

        public IRelationalCommandBuilder P_Sql => this.Sql;
        public string P_AliasSeparator => this.AliasSeparator;


        public void P_GenerateSetOperation(SetOperationBase setOperation) => this.GenerateSetOperation(setOperation);

        public void P_GenerateTop(SelectExpression selectExpression) => this.GenerateTop(selectExpression);

        public void P_GeneratePseudoFromClause()=> this.GeneratePseudoFromClause();

        public void P_GenerateOrderings(SelectExpression selectExpression) => this.GenerateOrderings(selectExpression);

        public void P_GenerateLimitOffset(SelectExpression selectExpression) => this.GenerateLimitOffset(selectExpression);

        public DbParameter CreateParameter(string parameterName, object? parameterValue = null)=> new OracleParameter(parameterName, parameterValue);

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