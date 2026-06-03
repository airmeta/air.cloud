using Air.Cloud.Core.Plugins.Security.MD5;
using Air.Cloud.Core.Standard.DataBase.Model;
using Air.Cloud.Core.Standard.DataBase.Options;

namespace Air.Cloud.UnitTest.Core.Standard
{
    /// <summary>
    /// <para>zh-cn:数据库标准模型与配置约定的单元测试。</para>
    /// <para>en-us:Unit tests for database standard models and configuration conventions.</para>
    /// </summary>
    public class DataBaseStandardTests
    {
        /// <summary>
        /// <para>zh-cn:验证数据库配置唯一标识由 Key 稳定计算，供连接池和仓储定位使用。</para>
        /// <para>en-us:Verifies the database option UID is calculated stably from Key for connection-pool and repository lookup.</para>
        /// </summary>
        [Fact]
        public void DataBaseOption_uid_should_be_md5_of_key()
        {
            var option = new DataBaseOption
            {
                Key = "default-sql"
            };

            Assert.Equal(MD5Encryption.GetMd5By32("default-sql"), option.UID);
            Assert.Equal(32, option.UID.Length);
        }

        /// <summary>
        /// <para>zh-cn:验证数据库配置默认类型为关系型数据库，避免未配置 Type 时被误认为 NoSQL。</para>
        /// <para>en-us:Verifies the default database option type is relational so missing Type is not treated as NoSQL.</para>
        /// </summary>
        [Fact]
        public void DataBaseOption_should_default_to_sql_type()
        {
            var option = new DataBaseOption();

            Assert.Equal(DataBaseOptions.关系型, option.Type);
        }

        /// <summary>
        /// <para>zh-cn:验证数据库类型常量保持当前约定值，避免配置文件与代码约定漂移。</para>
        /// <para>en-us:Verifies database type constants keep current contract values so configuration files and code do not drift.</para>
        /// </summary>
        [Fact]
        public void DataBaseOptions_type_constants_should_match_configuration_contract()
        {
            Assert.Equal("SQL", DataBaseOptions.关系型);
            Assert.Equal("NOSQL", DataBaseOptions.非关系型);
        }

        /// <summary>
        /// <para>zh-cn:验证 NoSQL 实体标准要求实体同时具备公开 Id 与私有实体标记。</para>
        /// <para>en-us:Verifies the NoSQL entity standard requires a public Id and the private entity marker.</para>
        /// </summary>
        [Fact]
        public void NoSqlEntity_should_expose_id_and_private_marker()
        {
            var document = new TestNoSqlEntity
            {
                Id = "doc-1"
            };

            Assert.Equal("doc-1", document.Id);
            Assert.IsAssignableFrom<INoSqlEntity>(document);
            Assert.IsAssignableFrom<IPrivateNoSqlEntity>(document);
        }

        private sealed class TestNoSqlEntity : INoSqlEntity
        {
            public string Id { get; set; } = string.Empty;
        }
    }
}
