using air.cloud.open.model.Models;

using System.ComponentModel.DataAnnotations;

namespace air.cloud.open.model.Dtos.InternalInterfaceMappingDtos
{
    public  class InternalInterfaceMappingSDto:IValidatableObject
    {
        /// <summary>
        /// <para>zh-cn:主键ID</para>
        /// <para>en-us:Primary Key ID</para>
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// <para>zh-cn:接口名称</para>
        /// <para>en-us:Interface Name</para>
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// <para>zh-cn:接口路由ID</para>
        /// <para>en-us:Interface Route ID</para>
        /// </summary>
        public string RouteId { get; set; }

        /// <summary>
        /// <para>zh-cn:描述</para>
        /// <para>en-us:Description</para>
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// <para>zh-cn:接口参数</para>
        /// <para>en-us:Interface Parameters</para>
        /// </summary>
        public IList<InterfaceRequestParameter> RequestParameters { get; set; }

        /// <summary>
        /// <para>zh-cn:接口响应参数</para>
        /// <para>en-us:Interface Response Parameters</para>
        /// </summary>
        public IList<InterfaceResponseParameter> ResponseParameters { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrEmpty(Name))
            {
                yield return new ValidationResult("接口名称不能为空", new[] { nameof(Name) });
            }
            if (string.IsNullOrEmpty(RouteId))
            {
                yield return new ValidationResult("接口路由ID不能为空", new[] { nameof(RouteId) });
            }
        }
    }
}
