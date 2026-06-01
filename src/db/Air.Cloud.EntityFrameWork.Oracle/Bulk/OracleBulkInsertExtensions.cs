using Oracle.ManagedDataAccess.Client;

using Zack.EFCore.Batch.Internal;

namespace Air.Cloud.EntityFrameWork.Oracle.Bulk
{
	/// <summary>
	/// <para>zh-cn:提供基于 OracleBulkCopy 的 Oracle 批量写入扩展方法。</para>
	/// <para>en-us:Provides Oracle bulk write extension methods based on OracleBulkCopy.</para>
	/// </summary>
	public static class OracleBulkInsertExtensions
	{
		private static OracleBulkCopy BuildSqlBulkCopy<TEntity>(OracleConnection conn, DbContext dbCtx, OracleBulkCopyOptions copyOptions) where TEntity : class
		{
			var dbSet = dbCtx.Set<TEntity>();
			var entityType = dbSet.EntityType;
			var dbProps = BulkInsertUtils.ParseDbProps<TEntity>(dbCtx, entityType);

			OracleBulkCopy bulkCopy = new OracleBulkCopy(conn, copyOptions);

			bulkCopy.DestinationTableName = $"\"{entityType.GetTableName()}\"" ;

			foreach (var dbProp in dbProps)
			{
				string columnName = dbProp.ColumnName;
				bulkCopy.ColumnMappings.Add(columnName, $"\"{columnName}\"");
			}
			return bulkCopy;
		}

		/// <summary>
		/// <para>zh-cn:将实体集合异步批量写入当前 DbContext 对应的 Oracle 数据表。</para>
		/// <para>en-us:Asynchronously bulk writes an entity collection to the Oracle table mapped by the current DbContext.</para>
		/// </summary>
		/// <typeparam name="TEntity">
		/// <para>zh-cn:需要批量写入的实体类型。</para>
		/// <para>en-us:The entity type to bulk write.</para>
		/// </typeparam>
		/// <param name="dbCtx">
		/// <para>zh-cn:提供实体映射、数据库连接与表结构信息的 DbContext。</para>
		/// <para>en-us:The DbContext that provides entity mapping, database connection, and table metadata.</para>
		/// </param>
		/// <param name="items">
		/// <para>zh-cn:需要写入数据库的实体集合。</para>
		/// <para>en-us:The entity collection to write to the database.</para>
		/// </param>
		/// <param name="copyOptions">
		/// <para>zh-cn:Oracle 批量复制选项。</para>
		/// <para>en-us:The Oracle bulk copy options.</para>
		/// </param>
		/// <param name="cancellationToken">
		/// <para>zh-cn:用于取消打开连接等异步操作的取消标记。</para>
		/// <para>en-us:The cancellation token used to cancel asynchronous operations such as opening the connection.</para>
		/// </param>
		/// <param name="bulkCopyTimeoutInSecond">
		/// <para>zh-cn:批量复制超时时间，单位为秒；为空时使用 OracleBulkCopy 默认值。</para>
		/// <para>en-us:The bulk copy timeout in seconds; when null, the OracleBulkCopy default is used.</para>
		/// </param>
		/// <returns>
		/// <para>zh-cn:表示异步批量写入操作的任务。</para>
		/// <para>en-us:A task that represents the asynchronous bulk write operation.</para>
		/// </returns>
		public static async Task BulkInsertAsync<TEntity>(this DbContext dbCtx,
			IEnumerable<TEntity> items, OracleBulkCopyOptions copyOptions = OracleBulkCopyOptions.Default, CancellationToken cancellationToken = default, int? bulkCopyTimeoutInSecond = null) where TEntity : class
		{
			var conn = dbCtx.Database.GetDbConnection();
			await conn.OpenIfNeededAsync(cancellationToken);
			DataTable dataTable = BulkInsertUtils.BuildDataTable(dbCtx, dbCtx.Set<TEntity>(), items);
			using (OracleBulkCopy bulkCopy = BuildSqlBulkCopy<TEntity>((OracleConnection)conn, dbCtx, copyOptions))
			{
				if (bulkCopyTimeoutInSecond != null)
				{
					bulkCopy.BulkCopyTimeout = bulkCopyTimeoutInSecond.Value;
				}
				bulkCopy.WriteToServer(dataTable);
			}
		}

		/// <summary>
		/// <para>zh-cn:将实体集合同步批量写入当前 DbContext 对应的 Oracle 数据表。</para>
		/// <para>en-us:Synchronously bulk writes an entity collection to the Oracle table mapped by the current DbContext.</para>
		/// </summary>
		/// <typeparam name="TEntity">
		/// <para>zh-cn:需要批量写入的实体类型。</para>
		/// <para>en-us:The entity type to bulk write.</para>
		/// </typeparam>
		/// <param name="dbCtx">
		/// <para>zh-cn:提供实体映射、数据库连接与表结构信息的 DbContext。</para>
		/// <para>en-us:The DbContext that provides entity mapping, database connection, and table metadata.</para>
		/// </param>
		/// <param name="items">
		/// <para>zh-cn:需要写入数据库的实体集合。</para>
		/// <para>en-us:The entity collection to write to the database.</para>
		/// </param>
		/// <param name="copyOptions">
		/// <para>zh-cn:Oracle 批量复制选项。</para>
		/// <para>en-us:The Oracle bulk copy options.</para>
		/// </param>
		/// <param name="cancellationToken">
		/// <para>zh-cn:保留的取消标记参数，用于与异步重载保持一致。</para>
		/// <para>en-us:The reserved cancellation token parameter, kept consistent with the asynchronous overload.</para>
		/// </param>
		/// <param name="bulkCopyTimeoutInSecond">
		/// <para>zh-cn:批量复制超时时间，单位为秒；为空时使用 OracleBulkCopy 默认值。</para>
		/// <para>en-us:The bulk copy timeout in seconds; when null, the OracleBulkCopy default is used.</para>
		/// </param>
		public static void BulkInsert<TEntity>(this DbContext dbCtx,
			IEnumerable<TEntity> items, OracleBulkCopyOptions copyOptions = OracleBulkCopyOptions.Default, CancellationToken cancellationToken = default, int? bulkCopyTimeoutInSecond = null) where TEntity : class
		{
			var conn = dbCtx.Database.GetDbConnection();
			conn.OpenIfNeeded();
			DataTable dataTable = BulkInsertUtils.BuildDataTable(dbCtx, dbCtx.Set<TEntity>(), items);
			using (OracleBulkCopy bulkCopy = BuildSqlBulkCopy<TEntity>((OracleConnection)conn, dbCtx, copyOptions))
			{
				if (bulkCopyTimeoutInSecond != null)
				{
					bulkCopy.BulkCopyTimeout = bulkCopyTimeoutInSecond.Value;
				}
				bulkCopy.WriteToServer(dataTable);
			}
		}
	}
}
