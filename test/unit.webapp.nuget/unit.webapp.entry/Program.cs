using Air.Cloud.Core.App;
using Air.Cloud.Core.Extensions;
using Air.Cloud.WebApp.App;

var builder = WebApplication.CreateBuilder(args);

var app = builder.WebInjectInFile();
app.Run();