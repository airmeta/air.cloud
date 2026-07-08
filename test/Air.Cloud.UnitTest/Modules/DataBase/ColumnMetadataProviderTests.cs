using Air.Cloud.Core.App;
using Air.Cloud.Core.Standard.DataBase.Locators;
using Air.Cloud.Core.Standard.DataBase.Model;
using Air.Cloud.EntityFrameWork.Core.Contexts;
using Air.Cloud.EntityFrameWork.Core.Entities.Attributes;
using Air.Cloud.EntityFrameWork.Core.Entities.Configures;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.Extensions.DependencyInjection;

using System.Reflection;

namespace Air.Cloud.UnitTest.Modules.DataBase
{
    /// <summary>
    /// <para>zh-cn:列元数据提供器模型构建测试集合。</para>
    /// <para>en-us:Test suite for column metadata providers during model building.</para>
    /// </summary>
    public class ColumnMetadataProviderTests
    {
        /// <summary>
        /// <para>zh-cn:验证注册自定义列元数据提供器后，类型映射和 ColumnTypeAttribute 会写入 EF Core 关系型列类型元数据。</para>
        /// <para>en-us:Verifies that registering a custom column metadata provider writes type mappings and ColumnTypeAttribute values into EF Core relational column type metadata.</para>
        /// </summary>
        [Fact]
        public void Registered_column_metadata_provider_should_apply_type_mapping_and_column_type_attribute()
        {
            var originalRootServices = AppCore.RootServices;
            var originalInternalServices = AppCore.InternalServices;
            var originalCrucialTypes = AppCore.CrucialTypes;
            try
            {
                AppCore.RootServices = new ServiceCollection()
                    .AddSingleton<IColumnMetadataProvider, TestColumnMetadataProvider>()
                    .BuildServiceProvider();
                AppCore.CrucialTypes = new[] { typeof(MetadataEntity) };

                using var context = new MetadataDbContext(
                    new DbContextOptionsBuilder<MetadataDbContext>().Options);
                var modelBuilder = new ModelBuilder(new ConventionSet());
                var entityBuilder = modelBuilder.Entity<MetadataEntity>();
                entityBuilder.Property(entity => entity.CreatedAt);
                entityBuilder.Property(entity => entity.Code);
                entityBuilder.Property(entity => entity.CommonCode);
                ConfigureColumnMetadata(modelBuilder, entityBuilder, context);

                var entityType = modelBuilder.Model.FindEntityType(typeof(MetadataEntity));

                Assert.NotNull(entityType);
                Assert.Equal(
                    "timestamp without time zone",
                    GetProperty(entityType!, nameof(MetadataEntity.CreatedAt)).GetColumnType());
                Assert.Equal(
                    "varchar(64)",
                    GetProperty(entityType!, nameof(MetadataEntity.Code)).GetColumnType());
                Assert.Equal(
                    "varchar(255)",
                    GetProperty(entityType!, nameof(MetadataEntity.CommonCode)).GetColumnType());
            }
            finally
            {
                AppCore.RootServices = originalRootServices;
                AppCore.InternalServices = originalInternalServices;
                AppCore.CrucialTypes = originalCrucialTypes;
            }
        }

        /// <summary>
        /// <para>zh-cn:通过反射调用框架内部的列元数据配置入口，避免测试依赖真实数据库 Provider。</para>
        /// <para>en-us:Invokes the framework's internal column metadata configuration entry point by reflection so the test does not depend on a real database provider.</para>
        /// </summary>
        /// <param name="modelBuilder">
        /// <para>zh-cn:测试使用的模型构建器。</para>
        /// <para>en-us:The model builder used by the test.</para>
        /// </param>
        /// <param name="entityBuilder">
        /// <para>zh-cn:测试使用的实体构建器。</para>
        /// <para>en-us:The entity builder used by the test.</para>
        /// </param>
        /// <param name="dbContext">
        /// <para>zh-cn:测试使用的 DbContext。</para>
        /// <para>en-us:The DbContext used by the test.</para>
        /// </param>
        private static void ConfigureColumnMetadata(
            ModelBuilder modelBuilder,
            Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder entityBuilder,
            DbContext dbContext)
        {
            var builderType = typeof(AppDbContext<>)
                .Assembly
                .GetType("Air.Cloud.EntityFrameWork.Core.Contexts.Builders.AppDbContextBuilder");
            Assert.NotNull(builderType);

            var method = builderType!.GetMethod(
                "ConfigureColumnMetadata",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(method);

            method!.Invoke(null, new object[] { modelBuilder, entityBuilder, dbContext, typeof(MasterDbContextLocator) });
        }

        /// <summary>
        /// <para>zh-cn:按名称查找实体属性元数据，未找到时抛出测试失败异常。</para>
        /// <para>en-us:Finds entity property metadata by name and fails the test when it is missing.</para>
        /// </summary>
        /// <param name="entityType">
        /// <para>zh-cn:实体类型元数据。</para>
        /// <para>en-us:The entity type metadata.</para>
        /// </param>
        /// <param name="propertyName">
        /// <para>zh-cn:属性名称。</para>
        /// <para>en-us:The property name.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:匹配的属性元数据。</para>
        /// <para>en-us:The matched property metadata.</para>
        /// </returns>
        private static IReadOnlyProperty GetProperty(IReadOnlyEntityType entityType, string propertyName)
        {
            var property = entityType.FindProperty(propertyName);
            Assert.NotNull(property);
            return property!;
        }

        /// <summary>
        /// <para>zh-cn:用于测试列元数据构建的 DbContext。</para>
        /// <para>en-us:DbContext used to test column metadata model building.</para>
        /// </summary>
        private sealed class MetadataDbContext : AppDbContext<MetadataDbContext>
        {
            /// <summary>
            /// <para>zh-cn:创建测试 DbContext。</para>
            /// <para>en-us:Creates the test DbContext.</para>
            /// </summary>
            /// <param name="options">
            /// <para>zh-cn:DbContext 选项。</para>
            /// <para>en-us:The DbContext options.</para>
            /// </param>
            public MetadataDbContext(DbContextOptions<MetadataDbContext> options)
                : base(options)
            {
            }
        }

        /// <summary>
        /// <para>zh-cn:测试实体。</para>
        /// <para>en-us:Test entity.</para>
        /// </summary>
        private sealed class MetadataEntity : IEntity
        {
            /// <inheritdoc />
            public string Id { get; set; } = Guid.NewGuid().ToString("N");

            /// <summary>
            /// <para>zh-cn:创建时间，使用提供器类型映射。</para>
            /// <para>en-us:Creation time that uses provider type mapping.</para>
            /// </summary>
            public DateTime CreatedAt { get; set; }

            /// <summary>
            /// <para>zh-cn:编码，使用 ColumnTypeAttribute 覆盖类型映射。</para>
            /// <para>en-us:Code that uses ColumnTypeAttribute to override type mapping.</para>
            /// </summary>
            [ColumnType(typeof(string), "varchar(64)")]
            public string? Code { get; set; }

            /// <summary>
            /// <para>zh-cn:通用编码，使用 ColumnTypeAttribute 指定 CLR 类型后走公共类型映射。</para>
            /// <para>en-us:Common code that uses ColumnTypeAttribute to specify the CLR type and then follows common type mapping.</para>
            /// </summary>
            [ColumnType(typeof(string))]
            public string? CommonCode { get; set; }

            /// <inheritdoc />
            public DateTime CreateTime { get; set; }

            /// <inheritdoc />
            public DateTime UpdateTime { get; set; }

            /// <inheritdoc />
            public bool Deleted { get; set; }
        }

        /// <summary>
        /// <para>zh-cn:测试用列元数据提供器。</para>
        /// <para>en-us:Column metadata provider used by tests.</para>
        /// </summary>
        private sealed class TestColumnMetadataProvider : DefaultColumnMetadataProvider
        {
            /// <inheritdoc />
            protected override IReadOnlyDictionary<Type, string> TypeMappings { get; } = new Dictionary<Type, string>
            {
                [typeof(DateTime)] = "timestamp without time zone",
                [typeof(string)] = "varchar(255)"
            };
        }
    }
}
