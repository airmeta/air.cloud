namespace air.gateway.Options
{
    public sealed class TraceLogSettings
    {
        /// <summary>
        /// 关键词过滤
        /// </summary>
        public IList<string> Filters { get; set; }=new List<string>();
        /// <summary>
        /// 是否启用本地记录
        /// </summary>
        public bool? EnableLocalLog { get; set; } = false;
        /// <summary>
        /// 本地记录目录
        /// </summary>
        public string LocalLogDirectory { get; set; } = "logs";

        internal static object SetDefaultSettings(TraceLogSettings options)
        {
            options.Filters = options.Filters.Count>0?options.Filters:new List<string>();
            options.EnableLocalLog ??= false;
            options.LocalLogDirectory ??= "logs";
            return options;
        }
    }
}
