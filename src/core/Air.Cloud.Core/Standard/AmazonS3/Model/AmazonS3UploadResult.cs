namespace Air.Cloud.Core.Standard.AmazonS3.Model
{
    /// <summary>
    /// <para>zh-cn:上传结果</para>
    /// <para>en-us:Upload result</para>
    /// </summary>
    public sealed class AmazonS3UploadResult
    {
        /// <summary>
        /// <para>zh-cn:Bucket 名称</para>
        /// <para>en-us:Bucket name</para>
        /// </summary>
        public string BucketName { get; set; }

        /// <summary>
        /// <para>zh-cn:对象键</para>
        /// <para>en-us:Object key</para>
        /// </summary>
        public string ObjectKey { get; set; }

        /// <summary>
        /// <para>zh-cn:对象 ETag</para>
        /// <para>en-us:Object ETag</para>
        /// </summary>
        public string ETag { get; set; }

        /// <summary>
        /// <para>zh-cn:对象版本号</para>
        /// <para>en-us:Object version id</para>
        /// </summary>
        public string VersionId { get; set; }

        /// <summary>
        /// <para>zh-cn:请求标识</para>
        /// <para>en-us:Request id</para>
        /// </summary>
        public string RequestId { get; set; }

        /// <summary>
        /// <para>zh-cn:对象访问地址</para>
        /// <para>en-us:Object URL</para>
        /// </summary>
        public string ObjectUrl { get; set; }
    }
}
