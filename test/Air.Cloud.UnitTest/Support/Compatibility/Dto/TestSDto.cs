namespace Air.Cloud.UnitTest.Compatibility.Dto
{
    /// <summary>
    /// <para>zh-cn:兼容旧测试服务签名的测试 DTO。</para>
    /// <para>en-us:Test DTO compatible with the legacy test service signatures.</para>
    /// </summary>
    public sealed class TestSDto
    {
        /// <summary>
        /// <para>zh-cn:标识。</para>
        /// <para>en-us:Identifier value.</para>
        /// </summary>
        public string? Id { get; set; }

        /// <summary>
        /// <para>zh-cn:用户标识。</para>
        /// <para>en-us:User identifier.</para>
        /// </summary>
        public string? UserId { get; set; }

        /// <summary>
        /// <para>zh-cn:服务编码。</para>
        /// <para>en-us:Service number.</para>
        /// </summary>
        public string? ServiceNo { get; set; }

        /// <summary>
        /// <para>zh-cn:到期时间。</para>
        /// <para>en-us:Expiration time.</para>
        /// </summary>
        public DateTime LoseTime { get; set; }
    }
}
