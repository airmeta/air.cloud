
using Air.Cloud.Core.Plugins.Security.MD5;
using Air.Cloud.WebApp.App;
var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseKestrel(options =>
{
    options.Limits.MaxRequestLineSize = int.MaxValue;//HTTP 请求行的最大允许大小。 默认为 8kb
    options.Limits.MaxRequestBufferSize = int.MaxValue;//请求缓冲区的最大大小。 默认为 1M
    //任何请求正文的最大允许大小（以字节为单位）,默认 30,000,000 字节，大约为 28.6MB
    options.Limits.MaxRequestBodySize = int.MaxValue;//限制请求长度
});
var app = builder.WebInjectInFile();
app.Run();
