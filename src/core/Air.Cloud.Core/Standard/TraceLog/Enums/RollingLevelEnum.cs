namespace Air.Cloud.Core.Standard.TraceLog.Enums
{
    /// <summary>
    /// <para>zh-cn:滚动级别</para>
    /// <para>en-us:Rolling level</para>
    /// </summary>
    public enum RollingLevelEnum
    {
        /// <summary>
        /// <para>zh-cn:从不滚动</para>
        /// <para>en-us:Never rolling</para>
        /// </summary>
        Never,
        /// <summary>
        /// .<para>zh-cn:按年滚动</para>
        /// .<para>en-us:Roll by year</para>
        /// </summary>
        Year,
        /// <summary>
        /// .<para>zh-cn:按月滚动</para>
        /// .<para>en-us:Roll by month</para>
        /// </summary>
        Month,
        /// <summary>
        /// .<para>zh-cn:按天滚动</para>
        /// .<para>en-us:Roll by day</para>
        /// </summary>
        Day,
        /// <summary>
        /// .<para>zh-cn:按小时滚动</para>
        /// .<para>en-us:Roll by hour</para>
        /// </summary>
        Hour
    }
}
