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
namespace Air.Cloud.Core.Util
{
    /// <summary>
    /// 身份证证件号码工具类
    /// </summary>
    public static class IdCardUtil
    {
        public static int DivRem(int a, int b, out int result)
        {
            result = a % b;
            return a / b;
        }

        /// <summary>
        /// 校验15位身份证号
        /// </summary>
        /// <param name="idCard"></param>
        /// <returns></returns>
        public static bool CheckIDCard15(string idCard)
        {
            long n = 0;
            if (long.TryParse(idCard, out n) == false || n < Math.Pow(10, 14))
            {
                return false;//数字验证
            }
            string address = "11x22x35x44x53x12x23x36x45x54x13x31x37x46x61x14x32x41x50x62x15x33x42x51x63x21x34x43x52x64x65x71x81x82x91";
            if (address.IndexOf(idCard.Remove(2)) == -1)
            {
                return false;//省份验证
            }
            string birth = idCard.Substring(6, 6).Insert(4, "-").Insert(2, "-");
            DateTime time = new DateTime();
            if (DateTime.TryParse(birth, out time) == false)
            {
                return false;//生日验证
            }
            return true;//符合15位身份证标准
        }

        /// <summary>
        /// 校验18位身份证号
        /// </summary>
        /// <param name="idCard"></param>
        /// <returns></returns>
        public static bool CheckIDCard18(string idCard)
        {
            long n = 0;
            if (long.TryParse(idCard.Remove(17), out n) == false || n < Math.Pow(10, 16) || long.TryParse(idCard.Replace('x', '0').Replace('X', '0'), out n) == false)
            {
                return false;//数字验证
            }
            string address = "11x22x35x44x53x12x23x36x45x54x13x31x37x46x61x14x32x41x50x62x15x33x42x51x63x21x34x43x52x64x65x71x81x82x91";
            if (address.IndexOf(idCard.Remove(2)) == -1)
            {
                return false;//省份验证
            }

            string birth = idCard.Substring(6, 8).Insert(6, "-").Insert(4, "-");
            DateTime time = new DateTime();
            if (DateTime.TryParse(birth, out time) == false)
            {
                return false;//生日验证
            }

            string[] arrVarifyCode = "1,0,x,9,8,7,6,5,4,3,2".Split(',');
            string[] Wi = "7,9,10,5,8,4,2,1,6,3,7,9,10,5,8,4,2".Split(',');
            char[] Ai = idCard.Remove(17).ToCharArray();
            int sum = 0;
            for (int i = 0; i < 17; i++)
            {
                sum += int.Parse(Wi[i]) * int.Parse(Ai[i].ToString());
            }
            int y = -1;
            DivRem(sum, 11, out y);
            if (arrVarifyCode[y] != idCard.Substring(17, 1).ToLower())
            {
                return false;//校验码验证
            }
            return true;//符合GB11643-1999标准
        }

        /// <summary>
        /// 转换15位身份证号码为18位
        /// </summary>
        /// <param name="idCard">15位的身份证</param>
        /// <returns>返回18位的身份证</returns>
        public static string IDCard15To18(string idCard)
        {
            int iS = 0;

            //加权因子常数
            int[] iW = new int[] { 7, 9, 10, 5, 8, 4, 2, 1, 6, 3, 7, 9, 10, 5, 8, 4, 2 };
            //校验码常数
            string LastCode = "10X98765432";
            //新身份证号
            string newIDCard;

            newIDCard = idCard.Substring(0, 6);
            //填在第6位及第7位上填上‘1’，‘9’两个数字
            newIDCard += "19";

            newIDCard += idCard.Substring(6, 9);

            //进行加权求和
            for (int i = 0; i < 17; i++)
            {
                iS += int.Parse(newIDCard.Substring(i, 1)) * iW[i];
            }

            //取模运算，得到模值
            int iY = iS % 11;
            //从LastCode中取得以模为索引号的值，加到身份证的最后一位，即为新身份证号。
            newIDCard += LastCode.Substring(iY, 1);
            return newIDCard;
        }

        /// <summary>
        /// 转换18位身份证号码为15位
        /// </summary>
        /// <param name="idCard18"></param>
        /// <returns></returns>
        public static string IDCard18To15(string idCard18)
        {
            var cardno15 = idCard18.Substring(0, 6) + idCard18.Substring(8, 9);
            return cardno15;
        }

        /// <summary>
        /// 获取年龄、性别
        /// </summary>
        /// <param name="identityCard"></param>
        /// <returns></returns>
        public static BirthdayAgeSex GetBirthdayAgeSex(string identityCard)
        {
            if (string.IsNullOrEmpty(identityCard))
            {
                return null;
            }
            else
            {
                //身份证长度15位一代身份证,18位位二代;其他不合法
                if (identityCard.Length != 15 && identityCard.Length != 18)
                {
                    return null;
                }
            }

            BirthdayAgeSex entity = new BirthdayAgeSex();
            string strSex = string.Empty;

            //处理18位的身份证号码从号码中得到生日和性别代码
            if (identityCard.Length == 18)
            {
                entity.Birthday = identityCard.Substring(6, 4) + "-" + identityCard.Substring(10, 2) + "-" + identityCard.Substring(12, 2);
                strSex = identityCard.Substring(14, 3);
            }
            if (identityCard.Length == 15)
            {
                entity.Birthday = "19" + identityCard.Substring(6, 2) + "-" + identityCard.Substring(8, 2) + "-" + identityCard.Substring(10, 2);
                strSex = identityCard.Substring(12, 3);
            }

            entity.Age = CalculateAge(entity.Birthday);//根据生日计算年龄
            if (int.Parse(strSex) % 2 == 0)//性别代码为偶数是女性奇数为男性
            {
                entity.Sex = "女";
            }
            else
            {
                entity.Sex = "男";
            }
            return entity;
        }
        /// <summary>
        /// 根据出生日期，计算精确的年龄
        /// </summary>
        /// <param name="birthDate">生日</param>
        /// <returns></returns>
        public static int CalculateAge(string birthDay)
        {
            DateTime birthDate = DateTime.Parse(birthDay);
            DateTime nowDateTime = DateTime.Now;
            int age = nowDateTime.Year - birthDate.Year;
            //再考虑月、天的因素
            if (nowDateTime.Month < birthDate.Month || nowDateTime.Month == birthDate.Month && nowDateTime.Day < birthDate.Day)
            {
                age--;
            }
            return age;
        }

        /// <summary>
        /// 定义 生日年龄性别 实体
        /// </summary>
        public class BirthdayAgeSex
        {
            public string Birthday { get; set; }
            public int Age { get; set; }
            public string Sex { get; set; }
        }

    }
}
