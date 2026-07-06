using Air.Cloud.EntityFrameWork.Core;
using Air.Cloud.EntityFrameWork.Core.Configure;
using Air.Cloud.EntityFrameWork.Kingbase.Configure;
using Air.Cloud.EntityFrameWork.Kingbase.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Xunit;

namespace Air.Cloud.EntityFrameWork.Kingbase.UnitTest
{
    /// <summary>
    /// <para>zh-cn:KingbaseES V9 数据库配置器和底层 Provider 扩展的单元测试集合。</para>
    /// <para>en-us:Unit test suite for the KingbaseES V9 database configurator and low-level provider extension.</para>
    /// </summary>
    public class KingbaseDatabaseConfigureTests
    {
        private const string ConnectionString = "Server=127.0.0.1;Port=54321;Database=air_cloud;User Id=system;Password=kingbase;";

        /// <summary>
        /// <para>zh-cn:验证 KingbaseDatabaseConfigure 实现 IDatabaseConfigure，并通过统一配置入口注册 Kingbase Provider 与全局迁移程序集。</para>
        /// <para>en-us:Verifies that KingbaseDatabaseConfigure implements IDatabaseConfigure and registers the Kingbase provider plus global migrations assembly through the unified configuration entry point.</para>
        /// </summary>
        [Fact]
        public void Configure_should_apply_provider_and_global_migrations_assembly()
        {
            var originalMigrationAssemblyName = Db.MigrationAssemblyName;
            Db.MigrationAssemblyName = "Air.Cloud.EntityFrameWork.Kingbase.Migrations";
            IDatabaseConfigure configure = new KingbaseDatabaseConfigure();
            var builder = new DbContextOptionsBuilder<TestKingbaseDbContext>();

            try
            {
                var result = configure.Configure<TestKingbaseDbContext>(builder, ConnectionString);

                Assert.Same(builder, result);
                using var context = new TestKingbaseDbContext(builder.Options);
                var relationalOptions = GetRelationalOptions(context);
                Assert.Equal("Air.Cloud.EntityFrameWork.Kingbase.Migrations", relationalOptions.MigrationsAssembly);
                Assert.Contains("Kdbndp", context.Database.ProviderName);
            }
            finally
            {
                Db.MigrationAssemblyName = originalMigrationAssemblyName;
            }
        }

        /// <summary>
        /// <para>zh-cn:验证底层 UseKingbase 扩展仍支持调用方传入迁移程序集，方便高级场景直接使用 EF Core 配置。</para>
        /// <para>en-us:Verifies that the low-level UseKingbase extension still supports caller-supplied migrations assembly for advanced direct EF Core configuration scenarios.</para>
        /// </summary>
        [Fact]
        public void UseKingbase_should_apply_provider_and_custom_migrations_assembly()
        {
            var builder = new DbContextOptionsBuilder<TestKingbaseDbContext>();

            builder.UseKingbase(ConnectionString, "Custom.Migrations");

            using var context = new TestKingbaseDbContext(builder.Options);
            var relationalOptions = GetRelationalOptions(context);
            Assert.Equal("Custom.Migrations", relationalOptions.MigrationsAssembly);
            Assert.Contains("Kdbndp", context.Database.ProviderName);
        }

        /// <summary>
        /// <para>zh-cn:验证 KingbaseDatabaseConfigure 在 builder 为空时抛出 ArgumentNullException，避免空引用延迟到 Provider 内部。</para>
        /// <para>en-us:Verifies that KingbaseDatabaseConfigure throws ArgumentNullException when builder is null so the null reference is not deferred into provider internals.</para>
        /// </summary>
        [Fact]
        public void Configure_should_throw_when_builder_is_null()
        {
            var configure = new KingbaseDatabaseConfigure();
            DbContextOptionsBuilder builder = null!;

            var exception = Assert.Throws<ArgumentNullException>(() =>
                configure.Configure<TestKingbaseDbContext>(builder, ConnectionString));

            Assert.Equal("builder", exception.ParamName);
        }

        /// <summary>
        /// <para>zh-cn:验证 UseKingbase 在 builder 为空时抛出 ArgumentNullException，保持底层扩展的失败模式稳定。</para>
        /// <para>en-us:Verifies that UseKingbase throws ArgumentNullException when builder is null, keeping the low-level extension failure mode stable.</para>
        /// </summary>
        [Fact]
        public void UseKingbase_should_throw_when_builder_is_null()
        {
            DbContextOptionsBuilder builder = null!;

            var exception = Assert.Throws<ArgumentNullException>(() => builder.UseKingbase(ConnectionString));

            Assert.Equal("builder", exception.ParamName);
        }

        private static RelationalOptionsExtension GetRelationalOptions(DbContext context)
        {
            return context.GetService<IDbContextOptions>()
                .Extensions
                .OfType<RelationalOptionsExtension>()
                .Single();
        }

        /// <summary>
        /// <para>zh-cn:用于验证 Kingbase Provider 选项构建的最小 DbContext。</para>
        /// <para>en-us:Minimal DbContext used to verify Kingbase provider option building.</para>
        /// </summary>
        private sealed class TestKingbaseDbContext : DbContext
        {
            /// <summary>
            /// <para>zh-cn:创建测试 DbContext，并接收 Kingbase 配置器生成的选项。</para>
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
