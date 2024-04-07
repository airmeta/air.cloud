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
using System.Data;
using System.Xml;
using System.Xml.Serialization;

namespace Air.Cloud.Core.Util
{
    public class XmlHelper
    {

        #region 反序列化
        /// <summary>
        /// 反序列化
        /// </summary>
        /// <param name="type">类型</param>
        /// <param name="xml">XML字符串</param>
        /// <returns></returns>
        public static object Deserialize(Type type, string xml)
        {
            try
            {
                using (StringReader sr = new StringReader(xml))
                {
                    XmlSerializer xmldes = new XmlSerializer(type);
                    return xmldes.Deserialize(sr);
                }
            }
            catch
            {
                throw new Exception("反序列化失败");
            }
        }
        /// <summary>
        /// 反序列化
        /// </summary>
        /// <param name="type"></param>
        /// <param name="xml"></param>
        /// <returns></returns>
        public static object Deserialize(Type type, Stream stream)
        {
            XmlSerializer xmldes = new XmlSerializer(type);
            return xmldes.Deserialize(stream);
        }
        #endregion

        #region 序列化XML文件
        /// <summary>
        /// 序列化XML文件
        /// </summary>
        /// <param name="type">类型</param>
        /// <param name="obj">对象</param>
        /// <returns></returns>
        public static string Serializer(Type type, object obj)
        {
            MemoryStream Stream = new MemoryStream();
            //创建序列化对象
            XmlSerializer xml = new XmlSerializer(type);
            try
            {
                //序列化对象
                xml.Serialize(Stream, obj);
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            Stream.Position = 0;
            StreamReader sr = new StreamReader(Stream);
            string str = sr.ReadToEnd();
            return str;
        }
        #endregion

        #region 将XML转换为DATATABLE

        /// <summary>
        /// 将XML转换为DATATABLE
        /// </summary>
        /// <param name="FileURL"></param>
        /// <returns></returns>
        public static DataTable XmlAnalysisArray(string FileURL)
        {
            DataSet ds = new DataSet();
            ds.ReadXml(FileURL);
            return ds.Tables[0];
        }
        #endregion

        #region 获取对应XML节点的值
        /// <summary>
        /// 摘要:获取对应XML节点的值
        /// </summary>
        /// <param name="stringRoot">XML节点的标记</param>
        /// <returns>返回获取对应XML节点的值</returns>
        public static string XmlAnalysis(string stringRoot, string xml)
        {
            if (stringRoot.Equals("") == false)
            {
                try
                {
                    XmlDocument XmlLoad = new XmlDocument();
                    XmlLoad.LoadXml(xml);
                    return XmlLoad.DocumentElement.SelectSingleNode(stringRoot).InnerXml.Trim();
                }
                catch
                {
                    throw new Exception("解析XML失败");
                }
            }
            return "";
        }
        #endregion


        /// <summary>
        /// 序列化为xml
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string XmlSerialize<T>(T obj)
        {
            using (StringWriter sw = new StringWriter())
            {
                Type t = obj.GetType();
                // 强制指定命名空间，覆盖默认的命名空间  
                XmlSerializerNamespaces namespaces = new XmlSerializerNamespaces();
                namespaces.Add(string.Empty, string.Empty);

                XmlSerializer serializer = new XmlSerializer(obj.GetType());
                // 序列化时增加namespaces
                serializer.Serialize(sw, obj, namespaces);

                sw.Close();
                return sw.ToString();
            }
        }


    }
}
