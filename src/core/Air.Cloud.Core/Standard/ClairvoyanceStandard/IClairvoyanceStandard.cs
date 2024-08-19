using Air.Cloud.Core.Standard.ClairvoyanceStandard.Options;

namespace Air.Cloud.Core.Standard.ClairvoyanceStandard
{
    public  interface IClairvoyanceStandard:IStandard 
    {
        public T Create<T>(IClairvoyanceOptions clairvoyanceOptions) where T : class;

        /// <summary>
        /// <para>zh-cn:上线操作</para>
        /// <para>en-us:OnLine</para>
        /// </summary>
        /// <returns></returns>
        public Task OnLineAsync();
        /// <summary>
        /// <para>zh-cn:下线操作</para>
        /// <para>en-us:UnderLine</para>
        /// </summary>
        /// <returns></returns>
        public Task UnderLineAsync();
    }
}
