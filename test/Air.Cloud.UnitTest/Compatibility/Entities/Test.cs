using Air.Cloud.Core.Standard.DataBase.Model;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Air.Cloud.UnitTest.Compatibility.Entities
{
    /// <summary>
    /// <para>zh-cn:兼容旧测试领域接口的测试实体。</para>
    /// <para>en-us:Test entity compatible with the legacy domain interface.</para>
    /// </summary>
    [Table("SYS_USER_SERVICE")]
    public class Test : IEntity
    {
        /// <inheritdoc />
        [Column("ID"), Key]
        public string? Id { get; set; }

        /// <summary>
        /// <para>zh-cn:用户标识。</para>
        /// <para>en-us:User identifier.</para>
        /// </summary>
        [Description("用户Id")]
        [Column("USER_ID")]
        public string? UserId { get; set; }

        /// <summary>
        /// <para>zh-cn:服务编码。</para>
        /// <para>en-us:Service number.</para>
        /// </summary>
        [Description("服务编码")]
        [Column("SERVICE_NO")]
        public string? ServiceNo { get; set; }

        /// <summary>
        /// <para>zh-cn:到期时间。</para>
        /// <para>en-us:Expiration time.</para>
        /// </summary>
        [Description("到期时间")]
        [Column("LOSE_TIME")]
        public DateTime LoseTime { get; set; }

        /// <inheritdoc />
        [NotMapped]
        public DateTime CreateTime { get; set; }

        /// <inheritdoc />
        [NotMapped]
        public DateTime UpdateTime { get; set; }

        /// <inheritdoc />
        [NotMapped]
        public bool Deleted { get; set; }
    }
}
