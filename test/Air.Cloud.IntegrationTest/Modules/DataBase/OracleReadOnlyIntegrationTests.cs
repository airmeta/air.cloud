using Air.Cloud.Core.App;
using Air.Cloud.EntityFrameWork.Core.Configure;
using Air.Cloud.EntityFrameWork.Core.Internal;
using Air.Cloud.EntityFrameWork.Core.ContextPools;
using Air.Cloud.EntityFrameWork.Core.Contexts.Attributes;
using Air.Cloud.EntityFrameWork.Core.Extensions;
using Air.Cloud.EntityFrameWork.Core.Extensions.DatabaseProvider;
using Air.Cloud.EntityFrameWork.Oracle.Configure;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Oracle.ManagedDataAccess.Client;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Air.Cloud.IntegrationTest.Modules.DataBase;

/// <summary>
/// <para>zh-cn:Oracle 真实连接只读集成测试。</para>
/// <para>en-us:Read-only Oracle integration tests using a real database connection.</para>
/// </summary>
/// <remarks>
/// <para>zh-cn:该测试只在 appsettings.json 中开启 RealDbIntegration:RunReadOnlyOracleQuery 后执行真实查询；默认关闭时直接返回，避免普通测试流程依赖外部数据库。</para>
/// <para>en-us:This test performs a real query only when RealDbIntegration:RunReadOnlyOracleQuery is enabled in appsettings.json; it returns immediately by default so normal test runs do not depend on an external database.</para>
/// </remarks>
public class OracleReadOnlyIntegrationTests
{
    /// <summary>
    /// <para>zh-cn:验证 Oracle Provider 可以通过真实连接对 SYS_USER_SERVICE 执行只读查询。</para>
    /// <para>en-us:Verifies that the Oracle provider can execute a read-only query against SYS_USER_SERVICE through a real connection.</para>
    /// </summary>
    /// <returns>
    /// <para>zh-cn:表示异步测试执行过程。</para>
    /// <para>en-us:Represents the asynchronous test execution.</para>
    /// </returns>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "DataBase")]
    public async Task Oracle_should_query_sys_user_service_read_only()
    {
        if (!IsEnabled())
        {
            return;
        }

        var connectionStringName = GetRequiredConfiguration("RealDbIntegration:OracleConnectionStringName");
        var configuredConnectionString = AppConfigurationLoader.InnerConfiguration[
            $"DataSourceSettings:ConnectionStrings:{connectionStringName}"];
        if (string.IsNullOrWhiteSpace(configuredConnectionString))
        {
            throw new InvalidOperationException($"DataSourceSettings:ConnectionStrings:{connectionStringName} is required when RealDbIntegration:RunReadOnlyOracleQuery is true.");
        }

        var connectionString = DbProvider.GetConnectionString<OracleReadOnlyDbContext>(connectionStringName);
        Assert.Equal(configuredConnectionString, connectionString);

        var queryUserId = GetRequiredConfiguration("RealDbIntegration:OracleQueryUserId");
        var options = new DbContextOptionsBuilder<OracleReadOnlyDbContext>()
            .UseOracle(connectionString, oracle =>
            {
                oracle.UseOracleSQLCompatibility(OracleSQLCompatibility.DatabaseVersion19);
                oracle.CommandTimeout(15);
            })
            .Options;

        await using var dbContext = new OracleReadOnlyDbContext(options);

        // Some supported Oracle versions reject the FETCH clause generated for Take(1).
        // The user-id predicate keeps this validation read-only and narrowly scoped.
        var query = dbContext.SysUserServices
            .AsNoTracking()
            .Where(item => item.UserId == queryUserId);
        var rows = await query.ToListAsync();

        Assert.NotNull(rows);
        Assert.All(rows, row => Assert.Equal(queryUserId, row.UserId));
    }

    /// <summary>
    /// <para>zh-cn:验证真实 Oracle DbContext 可以通过 Provider 类型注册自动解析，而不依赖手写 Provider 字符串。</para>
    /// <para>en-us:Verifies that a real Oracle DbContext is resolved through type-based Provider registration without a handwritten Provider string.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "DataBase")]
    public async Task Oracle_type_based_provider_registration_should_resolve_real_context()
    {
        if (!IsEnabled()) return;

        var connectionStringName = GetRequiredConfiguration("RealDbIntegration:OracleConnectionStringName");
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(AppConfiguration.Configuration);
        services.AddDatabaseConfigure<OracleDatabaseConfigure, OracleDataBaseProvider>();
        services.AddDatabaseAccessor(registration =>
        {
            registration.AddDb<OracleReadOnlyDbContext, OracleIntegrationLocator>(connectionMetadata: connectionStringName);
        });

        await using var serviceProvider = services.BuildServiceProvider();
        await using var dbContext = serviceProvider.GetRequiredService<OracleReadOnlyDbContext>();
        var pool = serviceProvider.GetRequiredService<IDbContextPool>();
        var transactionManager = serviceProvider.GetRequiredService<ILocalTransactionManager>();
        Assert.Same(pool, transactionManager);
        var result = await dbContext.Database.SqlQueryRaw<int>("SELECT 1 AS VALUE FROM DUAL").ToListAsync();

        Assert.Equal(new[] { 1 }, result);
    }

    /// <summary>
    /// <para>zh-cn:验证两个共享同一 Oracle 连接的上下文可以参加同一个本地事务，并在回滚后清理事务绑定。</para>
    /// <para>en-us:Verifies that two contexts sharing one Oracle connection participate in one local transaction and that rollback clears their transaction bindings.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "DataBase")]
    public async Task Oracle_shared_connection_contexts_should_share_and_clear_local_transaction()
    {
        if (!IsEnabled()) return;

        var connectionStringName = GetRequiredConfiguration("RealDbIntegration:OracleConnectionStringName");
        var connectionString = DbProvider.GetConnectionString<OracleReadOnlyDbContext>(connectionStringName);
        await using var connection = new OracleConnection(connectionString);
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<OracleReadOnlyDbContext>()
            .UseOracle(connection, oracle =>
            {
                oracle.UseOracleSQLCompatibility(OracleSQLCompatibility.DatabaseVersion19);
                oracle.CommandTimeout(15);
            })
            .Options;

        await using var first = new OracleReadOnlyDbContext(options);
        await using var second = new OracleReadOnlyDbContext(options);
        var pool = new DbContextPool(null!);
        var transactionManager = (ILocalTransactionManager)pool;
        pool.AddToPool(first);
        pool.AddToPool(second);

        transactionManager.Begin();
        transactionManager.Begin();
        Assert.Equal(2, transactionManager.Depth);
        Assert.NotNull(pool.DbContextTransaction);
        Assert.Same(connection, first.Database.GetDbConnection());
        Assert.Same(connection, second.Database.GetDbConnection());
        Assert.Same(
            pool.DbContextTransaction!.GetDbTransaction(),
            second.Database.CurrentTransaction!.GetDbTransaction());

        var firstResult = await first.Database.SqlQueryRaw<int>("SELECT 1 AS VALUE FROM DUAL").ToListAsync();
        var secondResult = await second.Database.SqlQueryRaw<int>("SELECT 1 AS VALUE FROM DUAL").ToListAsync();
        Assert.Equal(new[] { 1 }, firstResult);
        Assert.Equal(new[] { 1 }, secondResult);

        // The inner scope must not commit or detach the outer transaction.
        transactionManager.Commit();
        Assert.NotNull(pool.DbContextTransaction);
        Assert.NotNull(first.Database.CurrentTransaction);
        Assert.NotNull(second.Database.CurrentTransaction);

        transactionManager.Rollback();
        Assert.Null(pool.DbContextTransaction);
        Assert.Null(first.Database.CurrentTransaction);
        Assert.Null(second.Database.CurrentTransaction);
    }

    /// <summary>
    /// <para>zh-cn:验证同一 Provider 的不同物理连接会维护独立本地事务，并可由上下文池统一回滚清理。</para>
    /// <para>en-us:Verifies that different physical connections for one Provider keep independent local transactions and can be rolled back and cleared by the context pool.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "DataBase")]
    public async Task Oracle_different_connections_should_use_independent_local_transactions()
    {
        if (!IsEnabled()) return;

        var connectionStringName = GetRequiredConfiguration("RealDbIntegration:OracleConnectionStringName");
        var connectionString = DbProvider.GetConnectionString<OracleReadOnlyDbContext>(connectionStringName);
        await using var firstConnection = new OracleConnection(connectionString);
        await using var secondConnection = new OracleConnection(connectionString);
        await firstConnection.OpenAsync();
        await secondConnection.OpenAsync();

        var firstOptions = new DbContextOptionsBuilder<OracleReadOnlyDbContext>()
            .UseOracle(firstConnection, oracle =>
            {
                oracle.UseOracleSQLCompatibility(OracleSQLCompatibility.DatabaseVersion19);
                oracle.CommandTimeout(15);
            })
            .Options;
        var secondOptions = new DbContextOptionsBuilder<OracleReadOnlyDbContext>()
            .UseOracle(secondConnection, oracle =>
            {
                oracle.UseOracleSQLCompatibility(OracleSQLCompatibility.DatabaseVersion19);
                oracle.CommandTimeout(15);
            })
            .Options;

        await using var first = new OracleReadOnlyDbContext(firstOptions);
        await using var second = new OracleReadOnlyDbContext(secondOptions);
        var pool = new DbContextPool(null!);
        pool.AddToPool(first);
        pool.BeginTransaction();

        // 第二个物理连接在外层事务已开启后加入，必须继承现有事务组的嵌套深度。
        pool.AddToPool(second);
        pool.BeginTransaction();

        Assert.NotNull(first.Database.CurrentTransaction);
        Assert.NotNull(second.Database.CurrentTransaction);
        Assert.NotSame(
            first.Database.CurrentTransaction!.GetDbTransaction(),
            second.Database.CurrentTransaction!.GetDbTransaction());

        pool.RollbackTransaction();
        Assert.NotNull(first.Database.CurrentTransaction);
        Assert.NotNull(second.Database.CurrentTransaction);

        pool.RollbackTransaction();

        Assert.Null(first.Database.CurrentTransaction);
        Assert.Null(second.Database.CurrentTransaction);
    }

    /// <summary>
    /// <para>zh-cn:验证同一本地事务中的 EF Core 保存和 EF Core 原生 SQL 更新可以一起回滚。</para>
    /// <para>en-us:Verifies that an EF Core save and an EF Core raw SQL update in one local transaction roll back together.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "DataBase")]
    public async Task Oracle_efcore_save_and_raw_sql_should_rollback_together()
    {
        if (!IsEnabled()) return;

        var connectionStringName = GetRequiredConfiguration("RealDbIntegration:OracleConnectionStringName");
        var connectionString = DbProvider.GetConnectionString<OracleReadOnlyDbContext>(connectionStringName);
        await using var connection = new OracleConnection(connectionString);
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<OracleReadOnlyDbContext>()
            .UseOracle(connection, oracle =>
            {
                oracle.UseOracleSQLCompatibility(OracleSQLCompatibility.DatabaseVersion19);
                oracle.CommandTimeout(15);
            })
            .Options;

        await using var first = new OracleReadOnlyDbContext(options);
        await using var second = new OracleReadOnlyDbContext(options);
        var initialRows = await first.SysUserServices
            .FromSqlRaw("SELECT ID, USER_ID, SERVICE_NO, LOSE_TIME FROM SYS_USER_SERVICE WHERE ROWNUM = 1")
            .AsNoTracking()
            .ToListAsync();
        var original = initialRows.FirstOrDefault();
        Assert.NotNull(original);
        Assert.NotNull(original.Id);
        var originalLoseTime = original.LoseTime;

        var pool = new DbContextPool(null!);
        pool.AddToPool(first);
        pool.AddToPool(second);
        pool.BeginTransaction();

        try
        {
            var trackedRows = await first.SysUserServices
                .Where(item => item.Id == original.Id)
                .ToListAsync();
            var tracked = Assert.Single(trackedRows);
            tracked.LoseTime = originalLoseTime.AddSeconds(2);
            await first.SaveChangesAsync(acceptAllChangesOnSuccess: false);

            var afterEfRows = await second.SysUserServices
                .AsNoTracking()
                .Where(item => item.Id == original.Id)
                .ToListAsync();
            var afterEf = Assert.Single(afterEfRows);
            Assert.True(afterEf.LoseTime > originalLoseTime);

            var affectedRows = await second.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE SYS_USER_SERVICE SET LOSE_TIME = LOSE_TIME + 1/86400 WHERE ID = {original.Id}");
            Assert.Equal(1, affectedRows);

            var afterSqlRows = await second.SysUserServices
                .AsNoTracking()
                .Where(item => item.Id == original.Id)
                .ToListAsync();
            var afterSql = Assert.Single(afterSqlRows);
            Assert.True(afterSql.LoseTime > afterEf.LoseTime);
        }
        finally
        {
            pool.RollbackTransaction();
        }

        await using var verification = new OracleReadOnlyDbContext(options);
        var restoredRows = await verification.SysUserServices
            .AsNoTracking()
            .Where(item => item.Id == original.Id)
            .ToListAsync();
        var restored = Assert.Single(restoredRows);
        Assert.Equal(originalLoseTime, restored.LoseTime);
    }

    /// <summary>
    /// <para>zh-cn:读取集成测试开关。</para>
    /// <para>en-us:Reads the integration-test switch.</para>
    /// </summary>
    /// <returns>
    /// <para>zh-cn:启用真实 Oracle 只读查询时返回 true。</para>
    /// <para>en-us:Returns true when the real Oracle read-only query is enabled.</para>
    /// </returns>
    private static bool IsEnabled()
    {
        return string.Equals(
            AppConfigurationLoader.InnerConfiguration["RealDbIntegration:RunReadOnlyOracleQuery"],
            "true",
            StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// <para>zh-cn:读取必需配置项。</para>
    /// <para>en-us:Reads a required configuration value.</para>
    /// </summary>
    /// <param name="key">
    /// <para>zh-cn:配置键。</para>
    /// <para>en-us:The configuration key.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:配置值。</para>
    /// <para>en-us:The configuration value.</para>
    /// </returns>
    private static string GetRequiredConfiguration(string key)
    {
        var value = AppConfigurationLoader.InnerConfiguration[key];
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"{key} is required.");
        }

        return value;
    }

    /// <summary>
    /// <para>zh-cn:只读 Oracle 查询使用的最小 DbContext。</para>
    /// <para>en-us:Minimal DbContext used by the read-only Oracle query.</para>
    /// </summary>
    private sealed class OracleReadOnlyDbContext : DbContext
    {
        /// <summary>
        /// <para>zh-cn:创建只读 Oracle 查询上下文。</para>
        /// <para>en-us:Creates the read-only Oracle query context.</para>
        /// </summary>
        /// <param name="options">
        /// <para>zh-cn:DbContext 配置项。</para>
        /// <para>en-us:The DbContext options.</para>
        /// </param>
        public OracleReadOnlyDbContext(DbContextOptions<OracleReadOnlyDbContext> options)
            : base(options)
        {
        }

        /// <summary>
        /// <para>zh-cn:SYS_USER_SERVICE 表。</para>
        /// <para>en-us:The SYS_USER_SERVICE table.</para>
        /// </summary>
        public DbSet<SysUserService> SysUserServices => Set<SysUserService>();
    }

    [DatabaseProvider(typeof(OracleDataBaseProvider))]
    private sealed class OracleIntegrationLocator : Air.Cloud.Core.Standard.DataBase.Locators.IDbContextLocator
    {
    }

    /// <summary>
    /// <para>zh-cn:SYS_USER_SERVICE 表的只读映射实体。</para>
    /// <para>en-us:Read-only mapped entity for the SYS_USER_SERVICE table.</para>
    /// </summary>
    [Table("SYS_USER_SERVICE")]
    private sealed class SysUserService
    {
        /// <summary>
        /// <para>zh-cn:主键。</para>
        /// <para>en-us:The primary key.</para>
        /// </summary>
        [Key]
        [Column("ID")]
        public string? Id { get; set; }

        /// <summary>
        /// <para>zh-cn:用户标识。</para>
        /// <para>en-us:The user identifier.</para>
        /// </summary>
        [Column("USER_ID")]
        public string? UserId { get; set; }

        /// <summary>
        /// <para>zh-cn:服务编码。</para>
        /// <para>en-us:The service number.</para>
        /// </summary>
        [Column("SERVICE_NO")]
        public string? ServiceNo { get; set; }

        /// <summary>
        /// <para>zh-cn:到期时间。</para>
        /// <para>en-us:The expiration time.</para>
        /// </summary>
        [Column("LOSE_TIME")]
        public DateTime LoseTime { get; set; }
    }
}
