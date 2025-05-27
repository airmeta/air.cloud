namespace Air.Cloud.DataBase.ElasticSearch.Enums
{
    /// <summary>
    /// <para>zh-cn:索引切分规则</para>
    /// <para>en-us:Index splitting rules</para>
    /// </summary>
    public enum IndexSegmentationPatternEnum
    {
        /// <summary>
        /// <para>zh-cn:不进行切分</para>
        /// <para>en-us:Do not perform segmentation</para>
        /// </summary>
        None,
        /// <summary>
        /// <para>zh-cn:按照年切分</para>
        /// <para>en-us:Segmentation by year</para>
        /// </summary>
        Year,
        /// <summary>
        /// <para>zh-cn:按照月切分</para>
        /// <para>en-us:Segmentation by month</para>
        /// </summary>
        Month,
        /// <summary>
        /// <para>zh-cn:按照天切分</para>
        /// <para>en-us:Segmentation by day</para>
        /// </summary>
        Day
    }
}
