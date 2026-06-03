using Air.Cloud.Core.App;

using Microsoft.EntityFrameworkCore;

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
        var connectionString = AppConfigurationLoader.InnerConfiguration[$"ConnectionStrings:{connectionStringName}"];
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException($"ConnectionStrings:{connectionStringName} is required when RealDbIntegration:RunReadOnlyOracleQuery is true.");
        }

        var queryUserId = GetRequiredConfiguration("RealDbIntegration:OracleQueryUserId");
        var options = new DbContextOptionsBuilder<OracleReadOnlyDbContext>()
            .UseOracle(connectionString, oracle =>
            {
                oracle.UseOracleSQLCompatibility(OracleSQLCompatibility.DatabaseVersion19);
                oracle.CommandTimeout(15);
            })
            .Options;

        await using var dbContext = new OracleReadOnlyDbContext(options);

        var rows = await dbContext.SysUserServices
            .AsNoTracking()
            .Where(item => item.UserId == queryUserId)
            .Take(1)
            .ToListAsync();

        Assert.NotNull(rows);
        Assert.True(rows.Count <= 1);
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
