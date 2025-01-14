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
using System.ComponentModel;

namespace Air.Cloud.Core.Extensions
{
    public static partial class Extensions
    {
        /// <summary>
        /// 将指定枚举的内容转换成int集合
        /// </summary>
        /// <param name="valueEnum"></param>
        /// <returns></returns>
        public static List<int> EnumToInts(this Enum valueEnum)
        {
            return Enum.GetValues(valueEnum.GetType()).Cast<int>().ToList();
        }

        public static List<dynamic> EnumToList(this Enum valueEnum)
        {
            List<dynamic> listEnter = new List<dynamic>();
            foreach (int value in Enum.GetValues(valueEnum.GetType()))
                listEnter.Add(new { Id = value, Name = Enum.GetName(valueEnum.GetType(), value) });
            return listEnter;
        }

        public static List<EnumList> EnumToListGener(this Enum valueEnum)
        {
            List<EnumList> listEnter = new List<EnumList>();
            foreach (int value in Enum.GetValues(valueEnum.GetType()))
                listEnter.Add(new EnumList { Id = value, Name = Enum.GetName(valueEnum.GetType(), value) });
            return listEnter;
        }


        /// <summary>
        /// 知道枚举,传入枚举英文,获取描述
        /// </summary>
        /// <param name="value"> </param>
        /// <param name="Name"> </param>
        /// <returns> </returns>
        public static string GetDescription(this Type value, string Name)
        {
            DescriptionAttribute attribute = value.GetField(Name)
                .GetCustomAttributes(typeof(DescriptionAttribute), false)
                .SingleOrDefault() as DescriptionAttribute;
            return attribute == null ? value.ToString() : attribute.Description;
        }
    }

    public class EnumList
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}
