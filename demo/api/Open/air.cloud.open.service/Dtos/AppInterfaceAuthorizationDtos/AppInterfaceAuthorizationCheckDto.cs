namespace air.cloud.open.service.Dtos.AppInterfaceAuthorizationDtos
{
    /// <summary>
    /// <para
    /// </summary>
    public  class AppInterfaceAuthorizationCheckDto
    {
        /// <summary>
        /// <para>zh-cn:应用Id</para>
        /// <para>en-us:Application Id</para>
        /// </summary>
        public string AppId { get; set; }

        /// <summary>
        /// <para>zh-cn:接口Id</para>
        /// <para>en-us:Action Id</para>
        /// </summary>
        public string ActionId { get; set; }


        /// <summary>
        /// <para>zh-cn:接口密钥</para>
        /// <para>en-us:Action Secret</para>
        /// </summary>
        public string ActionSecret { get; set; }
    }
}
