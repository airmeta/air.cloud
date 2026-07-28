using air.cloud.open.model.Dtos.InternalInterfaceMappingDtos;
using air.cloud.security.common.Base.Dtos;
using air.cloud.security.common.Model;

using Air.Cloud.Core.Standard.DataBase.Domains;
using Air.Cloud.Core.Standard.DynamicServer;

namespace air.cloud.open.model.Domains
{
    /// <summary>
    /// <para>zh-cn:内部接口映射领域</para>
    /// <para>en-us:Internal interface mapping domain</para>
    /// </summary>
    public interface IInternalInterfaceMappingDomain: IEntityDomain, ITransient
    {

        /// <summary>
        /// <para>zh-cn:创建内部接口映射</para>
        /// <para>en-us:Create internal interface mapping</para>
        /// </summary>
        /// <param name="dto">
        ///   <para>zh-cn:内部接口映射数据传输对象</para>
        ///   <para>en-us:Internal interface mapping data transfer object</para>
        /// </param>
        /// <returns></returns>
        public Task<string> CreateInternalInterfaceAsync(InternalInterfaceMappingSDto dto);

        /// <summary>
        /// <para>zh-cn:更新内部接口映射</para>
        /// <para>en-us:Update internal interface mapping</para>
        /// </summary>
        /// <param name="dto">
        ///   <para>zh-cn:内部接口映射数据传输对象</para>
        ///   <para>en-us:Internal interface mapping data transfer object</para>
        /// </param>
        /// <returns></returns>
        public Task<string> UpdateInternalInterfaceAsync(InternalInterfaceMappingSDto dto);


        /// <summary>
        /// <para>zh-cn:删除内部接口映射</para>
        /// <para>en-us:Delete internal interface mapping</para>
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        public Task<bool> DeleteInternalInterfaceAsync(string Id);

        /// <summary>
        /// <para>zh-cn:获取内部接口映射详情</para>
        /// <para>en-us:Get internal interface mapping details</para>
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        public Task<InternalInterfaceMappingSDto> GetInternalInterfaceAsync(string Id);

        /// <summary>
        /// <para>zh-cn:获取内部接口映射分页列表</para>
        /// <para>en-us:Get internal interface mapping page list</para>
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        public Task<PageList<InternalInterfaceMappingRDto>> GetInternalInterfacePageListAsync(InternalInterfaceMappingQDto dto);
        /// <summary>
        /// <para>zh-cn:获取内部接口映射选择列表</para> 
        /// <para>en-us:Get internal interface mapping select list</para>
        /// </summary>
        /// <param name="baseQDto"></param>
        /// <returns></returns>
        Task<IList<InternalInterfaceMappingSDto>> GetInternalInterfaceSelectAsync(InternalInterfaceMappingQDto baseQDto);
    }
}
