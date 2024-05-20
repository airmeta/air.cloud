
/*
 * Copyright (c) 2024 星曳数据
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 *
 * This file is provided under the Mozilla Public License Version 2.0,
 * and the "NO WARRANTY" clause of the MPL is hereby expressly
 * acknowledged.
 */
using Air.Cloud.Plugins.SpecificationDocument.Extensions.Options;

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