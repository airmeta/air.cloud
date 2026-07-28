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
using Air.Cloud.Core;

namespace air.cloud.account.service.Utils
{

    /// <summary>
    /// <para>zh-cn:验证码工具类</para>
    /// <para>en-us:Captcha code utility class</para>
    /// </summary>
    public static class CaptchaCodeUtil
    {

        /// <summary>
        /// <para>zh-cn: 生成验证码图片的Base64字符串</para>
        /// <para>en-us: Generate a Base64 string of the captcha code image</para>
        /// </summary>
        /// <param name="UKey">
        ///  <para>zh-cn: 用户会话标识，用于存储和验证验证码</para>
        ///  <para>en-us: User session identifier, used to store and validate the captcha code</para>
        /// </param>
        /// <returns>
        ///  <para>zh-cn: 返回验证码图片的Base64字符串</para>
        ///  <para>en-us: Returns the Base64 string of the captcha code image</para>
        /// </returns>
        public static string GenerateCaptchaCode(string UKey)
        {    
            #region  验证码
            var codeHelper = new SkiaSharpValidateCode(6, true);
            string Code = codeHelper.GetVerifyCodeImageBase64(3, 1);
            AppRealization.RedisCache.String.Set($"VerifyCode:{UKey}", codeHelper.VerifyCodeResult, new TimeSpan(0, 3, 0));
            #endregion
            return Code;
        }

        /// <summary>
        /// <para>zh-cn: 验证验证码是否正确</para>
        /// <para>en-us: Validate if the captcha code is correct</para>
        /// </summary>
        /// <param name="UKey">
        ///  <para>zh-cn: 用户会话标识，用于存储和验证验证码</para>
        ///  <para>en-us: User session identifier, used to store and validate the captcha code</para>
        /// </param>
        /// <param name="Code">
        ///  <para>zh-cn: 用户输入的验证码</para>
        ///  <para>en-us: User input captcha code</para>
        /// </param>
        /// <returns>
        ///  <para>zh-cn: 返回验证码验证结果，true表示正确，false表示错误</para>
        ///  <para>en-us: Returns the captcha code validation result, true indicates correct, false indicates incorrect</para>
        /// </returns>
        public static bool ValidateCaptchaCode(string UKey, string Code)
        {
            var cacheCode = AppRealization.RedisCache.String.Get<string>($"VerifyCode:{UKey}");
            if (string.IsNullOrWhiteSpace(cacheCode)) return false;
            return string.Equals(cacheCode, Code, StringComparison.OrdinalIgnoreCase);
        }

    }
}
