using Air.Cloud.EntityFrameWork.Kingbase.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Xunit;

namespace Air.Cloud.EntityFrameWork.Kingbase.UnitTest
{
    /// <summary>
    /// <para>zh-cn:KingbaseES V9 EF Core 适配器的单元测试集合。</para>
    /// <para>en-us:Unit test suite for the KingbaseES V9 EF Core adapter.</para>
    /// </summary>
    public class KingbaseDbContextOptionsBuilderExtensionsTests
    {
        /// <summary>
        /// <para>zh-cn:验证 Configure 会注册 Kingbase Provider，并把迁移程序集设置为 Air.Cloud 当前数据库迁移程序集名称。</para>
        /// <para>en-us:Verifies that Configure registers the Kingbase provider and sets the migrations assembly to the current Air.Cloud database migration assembly name.</para>
        /// </summary>
        [Fact]
        public void UseAirCloudKingbase_should_apply_kingbase_provider_and_migrations_assembly()
        {
            const string connectionString = "Server=127.0.0.1;Port=54321;Database=air_cloud;User Id=system;Password=kingbase;";
            var builder = new DbContextOptionsBuilder<TestKingbaseDbContext>();

            builder.UseKingbase(connectionString, "Air.Cloud.EntityFrameWork.Kingbase.Migrations");

            using var context = new TestKingbaseDbContext(builder.Options);
            var relationalOptions = context.GetService<IDbContextOptions>()
                .Extensions
                .OfType<RelationalOptionsExtension>()
                .Single();

            Assert.Equal("Air.Cloud.EntityFrameWork.Kingbase.Migrations", relationalOptions.MigrationsAssembly);
            Assert.Contains("Kdbndp", context.Database.ProviderName);
        }

        /// <summary>
        /// <para>zh-cn:验证 Kingbase 配置器可以通过 IDatabaseConfigure 抽象被调用，保持与数据库框架注册入口兼容。</para>
        /// <para>en-us:Verifies that the Kingbase configurator can be invoked through the IDatabaseConfigure abstraction, preserving compatibility with the database framework registration entry.</para>
        /// </summary>
        [Fact]
        public void UseAirCloudKingbase_should_return_same_builder_for_chained_configuration()
        {
            var builder = new DbContextOptionsBuilder<TestKingbaseDbContext>();

            var result = builder.UseKingbase(
                "Server=127.0.0.1;Port=54321;Database=air_cloud;User Id=system;Password=kingbase;");

            Assert.Same(builder, result);
            using var context = new TestKingbaseDbContext(builder.Options);
            Assert.Contains("Kdbndp", context.Database.ProviderName);
        }

        /// <summary>
        /// <para>zh-cn:用于验证 Kingbase Provider 选项构建的最小 DbContext。</para>
        /// <para>en-us:Minimal DbContext used to verify Kingbase provider option building.</para>
        /// </summary>
        private sealed class TestKingbaseDbContext : DbContext
        {
            /// <summary>
            /// <para>zh-cn:创建测试 DbContext，并接收由 Kingbase 配置器生成的选项。</para>
            /// <para>en-us:Creates the test DbContext and receives options produced by the Kingbase configurator.</para>
            /// </summary>
            /// <param name="options">
            /// <para>zh-cn:DbContext 运行所需的配置选项。</para>
            /// <para>en-us:The options required by the DbContext runtime.</para>
            /// </param>
            public TestKingbaseDbContext(DbContextOptions<TestKingbaseDbContext> options)
                : base(options)
            {
            }
        }
    }
}
