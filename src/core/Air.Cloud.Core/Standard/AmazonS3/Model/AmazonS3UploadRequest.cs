namespace Air.Cloud.Core.Standard.AmazonS3.Model
{
    /// <summary>
    /// <para>zh-cn:上传参数</para>
    /// <para>en-us:Upload request</para>
    /// </summary>
    public sealed class AmazonS3UploadRequest
    {
        /// <summary>
        /// <para>zh-cn:访问键（通过配置映射到 token）</para>
        /// <para>en-us:Access key (mapped to token by configuration)</para>
        /// </summary>
        public string Key { get; set; }

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
        /// <para>zh-cn:上传内容流</para>
        /// <para>en-us:Upload content stream</para>
        /// </summary>
        public Stream Content { get; set; }

        /// <summary>
        /// <para>zh-cn:内容长度</para>
        /// <para>en-us:Content length</para>
        /// </summary>
        public long? ContentLength { get; set; }

        /// <summary>
        /// <para>zh-cn:内容类型</para>
        /// <para>en-us:Content type</para>
        /// </summary>
        public string ContentType { get; set; }

        /// <summary>
        /// <para>zh-cn:访问控制策略</para>
        /// <para>en-us:Access control policy</para>
        /// </summary>
        public string Acl { get; set; }

        /// <summary>
        /// <para>zh-cn:存储类型</para>
        /// <para>en-us:Storage class</para>
        /// </summary>
        public string StorageClass { get; set; }

        /// <summary>
        /// <para>zh-cn:服务端加密方式</para>
        /// <para>en-us:Server-side encryption</para>
        /// </summary>
        public string ServerSideEncryption { get; set; }

        /// <summary>
        /// <para>zh-cn:区域</para>
        /// <para>en-us:Region</para>
        /// </summary>
        public string Region { get; set; }

        /// <summary>
        /// <para>zh-cn:自定义元数据</para>
        /// <para>en-us:Custom metadata</para>
        /// </summary>
        public IDictionary<string, string> Metadata { get; set; }

        /// <summary>
        /// <para>zh-cn:对象标签</para>
        /// <para>en-us:Object tags</para>
        /// </summary>
        public IDictionary<string, string> Tags { get; set; }
    }
}
