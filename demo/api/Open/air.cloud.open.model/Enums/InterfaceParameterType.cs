using System.Text.Json.Serialization;

namespace air.cloud.open.model.Enums
{
    /// <summary>
    /// 接口参数类型枚举（适配请求/响应参数、嵌套结构、Stream文件地址绑定）
    /// 【JSON/存储字符串值】：枚举的ToString()值（小写，前后端统一）
    /// 【Stream类型】：专属标识三方对象存储地址，用于后续文件绑定逻辑
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))] // 序列化时转成字符串（而非数字），前后端友好
    public enum InterfaceParameterType
    {
        #region 基础数据类型（适配常规参数，对应C#/JSON基础类型）
        /// <summary>
        /// 字符串类型【string】
        /// 场景：常规文本、编码值、简短标识等
        /// </summary>
        String,
        /// <summary>
        /// 32位整数类型【int】
        /// 场景：ID、数量、状态码等常规数值
        /// </summary>
        Int,
        /// <summary>
        /// 64位整数类型【long】
        /// 场景：大数值ID、时间戳、计数统计等
        /// </summary>
        Long,
        /// <summary>
        /// 单精度浮点类型【float】
        /// 场景：非高精度小数（如简单计算值）
        /// </summary>
        Float,
        /// <summary>
        /// 双精度浮点类型【double】
        /// 场景：高精度小数（如金额、比例、坐标等）
        /// </summary>
        Double,
        /// <summary>
        /// 布尔类型【bool】
        /// 场景：是否必填、是否启用、状态标记等
        /// </summary>
        Bool,
        /// <summary>
        /// 十进制高精度类型【decimal】
        /// 场景：金融金额、税费等要求无精度丢失的数值
        /// </summary>
        Decimal,
        /// <summary>
        /// 日期时间类型【datetime】
        /// 场景：创建时间、更新时间、业务时间等（建议统一格式：yyyy-MM-dd HH:mm:ss）
        /// </summary>
        Datetime,
        /// <summary>
        /// 日期类型【date】
        /// 场景：仅需年月日的业务日期（格式：yyyy-MM-dd）
        /// </summary>
        Date,
        #endregion

        #region 复合/嵌套类型（适配你之前的对象嵌套、数组嵌套结构）
        /// <summary>
        /// 对象类型【object】
        /// 场景：嵌套子对象（如A中的B属性），对应模型的ListItems/MapItems存子参数
        /// </summary>
        Object,
        /// <summary>
        /// 数组类型【array】
        /// 场景：数组/集合参数（如A中的C[]属性、根节点数组），Items存单个数组项结构
        /// </summary>
        Array,
        #endregion

        #region 特殊类型（新增Stream文件类型，适配三方对象存储地址）
        /// <summary>
        /// 流/文件类型【stream】
        /// 场景：三方对象存储文件地址（字符串格式），用于后续文件绑定、下载、预览逻辑
        /// 备注：该类型的参数值（Value/DefaultValue）固定存储对象存储的string地址
        /// </summary>
        Stream
        #endregion
    }
}
