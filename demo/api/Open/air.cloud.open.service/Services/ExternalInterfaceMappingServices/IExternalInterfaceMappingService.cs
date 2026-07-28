using air.cloud.open.model.Dtos.ExternalInterfaceMappingDtos;
using air.cloud.security.common.Base.Dtos;
using air.cloud.security.common.Model;

using Air.Cloud.Core.Standard.DynamicServer;

namespace air.cloud.open.service.Services.ExternalInterfaceMappingServices
{
    public interface IExternalInterfaceMappingService:IDynamicService,ITransient
    {
        /// <summary>
        /// <para>zh-cn:创建外部接口映射</para>
        /// <para>en-us:Create external interface mapping</para>
        /// </summary>
        /// <param name="dto">
        ///   <para>zh-cn:外部接口映射数据传输对象</para>
        ///   <para>en-us:External interface mapping data transfer object</para>
        /// </param>
        /// <returns></returns>
        public Task<string> SaveExternalInterfaceAsync(ExternalInterfaceMappingSDto dto);

        /// <summary>
        /// <para>zh-cn:删除外部接口映射</para>
        /// <para>en-us:Delete external interface mapping</para>
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        public Task<bool> DeleteExternalInterfaceAsync(string Id);

        /// <summary>
        /// <para>zh-cn:获取外部接口映射详情</para>
        /// <para>en-us:Get external interface mapping details</para>
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        public Task<ExternalInterfaceMappingSDto> GetExternalInterfaceAsync(string Id);

        /// <summary>
        /// <para>zh-cn:获取外部接口映射分页列表</para>
        /// <para>en-us:Get external interface mapping page list</para>
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        public Task<PageList<ExternalInterfaceMappingSDto>> GetExternalInterfacePageListAsync(ExternalInterfaceMappingQDto dto);


        /// <summary>
        /// <para>zh-cn:获取外部接口映射选择列表</para>
        /// <para>en-us:Get external interface mapping select list</para>
        /// </summary>
        /// <param name="baseQDto"></param>
        /// <returns></returns>
        public Task<IList<ExternalInterfaceMappingSDto>> GetExternalInterfaceSelectAsync(ExternalInterfaceMappingQDto baseQDto);

    }
}
