namespace Air.Cloud.Core.Standard.AmazonS3.Model
{
    /// <summary>
    ///  <para>zh-cn:Amazon S3 对象信息</para>
    ///  <para>en-us:Amazon S3 object info</para>
    /// </summary>
    public sealed class AmazonS3ObjectInfo
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
        /// <para>zh-cn:对象大小（字节）</para>
        /// <para>en-us:Object size (bytes)</para>
        /// </summary>
        public long Size { get; set; }

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
        /// <para>zh-cn:内容类型</para>
        /// <para>en-us:Content type</para>
        /// </summary>
        public string ContentType { get; set; }

        /// <summary>
        /// <para>zh-cn:最后修改时间</para>
        /// <para>en-us:Last modified time</para>
        /// </summary>
        public DateTimeOffset? LastModified { get; set; }

        /// <summary>
        /// <para>zh-cn:自定义元数据</para>
        /// <para>en-us:Custom metadata</para>
        /// </summary>
        public IDictionary<string, string> Metadata { get; set; }
    }
}
