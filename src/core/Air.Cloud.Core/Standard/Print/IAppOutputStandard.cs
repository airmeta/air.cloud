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
using System.Diagnostics;
using System.Text;

namespace Air.Cloud.Core.Standard.Print
{
    /// <summary>
    /// 应用程序输出标准
    /// </summary>
    public interface IAppOutputStandard : IStandard
    {
        /// <summary>
        /// <para>zh-cn:制表符</para>
        /// <para>en-us:TabCharacter</para>
        /// </summary>
        public static string TabCharacter = " ";
        /// <summary>
        /// 输出对象
        /// </summary>
        /// <param name="title">输出对象说明</param>
        /// <param name="content">对象内容</param>
        /// <param name="level">输出等级</param>
        /// <param name="type">输出类别</param>
        /// <param name="state">显式输出</param>
        /// <param name="AdditionalParams">附加参数</param>
        /// <param name="sourceAssembly">输出来源程序集</param>
        public void Print(string title, string content, AppPrintLevel level = AppPrintLevel.Information, string type = "default", bool state = true, Dictionary<string, object> AdditionalParams = null, string sourceAssembly = null);
        /// <summary>
        /// 输出对象
        /// </summary>
        /// <param name="Content">打印内容</param>
        public void Print(AppPrintInformation Content);
        /// <summary>
        /// 输出对象
        /// </summary>
        /// <typeparam name="T">内容类型 继承自AppPrintInformation</typeparam>
        /// <param name="Content">打印内容</param>
        public void Print<T>(T Content) where T : AppPrintInformation,new();
        /// <summary>
        /// 输出异常
        /// </summary>
        /// <param name="exception">异常信息</param>
        /// <param name="pairs">附加参数</param>
        public void Error(Exception exception, Dictionary<string, object> pairs = default);
    }

    /// <summary>
    /// 默认的输出标准实现
    /// </summary>
    [IgnoreScanning]
    public class DefaultAppOutputDependency : IAppOutputStandard
    {
        /// <inheritdoc/>
        public void Error(Exception exception,Dictionary<string, object> pairs=default)
        {
            Print(new AppPrintInformation()
            {
                Title = "异常拦截",
                State = true,
                AdditionalParams = pairs,
                Content = exception.Message,
                Level = AppPrintLevel.Error
            });
            throw exception;
        }
        /// <inheritdoc/>
        public void Print(AppPrintInformation Content)
        {
            Print<AppPrintInformation>(Content);
        }

        /// <inheritdoc/>
        public void Print<T>(T Content) where T : AppPrintInformation, new()
        {
            if (Content == null || (Content.Level == AppPrintLevel.Debug && !AppEnvironment.IsDevelopment))
            {
                return;
            }

            Console.WriteLine(Content.Level == AppPrintLevel.Error
                ? FormatErrorOutput(Content)
                : FormatSingleLineOutput(Content));
        }

        /// <inheritdoc/>
        public void Print(string title, string content, AppPrintLevel level =AppPrintLevel.Information, string type = AppPrintConstType.DEFAULT_TYPE, bool state = true, Dictionary<string, object> AdditionalParams = null, string sourceAssembly = null)
        {
            Print(new AppPrintInformation()
            {
                State = state,
                Title = title,
                Content = content,
                Level = level,
                Type = type,
                AdditionalParams = AdditionalParams,
                SourceAssembly = sourceAssembly
            });
        }
        /// <summary>
        /// 辅助方法：处理多行文本，给每行添加指定缩进
        /// </summary>
        private static string IndentMultilineText(string text, string indent)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            // 统一换行符（兼容 Windows \r\n 和 Linux \n）
            var lines = text.Replace("\r\n", "\n").Split(new[] { '\n' }, StringSplitOptions.None);

            // 只有一行时直接返回，多行时每行加缩进
            if (lines.Length == 1)
                return lines[0];

            // 多行文本：每行添加缩进，最后合并
            return string.Join($"\n{indent}", lines);
        }

        private static string FormatSingleLineOutput(AppPrintInformation content)
        {
            var stringBuilder = new StringBuilder(256);
            AppendHeader(stringBuilder, content);

            if (HasAdditionalParams(content))
            {
                stringBuilder.Append(IAppOutputStandard.TabCharacter);
                stringBuilder.Append("Params:{");

                bool isFirst = true;
                foreach (var pair in content.AdditionalParams)
                {
                    if (!isFirst)
                    {
                        stringBuilder.Append("; ");
                    }

                    stringBuilder.Append(pair.Key);
                    stringBuilder.Append('=');
                    stringBuilder.Append(ToSingleLine(pair.Value));
                    isFirst = false;
                }

                stringBuilder.Append('}');
            }

            return stringBuilder.ToString();
        }

        private static string FormatErrorOutput(AppPrintInformation content)
        {
            var stringBuilder = new StringBuilder(512);
            AppendHeader(stringBuilder, content);

            if (!HasAdditionalParams(content))
            {
                return stringBuilder.ToString();
            }

            stringBuilder.Append(Environment.NewLine);
            stringBuilder.Append("    Exception:");
            stringBuilder.Append(Environment.NewLine);

            foreach (var pair in content.AdditionalParams)
            {
                stringBuilder.Append("        ");
                stringBuilder.Append(pair.Key);
                stringBuilder.Append(':');
                stringBuilder.Append(IAppOutputStandard.TabCharacter);

                string value = pair.Value?.ToString();
                if (IsMultilineText(value, out _))
                {
                    stringBuilder.Append(Environment.NewLine);
                    stringBuilder.Append("          ");
                }

                stringBuilder.Append(IndentMultilineText(value, "          "));
                stringBuilder.Append(Environment.NewLine);
            }

            return stringBuilder.ToString().TrimEnd();
        }

        private static void AppendHeader(StringBuilder stringBuilder, AppPrintInformation content)
        {
            stringBuilder.Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            stringBuilder.Append(IAppOutputStandard.TabCharacter);
            stringBuilder.Append('[');
            stringBuilder.Append(content.GetLevelTag());
            stringBuilder.Append(']');
            stringBuilder.Append(IAppOutputStandard.TabCharacter);
            stringBuilder.Append('[');
            stringBuilder.Append(GetOutputSourceAssembly(content));
            stringBuilder.Append(']');
            stringBuilder.Append(IAppOutputStandard.TabCharacter);

            string title = ToSingleLine(content.Title);
            string message = ToSingleLine(content.Content);

            if (!string.IsNullOrWhiteSpace(title))
            {
                stringBuilder.Append(title);
            }

            if (!string.IsNullOrWhiteSpace(title) && !string.IsNullOrWhiteSpace(message))
            {
                stringBuilder.Append(" | ");
            }

            if (!string.IsNullOrWhiteSpace(message))
            {
                stringBuilder.Append(message);
            }
        }

        private static bool HasAdditionalParams(AppPrintInformation content)
        {
            return content.AdditionalParams != null && content.AdditionalParams.Count > 0;
        }

        private static string ToSingleLine(object value)
        {
            return value?.ToString()?.Replace("\r\n", " ").Replace("\n", " ") ?? string.Empty;
        }

        private static string GetOutputSourceAssembly(AppPrintInformation content)
        {
            if (!string.IsNullOrWhiteSpace(content.SourceAssembly))
            {
                return content.SourceAssembly;
            }

            content.SourceAssembly = GetOutputCallerAssemblyName();
            return content.SourceAssembly;
        }

        /// <summary>
        /// 判断字符串是单行还是多行（精确版，可获取行数）
        /// </summary>
        /// <param name="text">要判断的字符串</param>
        /// <param name="lineCount">输出：字符串的行数</param>
        /// <returns>true=多行，false=单行</returns>
        public static bool IsMultilineText(string text, out int lineCount)
        {
            if (string.IsNullOrEmpty(text))
            {
                lineCount = 0;
                return false;
            }

            // 统一分割符：先将 \r\n 替换为 \n，再按 \n 分割
            string normalizedText = text.Replace("\r\n", "\n");
            string[] lines = normalizedText.Split(new[] { '\n' }, StringSplitOptions.None);

            lineCount = lines.Length;
            return lineCount > 1; // 行数 > 1 即为多行
        }

        private static string GetOutputCallerAssemblyName()
        {
            var stackTrace = new StackTrace(false);

            foreach (var frame in stackTrace.GetFrames() ?? Array.Empty<StackFrame>())
            {
                var method = frame.GetMethod();
                var declaringType = method?.DeclaringType;
                var assembly = declaringType?.Assembly;

                if (assembly == null)
                {
                    continue;
                }

                if (declaringType == typeof(DefaultAppOutputDependency))
                {
                    continue;
                }

                return assembly.GetName().Name;
            }

            return "unknown";
        }
    }
}
