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
using SkiaSharp;

using System.Text;

namespace air.cloud.account.service.Utils
{
    /// <summary>
    /// 验证码生成类
    /// </summary>
    internal class SkiaSharpValidateCode
    {
        private const int CaptchaFontSize = 24;
        private const int CaptchaHeight = 40;
        private const int CaptchaWidth = 132;

        private Random ObjRandom = new Random();

        private SKFont CreateFont() => new(SKFontManager.Default.CreateTypeface(File.Open("Assets/Cascadia-Code-Regular-2.ttf", FileMode.Open, FileAccess.Read)), SetFontSize);
        #region setting

        /// <summary>
        /// //验证码长度
        /// </summary>
        public int SetLength { get; set; } = 4;
        /// <summary>
        /// 验证码字符串
        /// </summary>
        public string SetVerifyCodeText { get; set; }
        /// <summary>
        /// 是否加入小写字母
        /// </summary>
        public bool SetAddLowerLetter { get; set; } = true;
        /// <summary>
        /// 是否加入大写字母
        /// </summary>
        public bool SetAddUpperLetter { get; set; } = true;
        /// <summary>
        /// 字体大小
        /// </summary>
        public int SetFontSize = CaptchaFontSize;
        /// <summary>
        ///  //字体颜色
        /// </summary>
        public SKColor SetFontColor { get; set; } = SKColors.Blue;

        /// <summary>
        /// 字体类型
        /// </summary>
        public string SetFontFamily = "Verdana";
        /// <summary>
        /// 背景色
        /// </summary>
        public SKColor SetBackgroundColor { get; set; } = SKColors.AliceBlue;
        /// <summary>
        /// 是否加入背景线
        /// </summary>
        public bool SetIsBackgroundLine { get; set; }

        /// <summary>
        /// 前景噪点数量
        /// </summary>
        public int SetForeNoisePointCount { get; set; } = 2;
        /// <summary>
        /// 随机码的旋转角度
        /// </summary>
        public int SetRandomAngle { get; set; } = 10;

        /// <summary>
        /// 是否随机字体颜色
        /// </summary>
        public bool SetIsRandomColor { get; set; } = true;
        /// <summary>
        /// 图片宽度
        /// </summary>
        public int SetWith { get; set; } = CaptchaWidth;
        /// <summary>
        /// 图片高度
        /// </summary>
        public int SetHeight { get; set; } = CaptchaHeight;

        /// <summary>
        /// 问题验证码答案，适用于运算符
        /// </summary>
        public string VerifyCodeResult { get; private set; }
        #endregion

        #region Constructor Method
        /// <summary>
        /// <para>zh-cn:创建固定 132 x 40 像素的验证码画布。运算题统一生成五个字符，保证固定前端输入框内的字号和布局不随题目变化。</para>
        /// <para>en-us:Creates a fixed 132 x 40 pixel captcha canvas. Arithmetic questions always use five characters so font size and layout remain stable in the fixed frontend field.</para>
        /// </summary>
        /// <param name="length">
        /// <para>zh-cn:非运算验证码的字符数量。</para>
        /// <para>en-us:Character count for a non-arithmetic captcha.</para>
        /// </param>
        /// <param name="isOperation">
        /// <para>zh-cn:是否生成运算题验证码。</para>
        /// <para>en-us:Whether to generate an arithmetic captcha.</para>
        /// </param>
        public SkiaSharpValidateCode(int length = 4, bool isOperation = false)
        {
            if (isOperation)
            {
                var dic = GetQuestion();
                SetVerifyCodeText = dic.Key;
                VerifyCodeResult = dic.Value;
                SetRandomAngle = 0;
            }
            else
            {
                SetLength = length;
                GetVerifyCodeText();
            }
            SetWith = CaptchaWidth;
            SetHeight = CaptchaHeight;
        }
        #endregion

        #region Private Method
        /// <summary>
        /// 得到验证码字符串
        /// </summary>
        private void GetVerifyCodeText()
        {

            //没有外部输入验证码时随机生成
            if (string.IsNullOrEmpty(SetVerifyCodeText))
            {
                StringBuilder objStringBuilder = new StringBuilder();

                //加入数字1-9
                for (int i = 1; i <= 9; i++)
                {
                    objStringBuilder.Append(i.ToString());
                }

                //加入大写字母A-Z，不包括O
                if (SetAddUpperLetter)
                {
                    char temp = ' ';

                    for (int i = 0; i < 26; i++)
                    {
                        temp = Convert.ToChar(i + 65);

                        //如果生成的字母不是'O'
                        if (!temp.Equals('O'))
                        {
                            objStringBuilder.Append(temp);
                        }
                    }
                }

                //加入小写字母a-z，不包括o
                if (SetAddLowerLetter)
                {
                    char temp = ' ';

                    for (int i = 0; i < 26; i++)
                    {
                        temp = Convert.ToChar(i + 97);

                        //如果生成的字母不是'o'
                        if (!temp.Equals('o'))
                        {
                            objStringBuilder.Append(temp);
                        }
                    }
                }

                //生成验证码字符串
                {
                    int index = 0;

                    for (int i = 0; i < SetLength; i++)
                    {
                        index = ObjRandom.Next(0, objStringBuilder.Length);

                        SetVerifyCodeText += objStringBuilder[index];

                        objStringBuilder.Remove(index, 1);
                    }
                }
            }
        }

        /// <summary>
        /// 获取随机颜色
        /// </summary>
        /// <returns></returns>
        private SKColor GetRandomColor()
        {
            Random RandomNum_First = new Random((int)DateTime.Now.Ticks);
            // 对于C#的随机数，没什么好说的
            Thread.Sleep(RandomNum_First.Next(50));
            Random RandomNum_Sencond = new Random((int)DateTime.Now.Ticks);
            // 为了在白色背景上显示，尽量生成深色
            int int_Red = RandomNum_First.Next(256);
            int int_Green = RandomNum_Sencond.Next(256);
            int int_Blue = (int_Red + int_Green > 400) ? 0 : 400 - int_Red - int_Green;
            int_Blue = (int_Blue > 255) ? 255 : int_Blue;
            return SKColor.FromHsv(int_Red, int_Green, int_Blue);
        }
        #endregion

        #region Public Method
        /// <summary>
        /// <para>zh-cn:生成固定五个字符的单数字运算题，避免除法被生成成双数字被除数而改变验证码画布中的排版。</para>
        /// <para>en-us:Generates a five-character single-digit arithmetic question, preventing division from using a two-digit dividend that would change the captcha layout.</para>
        /// </summary>
        /// <param name="questionList">
        /// <para>zh-cn:可选的自定义题目和答案映射。</para>
        /// <para>en-us:Optional custom question-to-answer mapping.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:用于绘制和校验的一组题目与答案。</para>
        /// <para>en-us:A question and answer pair used for rendering and validation.</para>
        /// </returns>
        public KeyValuePair<string, string> GetQuestion(Dictionary<string, string> questionList = null)
        {
            if (questionList == null)
            {
                questionList = new Dictionary<string, string>();
                var operArray = new string[] { "+", "*", "-", "/" };
                var left = ObjRandom.Next(1, 10);
                var right = ObjRandom.Next(1, 10);
                var oper = operArray[ObjRandom.Next(0, operArray.Length)];
                string key = string.Empty, val = string.Empty;
                switch (oper)
                {
                    case "+":
                        key = string.Format("{0}+{1}=?", left, right);
                        val = (left + right).ToString();
                        questionList.Add(key, val);
                        break;
                    case "*":
                        key = string.Format("{0}×{1}=?", left, right);
                        val = (left * right).ToString();
                        questionList.Add(key, val);
                        break;
                    case "-":
                        if (left < right)
                        {
                            var intTemp = left;
                            left = right;
                            right = intTemp;
                        }
                        questionList.Add(left + "-" + right + "=?", (left - right).ToString());
                        break;
                    case "/":
                        var quotient = ObjRandom.Next(1, 10);
                        right = ObjRandom.Next(1, (9 / quotient) + 1);
                        left = right * quotient;
                        questionList.Add(left + "÷" + right + "=?", quotient.ToString());
                        break;
                }
            }
            return questionList.ToList()[ObjRandom.Next(0, questionList.Count)];
        }



        #endregion

        #region newCode


        /// <summary>
        /// 干扰线的颜色集合
        /// </summary>
        public static List<SKColor> colors { get; set; }
        static SkiaSharpValidateCode()
        {
            colors = new List<SKColor>
        {
            SKColors.AliceBlue,
            SKColors.PaleGreen,
            SKColors.PaleGoldenrod,
            SKColors.Orchid,
            SKColors.OrangeRed,
            SKColors.Orange,
            SKColors.OliveDrab,
            SKColors.Olive,
            SKColors.OldLace,
            SKColors.Navy,
            SKColors.NavajoWhite,
            SKColors.Moccasin,
            SKColors.MistyRose,
            SKColors.MintCream,
            SKColors.MidnightBlue,
            SKColors.MediumVioletRed,
            SKColors.MediumTurquoise,
            SKColors.MediumSpringGreen,
            SKColors.LightSlateGray,
            SKColors.LightSteelBlue,
            SKColors.LightYellow,
            SKColors.Lime,
            SKColors.LimeGreen,
            SKColors.Linen,
            SKColors.PaleTurquoise,
            SKColors.Magenta,
            SKColors.MediumAquamarine,
            SKColors.MediumBlue,
            SKColors.MediumOrchid,
            SKColors.MediumPurple,
            SKColors.MediumSeaGreen,
            SKColors.MediumSlateBlue,
            SKColors.Maroon,
            SKColors.PaleVioletRed,
            SKColors.PapayaWhip,
            SKColors.PeachPuff,
            SKColors.Snow,
            SKColors.SpringGreen,
            SKColors.SteelBlue,
            SKColors.Tan,
            SKColors.Teal,
            SKColors.Thistle,
            SKColors.SlateGray,
            SKColors.Tomato,
            SKColors.Violet,
            SKColors.Wheat,
            SKColors.LightSkyBlue,
            SKColors.SlateBlue,
            SKColors.Silver,
            SKColors.Peru,
            SKColors.Pink,
            SKColors.Plum,
            SKColors.PowderBlue,
            SKColors.Purple,
            SKColors.Red,
            SKColors.SkyBlue,
            SKColors.RosyBrown,
            SKColors.SaddleBrown,
            SKColors.Salmon,
            SKColors.SandyBrown,
            SKColors.SeaGreen,
            SKColors.SeaShell,
            SKColors.Sienna,
            SKColors.RoyalBlue,
            SKColors.LightSeaGreen,
            SKColors.LightSalmon,
            SKColors.LightPink,
            SKColors.Crimson,
            SKColors.Cyan,
            SKColors.DarkBlue,
            SKColors.DarkCyan,
            SKColors.DarkGoldenrod,
            SKColors.DarkGray,
            SKColors.Cornsilk,
            SKColors.DarkGreen,
            SKColors.DarkMagenta,
            SKColors.DarkOliveGreen,
            SKColors.DarkOrange,
            SKColors.DarkOrchid,
            SKColors.DarkRed,
            SKColors.DarkSalmon,
            SKColors.DarkKhaki,
            SKColors.DarkSeaGreen,
            SKColors.CornflowerBlue,
            SKColors.Chocolate,
            SKColors.AntiqueWhite,
            SKColors.Aqua,
            SKColors.Aquamarine,
            SKColors.Azure,
            SKColors.Beige,
            SKColors.Bisque,
            SKColors.Coral,
            SKColors.Black,
            SKColors.Blue,
            SKColors.BlueViolet,
            SKColors.Brown,
            SKColors.BurlyWood,
            SKColors.CadetBlue,
            SKColors.Chartreuse,
            SKColors.BlanchedAlmond,
            SKColors.Transparent,
            SKColors.DarkSlateBlue,
            SKColors.DarkTurquoise,
            SKColors.IndianRed,
            SKColors.Indigo,
            SKColors.Ivory,
            SKColors.Khaki,
            SKColors.Lavender,
            SKColors.LavenderBlush,
            SKColors.HotPink,
            SKColors.LawnGreen,
            SKColors.LightBlue,
            SKColors.LightCoral,
            SKColors.LightCyan,
            SKColors.LightGoldenrodYellow,
            SKColors.LightGray,
            SKColors.LightGreen,
            SKColors.LemonChiffon,
            SKColors.DarkSlateGray,
            SKColors.Honeydew,
            SKColors.Green,
            SKColors.DarkViolet,
            SKColors.DeepPink,
            SKColors.DeepSkyBlue,
            SKColors.DimGray,
            SKColors.DodgerBlue,
            SKColors.Firebrick,
            SKColors.GreenYellow,
            SKColors.FloralWhite,
            SKColors.Fuchsia,
            SKColors.Gainsboro,
            SKColors.GhostWhite
        };
        }
        /// <summary>
        /// 创建画笔
        /// </summary>
        /// <param name="color"></param>
        /// <param name="fontSize"></param>
        /// <returns></returns>
        private SKPaint CreatePaint(SKColor color, float fontSize)
        {
            SkiaSharp.SKTypeface font = SKTypeface.FromFamilyName(null, SKFontStyleWeight.SemiBold, SKFontStyleWidth.ExtraCondensed, SKFontStyleSlant.Upright);
            SKPaint paint = new SKPaint();
            paint.IsAntialias = true;
            paint.Color = color;
            paint.Typeface = font;
            paint.TextSize = fontSize;
            return paint;
        }
        /// <summary>
        /// <para>zh-cn:生成固定尺寸验证码 PNG。文字按实际字体度量在画布中央绘制，避免固定前端显示框出现裁切或按题目长度缩放。</para>
        /// <para>en-us:Generates a fixed-size captcha PNG. Text is centered using actual font metrics, avoiding cropping or question-length scaling in the fixed frontend field.</para>
        /// </summary>
        /// <param name="lineNum">
        /// <para>zh-cn:干扰线数量。</para>
        /// <para>en-us:Number of interference lines.</para>
        /// </param>
        /// <param name="lineStrookeWidth">
        /// <para>zh-cn:干扰线宽度。</para>
        /// <para>en-us:Interference line width.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:编码后的验证码 PNG 字节。</para>
        /// <para>en-us:Encoded captcha PNG bytes.</para>
        /// </returns>
        public byte[] GetVerifyCodeImage(int lineNum = 1, int lineStrookeWidth = 1)
        {
            //创建bitmap位图
            using (SKBitmap image2d = new SKBitmap(SetWith, SetHeight, SKColorType.Bgra8888, SKAlphaType.Premul))
            {
                //创建画笔
                using (SKCanvas canvas = new SKCanvas(image2d))
                {
                    //填充背景颜色为白色
                    if (SetIsRandomColor)
                    {
                        SetFontColor = GetRandomColor();
                    }
                    //填充白色背景
                    canvas.Clear(SetBackgroundColor);

                    AddForeNoisePoint(image2d);
                    AddBackgroundNoisePoint(image2d, canvas);

                    using SKFont font = CreateFont();
                    using var drawStyle = new SKPaint();
                    drawStyle.IsAntialias = true;
                    drawStyle.TextSize = SetFontSize;
                    drawStyle.Color = SetFontColor;
                    var metrics = font.Metrics;
                    var px = (SetWith - font.MeasureText(SetVerifyCodeText)) / 2;
                    var py = (SetHeight - metrics.Ascent - metrics.Descent) / 2;
                    canvas.DrawText(SetVerifyCodeText, px, py, font, drawStyle);
                    //画随机干扰线
                    using (SKPaint disturbStyle = new SKPaint())
                    {
                        Random random = new Random();
                        for (int i = 0; i < lineNum; i++)
                        {
                            disturbStyle.Color = colors[random.Next(colors.Count)];
                            disturbStyle.StrokeWidth = lineStrookeWidth;
                            canvas.DrawLine(random.Next(0, SetWith), random.Next(0, SetHeight), random.Next(0, SetWith), random.Next(0, SetHeight), disturbStyle);
                        }
                    }
                    //返回图片byte
                    using (SKImage img = SKImage.FromBitmap(image2d))
                    {
                        using (SKData p = img.Encode(SKEncodedImageFormat.Png, 100))
                        {
                            return p.ToArray();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 获取验证码
        /// </summary>
        /// <param name="captchaText">验证码文字</param>
        /// <param name="width">图片宽度</param>
        /// <param name="height">图片高度</param>
        /// <param name="lineNum">干扰线数量</param>
        /// <param name="lineStrookeWidth">干扰线宽度</param>
        /// <returns></returns>
        public string GetVerifyCodeImageBase64(int lineNum = 1, int lineStrookeWidth = 1)
        {
            return Convert.ToBase64String(GetVerifyCodeImage(lineNum, lineStrookeWidth));
        }

        private void AddForeNoisePoint(SKBitmap objBitmap)
        {
            for (int i = 0; i < objBitmap.Width * SetForeNoisePointCount; i++)
            {
                objBitmap.SetPixel(ObjRandom.Next(objBitmap.Width), ObjRandom.Next(objBitmap.Height), SetFontColor);
            }
        }

        private void AddBackgroundNoisePoint(SKBitmap objBitmap, SKCanvas objGraphics)
        {
            using (SKPaint objPen = CreatePaint(SKColors.Azure, 0))
            {
                for (int i = 0; i < objBitmap.Width * 2; i++)
                {
                    objGraphics.DrawRect(ObjRandom.Next(objBitmap.Width), ObjRandom.Next(objBitmap.Height), 1, 1, objPen);
                }
            }
            if (SetIsBackgroundLine)
            {
                //画图片的背景噪音线
                for (var i = 0; i < 12; i++)
                {
                    var x1 = ObjRandom.Next(objBitmap.Width);
                    var x2 = ObjRandom.Next(objBitmap.Width);
                    var y1 = ObjRandom.Next(objBitmap.Height);
                    var y2 = ObjRandom.Next(objBitmap.Height);

                    objGraphics.DrawLine(x1, y1, x2, y2, CreatePaint(SKColors.Silver, 0));
                }
            }
        }

        #endregion
    }
}
