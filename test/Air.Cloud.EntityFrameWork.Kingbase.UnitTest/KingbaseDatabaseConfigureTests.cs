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
        /// <para>zh-cn:验证 UseKingbase 会注册 Kingbase Provider，并把迁移程序集设置为调用方传入的程序集名称。</para>
        /// <para>en-us:Verifies that UseKingbase registers the Kingbase provider and sets the migrations assembly to the assembly name supplied by the caller.</para>
        /// </summary>
        [Fact]
        public void UseKingbase_should_apply_provider_and_migrations_assembly()
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
        /// <para>zh-cn:验证 UseKingbase 返回同一个构建器实例，便于调用方继续进行 EF Core 链式配置。</para>
        /// <para>en-us:Verifies that UseKingbase returns the same builder instance so callers can continue EF Core chained configuration.</para>
        /// </summary>
        [Fact]
        public void UseKingbase_should_return_same_builder_for_chained_configuration()
        {
            var builder = new DbContextOptionsBuilder<TestKingbaseDbContext>();

            var result = builder.UseKingbase(
                "Server=127.0.0.1;Port=54321;Database=air_cloud;User Id=system;Password=kingbase;");

            Assert.Same(builder, result);
            using var context = new TestKingbaseDbContext(builder.Options);
            Assert.Contains("Kdbndp", context.Database.ProviderName);
        }

        /// <summary>
        /// <para>zh-cn:验证传入空 builder 时抛出 ArgumentNullException，避免空引用被延迟到 Provider 内部。</para>
        /// <para>en-us:Verifies that a null builder throws ArgumentNullException so the null reference is not deferred into provider internals.</para>
        /// </summary>
        [Fact]
        public void UseKingbase_should_throw_when_builder_is_null()
        {
            DbContextOptionsBuilder builder = null!;

            var exception = Assert.Throws<ArgumentNullException>(() => builder.UseKingbase("Server=127.0.0.1;"));

            Assert.Equal("builder", exception.ParamName);
        }

        /// <summary>
        /// <para>zh-cn:用于验证 Kingbase Provider 选项构建的最小 DbContext。</para>
        /// <para>en-us:Minimal DbContext used to verify Kingbase provider option building.</para>
        /// </summary>
        private sealed class TestKingbaseDbContext : DbContext
        {
            /// <summary>
            /// <para>zh-cn:创建测试 DbContext，并接收由 Kingbase 扩展生成的选项。</para>
            /// <para>en-us:Creates the test DbContext and receives options produced by the Kingbase extension.</para>
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
