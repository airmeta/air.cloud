using air.cloud.security.common.Base.Dtos;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace air.cloud.open.model.Dtos.AppInterfaceAuthorizationDtos
{
    /// <summary>
    /// <para>zh-cn:应用接口授权查询数据传输对象</para>
    /// <para>en-us:Application Interface Authorization Query Data Transfer Object</para>
    /// </summary>
    public class AppInterfaceAuthorizationQDto:BaseQDto
    {
        /// <summary>
        /// <para>zh-cn:应用Id</para>
        /// <para>en-us:App Id</para>
        /// </summary>
        public string AppId { get; set; }

        /// <summary>
        /// <para>zh-cn:外部接口Id</para>
        /// <para>en-us:External Interface Id</para>
        /// </summary>
        public string ExternalInterfaceId { get; set; }
    }
}
