using System.ComponentModel.DataAnnotations.Schema;

namespace air.cloud.open.model.Entitys.AssociationEntitys
{
    [Table("APP_ROUTE")]
    public  class AppRoute
    {
        /// <summary>
        /// <para>zh-cn:唯一标识</para>
        /// <para>en-us:Id</para>
        /// </summary>
        [Column("ID")]
        public string Id { get; set; }

        /// <summary>
        /// 应用编号
        /// </summary>
        [Column("APP_ID")]
        public string AppId { get; set; }
        /// <summary>
        /// 路由地址
        /// </summary>
        [Column("ROUTE")]
        public string Route { get; set; }

    }
}
