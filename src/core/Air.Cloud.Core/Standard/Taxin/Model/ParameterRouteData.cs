<<<<<<< HEAD
﻿/*
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
namespace Air.Cloud.Core.Standard.Taxin.Model
=======
﻿namespace Air.Cloud.Core.Standard.Taxin.Model
>>>>>>> aeba4aab7dcf969688fd35ab1ea3ac980b15307d
{
    /// <summary>
    /// <para>zh-cn:参数数据</para>
    /// <para>en-us:Parameter data</para>
    /// </summary>
    public class TaxinRouteParameter
    {
        /// <summary>
        /// <para>zh-cn:参数名</para>
        /// <para>en-us:Parameter name</para>
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// <para>zh-cn:参数类型</para>
        /// <para>en-us:Parameter type</para>
        /// </summary>
        public string ParameterType { get; set; }
        /// <summary>
        /// <para>zh-cn:默认值</para>
        /// <para>en-us:Default value</para>
        /// </summary>
        public object DefaultValue { get; set; }
        /// <summary>
        /// <para>zh-cn:参数位置</para>
        /// <para>en-us:Parameter position</para>
        /// </summary>
        public int Position { get; set; }
        /// <summary>
        /// <para>zh-cn:是否可选</para>
        /// <para>en-us:Is optional</para>
        /// </summary>
        public bool IsOptional { get; set; }

    }
}
