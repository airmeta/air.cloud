using Air.Cloud.Plugins.SpecificationDocument.Extensions.Options;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Air.Cloud.Plugins.SpecificationDocument.Options
{
    /// <summary>
    /// 配置信息
    /// </summary>
    public sealed class SpecificationDocumentInjectConfigureOptions
    {
        /// <summary>
        /// 规范化结果配置
        /// </summary>
        public Action<SpecificationDocumentServiceOptions> SpecificationDocumentService { get; set; }

        /// <summary>
        /// 规范化结果中间件配置
        /// </summary>
        public Action<SpecificationDocumentConfigureOptions> SpecificationDocumentConfigure { get; set; }
    }
}