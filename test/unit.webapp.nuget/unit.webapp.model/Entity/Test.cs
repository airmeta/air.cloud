using Air.Cloud.Core.Standard.DataBase.Model;

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace unit.webapp.model.Entity
{
    /// <summary>
    /// 测试类实体
    /// </summary>
    [Table("SYS_USER_SERVICE")]
    public class Test : IEntity
    {
        [Column("ID"), Key]
        public string? Id { get; set; }
        /// <summary>
        /// 用户Id
        /// </summary>
        [Description("用户Id")]
        [Column("USER_ID")]
        public string? UserId { get; set; }

        /// <summary>
        /// 服务编码
        /// </summary>
        [Description("服务编码")]
        [Column("SERVICE_NO")]
        public string? ServiceNo { get; set; }

        /// <summary>
        /// 到期时间
        /// </summary>
        [Description("到期时间")]
        [Column("LOSE_TIME")]
        public DateTime LoseTime { get; set; }


    }
}
