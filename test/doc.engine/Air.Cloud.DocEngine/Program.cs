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
using Air.Cloud.Core.App;
using Air.Cloud.Core.Extensions;
using Air.Cloud.Core.Plugins.Reflection.Internal;
using Air.Cloud.DocEngine.XmlDocHelper;

using System;
using System.Net;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Xml.Linq;

using Air.Cloud.Core.App;
using Air.Cloud.HostApp.Dependency;
using System.Net;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Hosting;
var x = Host.CreateDefaultBuilder(args);
var builder = x.InjectGrpcServer(s =>
{
    s.UseStartup<>();
}).HostInjectInFile(Assembly.GetExecutingAssembly());
var app = builder.Build();

app.Run();

//SocketClient.LoaderOptimization(IPAddress.Parse("127.0.0.1"), 8888);


//var xmlCommentHelper = new XmlCommentHelper();
//xmlCommentHelper.LoadAll();

//IEnumerable<Type> alltypes = typeof(AppCore).Assembly.GetTypes().Where(s => s.IsPublic);
//string BasePath = "E:\\Docs";
//if (!Directory.Exists(BasePath)) Directory.CreateDirectory(BasePath);
//string sealeds = "sealed";
//string interfaces = "interface";
//string classs = "class";
//string[] ig = new string[]
//       {
//            "GetType",
//             "ToString",
//             "Equals",
//             "GetHashCode"
//       };
//Dictionary<string,string> strings = new Dictionary<string, string>();
//foreach (var type in alltypes)
//{
//    string RoutePath = type.FullName.Replace("." + type.Name, "").Replace(".", "\\");
//    string FileDirectory = BasePath + "\\" + RoutePath;
//    if (!Directory.Exists(FileDirectory)) Directory.CreateDirectory(FileDirectory);
//    string FilePath = FileDirectory + "\\" + type.Name.Replace("`1", "");
//    if (!File.Exists(FilePath + ".md"))
//    {
//        var file = File.Create(FilePath + ".md");
//        file.Close();
//        file.Dispose();
//        //File.Copy(FilePath+".md", FilePath+"_"+Guid.NewGuid().ToString()+ ".md");
//        //File.DeleteAsync(FilePath + ".md");
//    }
//    List<string> Contents = new List<string>();
//    var typeComment = xmlCommentHelper.GetTypeComment(type);
//    Contents.Add($"## {type.Name}.cs ");
//    Contents.Add("\r\n");
//    Contents.Add("#### 描述:");
//    Contents.Add("\r\n");
//    Contents.Add(typeComment.Replace("<br>", ""));
//    Contents.Add("\r\n");
//    Contents.Add("#### 定义: ");
//    Contents.Add("``` csharp");
//    Contents.Add($"public {(type.IsSealed ? sealeds : string.Empty)} {(type.Name.EndsWith("`1") ? interfaces : classs)} {type.Name.Replace("`1", "")}");
//    Contents.Add("```");

//    var fields = type.GetFields(bindingAttr: System.Reflection.BindingFlags.NonPublic);
//    foreach (var field in fields)
//    {
//        var fieldComment = xmlCommentHelper.GetFieldOrPropertyComment(field);
//        Console.WriteLine($"{field.Name}字段的注释：{fieldComment}");
//    }
//    var properties = type.GetProperties();
//    if (properties.Any())
//    {
//        Contents.Add($"---");
//        Contents.Add($"## 属性 ");
//        Contents.Add($"| Name      | Type | Description|");
//        Contents.Add("| ----------- | ----------- |-----------|");
//    }
//    foreach (var property in properties)
//    {
//        var propertyComment = xmlCommentHelper.GetFieldOrPropertyComment(property);
//        Contents.Add($"|     {property.Name} |  {property.PropertyType.FullName?.Substring(0, (property.PropertyType.FullName.IndexOf("`1") == -1 ? property.PropertyType.FullName.Length : property.PropertyType.FullName.IndexOf("`1")))} | {propertyComment.Replace("<br>", "").Replace("\r\n", "<br>")} |");
//    }

//    var methods = type.GetMethods().Where(s => (!(s.Name.StartsWith("set_") || s.Name.StartsWith("get_"))) && (!ig.Contains(s.Name)));
//    if (methods.Any())
//    {
//        Contents.Add($"---");
//        Contents.Add($"## 方法 ");
//        Contents.Add("| MethodName      | Description | ");
//        Contents.Add("| ----------- | ----------- |");
//    }
//    foreach (var method in methods)
//    {
//        var methodComment = xmlCommentHelper.GetMethodComment(method);
//        Contents.Add($"| {method.Name} | {methodComment.Replace("<br>", "").Replace("\r\n", "<br>")} |");
//    }
//    if (methods.Any())
//    {
//        Contents.Add($"---");
//        Contents.Add($"### 方法详解 ");
//    }
//    foreach (var method in methods)
//    {
//        var methodComment = xmlCommentHelper.GetMethodComment(method);
//        var dict = xmlCommentHelper.GetParameterComments(method);
//        Contents.Add($"####  {method.Name}");
//        Contents.Add($"* 方法描述:<br> {methodComment.Replace("<br>", "").Replace("\r\n", "<br>")}");
//        Contents.Add($"* 方法类型:"+(method.IsStatic?"静态方法":method.IsAsync()?"异步方法":"普通方法"));
//        if (method.ReturnType.IsGenericType)
//        {
//            string c = method.ReturnType.GetGenericArguments()[0].Name;
//            Contents.Add($"* 响应类型: {method.ReturnType.Name.Replace("`1", "").Replace("1", "")}<{c}>");
//        }
//        else
//        {
//            Contents.Add($"* 响应类型:<br> {method.ReturnType.Name} <br> ({method.ReturnType.FullName?.Split(",")[0]})");
//        }
//        Contents.Add($"* 方法参数:");
//        ParameterInfo[] methodParameterInfos = method.GetParameters();
//        string[] Arr = dict.Keys.ToArray();
//        Contents.Add($"| Name      | Type | Description|");
//        Contents.Add("| ----------- | ----------- |-----------|");
//        for (int i = 0; i < methodParameterInfos.Count(); i++)
//        {
//            ParameterInfo? para = methodParameterInfos[i];
//            if (para == null|| para.Name==null) continue;
//            string? Value = dict[para.Name];
//            string TypeStr = string.Empty;
//            if (para.ParameterType.IsGenericType)
//            {
//                string c = para.ParameterType.GetGenericArguments()[0].Name;
//                TypeStr=$"{para.ParameterType.Name.Replace("`1", "").Replace("1","")}<{c}>";
//            }
//            else
//            {
//                TypeStr = $"{para.ParameterType.Name}";
//            }
//            Contents.Add($"| {para.Name} | {TypeStr} |<br> {Value.Replace("<br>", "").Replace("\r\n", "<br>")}|");
//        }
//    }
//    File.WriteAllLines(FilePath + ".md", Contents);
//    string Url = FilePath.Replace(BasePath, "").Replace("\\", "/").ToLower().Replace(type.Name.Replace("`1", "").ToLower(), type.Name.Replace("`1", "")).Replace("/air/cloud/", "/air.cloud/");
//    if (!strings.ContainsKey(Url))
//    {
//        strings.Add(Url, $"* [{type.Name.Replace("`1", "")}.cs]({Url}.md)");
//    }
//}
//File.AppendAllLines(BasePath + "/" + "Slide.md", strings.OrderBy(s=>s.Key).Select(s=>s.Value).ToList());
//Console.WriteLine("====================================");