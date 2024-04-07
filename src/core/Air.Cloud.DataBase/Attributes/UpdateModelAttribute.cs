// Copyright (c) 2020-2022 百小僧, Baiqian Co.,Ltd.
// Furion is licensed under Mulan PSL v2.
// You can use this software according to the terms and conditions of the Mulan PSL v2.
// You may obtain a copy of Mulan PSL v2 at:
//             https://gitee.com/dotnetchina/Furion/blob/master/LICENSE
// THIS SOFTWARE IS PROVIDED ON AN "AS IS" BASIS, WITHOUT WARRANTIES OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO NON-INFRINGEMENT, MERCHANTABILITY OR FIT FOR A PARTICULAR PURPOSE.
// See the Mulan PSL v2 for more details.

namespace Air.Cloud.DataBase.Attributes
{
    /// <summary>
    /// 更新模型信息
    /// </summary>
    /// <remarks>
    /// 可复用,支持对于单个模型单个属性使用多次改对象
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property)]
    public class UpdateModelAttribute : Attribute
    {
        /// <summary>
        /// 更新方式 0包括更新 1排除更新
        /// </summary>
        public UpdateMethod Type { get; set; }
        /// <summary>
        /// 标识
        /// </summary>
        public string Key { get; set; } = string.Empty;
    }

    /// <summary>
    /// 更新方式
    /// </summary>
    public enum UpdateMethod
    {
        Include,
        Exclude
    }
}
