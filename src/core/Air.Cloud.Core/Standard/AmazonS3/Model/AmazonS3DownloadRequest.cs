namespace Air.Cloud.Core.Standard.AmazonS3.Model
{

    /// <summary>
    /// <para>zh-cn:下载参数</para>
    /// <para>en-us:Download request</para>
    /// </summary>
    public sealed class AmazonS3DownloadRequest
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
        /// <para>zh-cn:范围下载开始位置（包含）</para>
        /// <para>en-us:Range download start position (inclusive)</para>
        /// </summary>
        public long? RangeStart { get; set; }

        /// <summary>
        /// <para>zh-cn:范围下载结束位置（包含）</para>
        /// <para>en-us:Range download end position (inclusive)</para>
        /// </summary>
        public long? RangeEnd { get; set; }

        /// <summary>
        /// <para>zh-cn:对象版本号</para>
        /// <para>en-us:Object version id</para>
        /// </summary>
        public string VersionId { get; set; }
    }
}
