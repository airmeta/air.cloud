using System.Collections.Generic;

namespace Air.Cloud.Core.Standard.AmazonS3.Options
{
    /// <summary>
    /// <para>zh-cn:Amazon S3 令牌配置（key -> token 信息）</para>
    /// <para>en-us:Amazon S3 token configuration (key -> token info)</para>
    /// </summary>
    public class AmazonS3Options : Dictionary<string, AmazonS3TokenOption>
    {
    }

    /// <summary>
    /// <para>zh-cn:Amazon S3 令牌项</para>
    /// <para>en-us:Amazon S3 token item</para>
    /// </summary>
    public class AmazonS3TokenOption
    {
        /// <summary>
        /// <para>zh-cn:访问令牌</para>
        /// <para>en-us:Access token</para>
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// <para>zh-cn:描述信息</para>
        /// <para>en-us:Description</para>
        /// </summary>
        public string Description { get; set; }
    }
}
