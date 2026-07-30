using Air.Cloud.Core.Standard.Container;
using Air.Cloud.Modules.Docker.Extensions;
using Air.Cloud.WebApp.App;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDockerEngineContainerService(options =>
{
    options.Enabled = true;
    options.DockerSocketPath = "/var/run/docker.sock";
});

var app = builder.WebInjectInFile();

app.MapGet("/stop", async (HttpContext context) =>
{
    var containerEngine = context.RequestServices.GetRequiredService<IContainerEngineStandard>();
    var id = context.Request.Query["ID"].ToString();
    return await containerEngine.StopAsync(id);
});

app.MapGet("/start", async (HttpContext context) =>
{
    var containerEngine = context.RequestServices.GetRequiredService<IContainerEngineStandard>();
    var id = context.Request.Query["ID"].ToString();
    return await containerEngine.StartAsync(id);
});

app.MapGet("/", async (HttpContext context) =>
{
    var containerEngine = context.RequestServices.GetRequiredService<IContainerEngineStandard>();
    return await containerEngine.ListAsync();
});

app.Run();
