
/*
 * Copyright (c) 2024-2030 星曳数据
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 *
 * This file is provided under the Mozilla Public License Version 2.0,
 * and the "NO WARRANTY" clause of the MPL is hereby expressly
 * acknowledged.
 */


/*
 * Copyright (c) 2024-2030 星曳数据
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 *
 * This file is provided under the Mozilla Public License Version 2.0,
 * and the "NO WARRANTY" clause of the MPL is hereby expressly
 * acknowledged.
 */
using Air.Cloud.Core.Modules.AppAspect.Attributes;

namespace Air.Cloud.Core.Modules.AppAspect.Model
{
    /// <summary>
    /// <para>zh-cn:Aspect切入配置元数据</para>
    /// <para>en-us:Aspect config metadata</para>
    /// </summary>
    public struct AspectMetadata
    {
        /// <summary>
        /// 目标类型
        /// </summary>
        public Type AspectTargetType { get; set; }
        /// <summary>
        /// 目标方法
        /// </summary>
        public string MethodName { get; set; }
        /// <summary>
        /// 环绕组
        /// </summary>
        public AspectAttribute[] Aspects { get; set; }
    }
}
