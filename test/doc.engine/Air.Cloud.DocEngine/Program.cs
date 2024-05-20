using Air.Cloud.Core.App;
using Air.Cloud.Core.Extensions;
using Air.Cloud.DocEngine.XmlDocHelper;

using System;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Xml.Linq;

var xmlCommentHelper = new XmlCommentHelper();
xmlCommentHelper.LoadAll();

IEnumerable<Type> alltypes = typeof(AppCore).Assembly.GetTypes().Where(s => s.IsPublic);
string BasePath = "E:\\Docs";
if (!Directory.Exists(BasePath)) Directory.CreateDirectory(BasePath);
string sealeds = "sealed";
string interfaces = "interface";
string classs = "class";
string[] ig = new string[]
       {
            "GetType",
             "ToString",
             "Equals",
             "GetHashCode"
       };
foreach (var type in alltypes)
{
    string RoutePath = type.FullName.Replace("." + type.Name, "").Replace(".", "\\");
    string FileDirectory = BasePath + "\\" + RoutePath;
    if (!Directory.Exists(FileDirectory)) Directory.CreateDirectory(FileDirectory);
    string FilePath = FileDirectory + "\\" + type.Name.Replace("`1", "");
    if (!File.Exists(FilePath + ".md"))
    {
        var file = File.Create(FilePath + ".md");
        file.Close();
        file.Dispose();
        //File.Copy(FilePath+".md", FilePath+"_"+Guid.NewGuid().ToString()+ ".md");
        //File.DeleteAsync(FilePath + ".md");
    }
    List<string> Contents = new List<string>();
    var typeComment = xmlCommentHelper.GetTypeComment(type);
    Contents.Add($"## {type.Name}.cs ");
    Contents.Add("\r\n");
    Contents.Add("#### 描述:");
    Contents.Add("\r\n");
    Contents.Add(typeComment.Replace("<br>", ""));
    Contents.Add("\r\n");
    Contents.Add("#### 定义: ");
    Contents.Add("``` csharp");
    Contents.Add($"public {(type.IsSealed ? sealeds : string.Empty)} {(type.Name.EndsWith("`1") ? interfaces : classs)} {type.Name.Replace("`1", "")}");
    Contents.Add("```");

    var fields = type.GetFields(bindingAttr: System.Reflection.BindingFlags.NonPublic);
    foreach (var field in fields)
    {
        var fieldComment = xmlCommentHelper.GetFieldOrPropertyComment(field);
        Console.WriteLine($"{field.Name}字段的注释：{fieldComment}");
    }
    var properties = type.GetProperties();
    if (properties.Any())
    {
        Contents.Add($"---");
        Contents.Add($"## 属性 ");
        Contents.Add($"| Name      | Type | Description|");
        Contents.Add("| ----------- | ----------- |-----------|");
    }
    foreach (var property in properties)
    {
        var propertyComment = xmlCommentHelper.GetFieldOrPropertyComment(property);
        Contents.Add($"|     {property.Name} |  {property.PropertyType.FullName?.Substring(0, (property.PropertyType.FullName.IndexOf("`1") == -1 ? property.PropertyType.FullName.Length : property.PropertyType.FullName.IndexOf("`1")))} | {propertyComment.Replace("<br>", "").Replace("\r\n", "<br>")} |");
    }

    var methods = type.GetMethods().Where(s => (!(s.Name.StartsWith("set_") || s.Name.StartsWith("get_"))) && (!ig.Contains(s.Name)));
    if (methods.Any())
    {
        Contents.Add($"---");
        Contents.Add($"## 方法 ");
        Contents.Add("| MethodName      | Description | ");
        Contents.Add("| ----------- | ----------- |");
    }
    foreach (var method in methods)
    {
        var methodComment = xmlCommentHelper.GetMethodComment(method);
        Contents.Add($"| {method.Name} | {methodComment.Replace("<br>", "").Replace("\r\n", "<br>")} |");
    }
    if (methods.Any())
    {
        Contents.Add($"---");
        Contents.Add($"### 方法详解 ");
    }
    foreach (var method in methods)
    {
        var methodComment = xmlCommentHelper.GetMethodComment(method);
        var dict = xmlCommentHelper.GetParameterComments(method);
        Contents.Add($"####  {method.Name}");
        Contents.Add($"* 描述:解析服务提供器");
        Contents.Add($"* MethodType:"+(method.IsStatic?"静态方法":method.IsAsync()?"异步方法":""));
        Contents.Add($"* Return:[IServiceProvider] (https://learn.microsoft.com/zh-cn/dotnet/api/system.iserviceprovider?view=net-6.0)");
        Contents.Add($"* 参数:");
        string[] Arr = dict.Keys.ToArray();
        for (int i = 0; i < Arr.Count(); i++)
        {
            string Key = Arr[i];
            string Value = dict[Key];
            Contents.Add($"  * 参数{i+1}:");
            Contents.Add($"     * 名称: {Key}");
            Contents.Add($"     * 类型: ------");
            Contents.Add($"     * 描述:<br> {Value.Replace("<br>", "").Replace("\r\n", "<br>")}");
        }
    }
    File.WriteAllLines(FilePath + ".md", Contents);

}
Console.WriteLine("====================================");