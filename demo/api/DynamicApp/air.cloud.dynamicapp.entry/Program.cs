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

using Air.Cloud.DynamicApp.Gateway;
using Air.Cloud.DynamicApp.Gateway.Extensions;
using Air.Cloud.DynamicApp.Host;
using Air.Cloud.DynamicApp.Host.Extensions;
using Air.Cloud.DynamicApp.Manager;
using Air.Cloud.DynamicApp.Manager.Extensions;
using Air.Cloud.DynamicApp.Orchestrator;
using Air.Cloud.DynamicApp.Orchestrator.Extensions;
using Air.Cloud.Modules.Consul.Extensions;
using Air.Cloud.Modules.Consul.Model;
using Air.Cloud.Modules.Docker.Extensions;

var builder = WebApplication.CreateBuilder(args);
var consulIntegration = ResolveConsulIntegration(builder.Configuration);
ConsulServiceOptions? consulServiceOptions = null;
if (consulIntegration.UsesConsul)
{
    consulServiceOptions = builder.LoadConfigurationFromConsul(options =>
    {
        options.EnableConfigurationCenter = consulIntegration.LoadConfiguration;
        options.EnableServiceRegistration = consulIntegration.RegisterService;
        options.EnableHealthCheck = consulIntegration.RegisterHealthCheck;
        options.DynamicAppManaged = false;
    });
}

var dynamicAppSection = builder.Configuration.GetSection("DynamicApp");

builder.Services.AddDynamicAppHost(
    configureProcess: options => ConfigureProcess(options, dynamicAppSection.GetSection("Process")),
    configureManagerApi: options => ConfigureManagerApi(options, dynamicAppSection.GetSection("ManagerApi")),
    configurePackageStore: options => ConfigurePackageStore(options, dynamicAppSection.GetSection("PackageStore")),
    configureHealthMonitor: options => ConfigureHealthMonitor(options, dynamicAppSection.GetSection("HealthMonitor")),
    configureSelfHealing: options => ConfigureSelfHealing(options, dynamicAppSection.GetSection("SelfHealing")),
    configureSelfHealingStore: options => ConfigureSelfHealingStore(options, dynamicAppSection.GetSection("SelfHealingStateStore")),
    configureArchiveStore: options => ConfigureArchiveStore(options, dynamicAppSection.GetSection("TargetArchiveStore")),
    configureTransactionLog: options => ConfigureTransactionLog(options, dynamicAppSection.GetSection("ControlPlaneTransactionLog")),
    configureDrain: options => ConfigureDrain(options, dynamicAppSection.GetSection("Drain")));

builder.Services.AddFileDynamicAppStateStore(options => ConfigureStateStore(options, dynamicAppSection.GetSection("StateStore")));
builder.Services.AddFileDynamicAppGatewayRouteStore(options => ConfigureGatewayRouteStore(options, dynamicAppSection.GetSection("GatewayRouteStore")));
builder.Services.AddFileDynamicAppManifestStore(options => ConfigureManifestStore(options, dynamicAppSection.GetSection("ManifestStore")));
builder.Services.AddDockerEngineContainerService(options => ConfigureDockerEngine(options, dynamicAppSection.GetSection("DockerEngine")));
builder.Services.AddDockerExistingContainerDynamicAppOrchestrator(options => ConfigureDockerOrchestrator(options, dynamicAppSection.GetSection("DockerExistingContainerOrchestrator")));
builder.Services.AddDynamicAppGateway(options => ConfigureGateway(options, dynamicAppSection.GetSection("Gateway")));
if (consulServiceOptions != null)
{
    builder.Services.RegisterConsulHealthCheckServices(consulServiceOptions);
}

var app = builder.Build();

app.MapGet("/Health", () => Results.Ok(new
{
    service = "air.dynamicapp",
    status = "Healthy",
    timestampUtc = DateTimeOffset.UtcNow
}));

app.UseDynamicAppManagerApi();
app.UseDynamicAppGateway();
if (consulServiceOptions != null)
{
    app.RegisterConsulServiceInformation(consulServiceOptions);
    app.RegisterConsulHealthCheck(consulServiceOptions);
}

app.Run();

static DynamicAppConsulIntegration ResolveConsulIntegration(IConfiguration configuration)
{
    var configurationCenter = configuration.GetSection("DynamicApp:ConfigurationCenter");
    var serviceRegistry = configuration.GetSection("DynamicApp:ServiceRegistry");
    var configurationProvider = ReadString(configurationCenter, "Provider", "File");
    var registryProvider = ReadString(serviceRegistry, "Provider", "None");
    var loadConfiguration = IsProvider(configurationProvider, "Consul")
        && ReadBool(configurationCenter, "Enabled", true);
    var registerService = IsProvider(registryProvider, "Consul")
        && ReadBool(serviceRegistry, "Enabled", true);
    var registerHealthCheck = registerService
        && ReadBool(serviceRegistry, "EnableHealthCheck", true);

    return new DynamicAppConsulIntegration(
        loadConfiguration || registerService || registerHealthCheck,
        loadConfiguration,
        registerService,
        registerHealthCheck);
}

static bool IsProvider(string value, string provider)
{
    return string.Equals(value, provider, StringComparison.OrdinalIgnoreCase);
}

static void ConfigureManagerApi(DynamicAppManagerApiOptions options, IConfiguration section)
{
    options.RoutePrefix = ReadString(section, "RoutePrefix", options.RoutePrefix);
    options.RequireTicket = ReadBool(section, "RequireTicket", ReadBool(section, "RequireAccessToken", options.RequireTicket));
    options.HeaderName = ReadString(section, "HeaderName", options.HeaderName);
    options.Tickets = ReadStringArray(section, "Tickets");
    if (options.Tickets.Length == 0)
    {
        options.Tickets = ReadStringArray(section, "AccessTokens");
    }
}

static void ConfigureProcess(DynamicAppProcessOptions options, IConfiguration section)
{
    options.UseShellExecute = ReadBool(section, "UseShellExecute", options.UseShellExecute);
    options.RedirectStandardOutput = ReadBool(section, "RedirectStandardOutput", options.RedirectStandardOutput);
    options.RedirectStandardError = ReadBool(section, "RedirectStandardError", options.RedirectStandardError);
    options.GracefulStopTimeout = TimeSpan.FromSeconds(ReadInt(section, "GracefulStopTimeoutSeconds", (int)options.GracefulStopTimeout.TotalSeconds));
    options.EarlyExitWatchTimeout = TimeSpan.FromMilliseconds(ReadInt(section, "EarlyExitWatchMilliseconds", (int)options.EarlyExitWatchTimeout.TotalMilliseconds));
    options.MonitorUnexpectedExit = ReadBool(section, "MonitorUnexpectedExit", options.MonitorUnexpectedExit);
}

static string[] ReadStringArray(IConfiguration section, string key)
{
    return section.GetSection(key).GetChildren()
        .Select(item => item.Value)
        .Where(value => !string.IsNullOrWhiteSpace(value))
        .Select(value => value!)
        .ToArray();
}

static void ConfigureHealthMonitor(DynamicAppHealthMonitorOptions options, IConfiguration section)
{
    options.Enabled = ReadBool(section, "Enabled", options.Enabled);
    options.ProbeInterval = TimeSpan.FromSeconds(ReadInt(section, "ProbeIntervalSeconds", (int)options.ProbeInterval.TotalSeconds));
    options.FailureThreshold = ReadInt(section, "FailureThreshold", options.FailureThreshold);
    options.SuccessThreshold = ReadInt(section, "SuccessThreshold", options.SuccessThreshold);
}

static void ConfigureSelfHealing(DynamicAppSelfHealingOptions options, IConfiguration section)
{
    options.Enabled = ReadBool(section, "Enabled", options.Enabled);
    options.ScanInterval = TimeSpan.FromSeconds(ReadInt(section, "ScanIntervalSeconds", (int)options.ScanInterval.TotalSeconds));
    options.RestartBackoff = TimeSpan.FromSeconds(ReadInt(section, "RestartBackoffSeconds", (int)options.RestartBackoff.TotalSeconds));
    options.MaxRestartAttempts = ReadInt(section, "MaxRestartAttempts", options.MaxRestartAttempts);
    options.AttemptWindow = TimeSpan.FromSeconds(ReadInt(section, "AttemptWindowSeconds", (int)options.AttemptWindow.TotalSeconds));
    options.ResetAttemptsAfterHealthy = ReadBool(section, "ResetAttemptsAfterHealthy", options.ResetAttemptsAfterHealthy);
}

static void ConfigurePackageStore(DynamicAppPackageStoreOptions options, IConfiguration section)
{
    options.RootPath = ReadString(section, "RootPath", options.RootPath);
    options.MaxPackageBytes = ReadLong(section, "MaxPackageBytes", options.MaxPackageBytes);
    options.AllowOverwrite = ReadBool(section, "AllowOverwrite", options.AllowOverwrite);
}

static void ConfigureDrain(DynamicAppDrainOptions options, IConfiguration section)
{
    options.Enabled = ReadBool(section, "Enabled", options.Enabled);
    options.ScanInterval = TimeSpan.FromSeconds(ReadInt(section, "ScanIntervalSeconds", (int)options.ScanInterval.TotalSeconds));
    options.DrainTimeout = TimeSpan.FromSeconds(ReadInt(section, "DrainTimeoutSeconds", (int)options.DrainTimeout.TotalSeconds));
    options.RemoveTargetWhenStopFails = ReadBool(section, "RemoveTargetWhenStopFails", options.RemoveTargetWhenStopFails);
}

static void ConfigureStateStore(FileDynamicAppStateStoreOptions options, IConfiguration section)
{
    options.FilePath = ReadNullableString(section, "FilePath") ?? options.FilePath;
    options.SaveOnChange = ReadBool(section, "SaveOnChange", options.SaveOnChange);
}

static void ConfigureGatewayRouteStore(FileDynamicAppGatewayRouteStoreOptions options, IConfiguration section)
{
    options.FilePath = ReadNullableString(section, "FilePath") ?? options.FilePath;
    options.SaveOnChange = ReadBool(section, "SaveOnChange", options.SaveOnChange);
}

static void ConfigureManifestStore(FileDynamicAppManifestStoreOptions options, IConfiguration section)
{
    options.FilePath = ReadNullableString(section, "FilePath") ?? options.FilePath;
    options.MaxRecordsPerApp = ReadInt(section, "MaxRecordsPerApp", options.MaxRecordsPerApp);
}

static void ConfigureSelfHealingStore(FileDynamicAppSelfHealingStateStoreOptions options, IConfiguration section)
{
    options.FilePath = ReadNullableString(section, "FilePath") ?? options.FilePath;
    options.SaveOnChange = ReadBool(section, "SaveOnChange", options.SaveOnChange);
}

static void ConfigureArchiveStore(FileDynamicAppTargetArchiveStoreOptions options, IConfiguration section)
{
    options.FilePath = ReadNullableString(section, "FilePath") ?? options.FilePath;
    options.SaveOnChange = ReadBool(section, "SaveOnChange", options.SaveOnChange);
}

static void ConfigureTransactionLog(FileDynamicAppControlPlaneTransactionLogOptions options, IConfiguration section)
{
    options.FilePath = ReadNullableString(section, "FilePath") ?? options.FilePath;
}

static void ConfigureDockerOrchestrator(DockerExistingContainerDynamicAppOrchestratorOptions options, IConfiguration section)
{
    options.Enabled = ReadBool(section, "Enabled", options.Enabled);
    options.ContainerNameTemplate = ReadString(section, "ContainerNameTemplate", options.ContainerNameTemplate);
    options.TargetBaseAddressTemplate = ReadNullableString(section, "TargetBaseAddressTemplate") ?? options.TargetBaseAddressTemplate;
}

static void ConfigureDockerEngine(Air.Cloud.Modules.Docker.Options.DockerEngineContainerServiceOptions options, IConfiguration section)
{
    options.Enabled = ReadBool(section, "Enabled", options.Enabled);
    options.DockerSocketPath = ReadString(section, "DockerSocketPath", options.DockerSocketPath);
    options.RequestTimeout = TimeSpan.FromSeconds(ReadInt(section, "RequestTimeoutSeconds", (int)options.RequestTimeout.TotalSeconds));
    options.StartTimeout = TimeSpan.FromSeconds(ReadInt(section, "StartTimeoutSeconds", (int)options.StartTimeout.TotalSeconds));
    options.StopTimeout = TimeSpan.FromSeconds(ReadInt(section, "StopTimeoutSeconds", (int)options.StopTimeout.TotalSeconds));
    options.StopGracePeriodSeconds = ReadInt(section, "StopGracePeriodSeconds", options.StopGracePeriodSeconds);
}

static void ConfigureGateway(DynamicAppGatewayOptions options, IConfiguration section)
{
    options.ForwardHostHeader = ReadBool(section, "ForwardHostHeader", options.ForwardHostHeader);
    options.ForwardTimeout = TimeSpan.FromSeconds(ReadInt(section, "ForwardTimeoutSeconds", (int)options.ForwardTimeout.TotalSeconds));
    options.MaxRequestBodyBytes = ReadLong(section, "MaxRequestBodyBytes", options.MaxRequestBodyBytes);
    options.RejectUpgradeRequests = ReadBool(section, "RejectUpgradeRequests", options.RejectUpgradeRequests);
}

static string ReadString(IConfiguration section, string key, string fallback)
{
    return string.IsNullOrWhiteSpace(section[key]) ? fallback : section[key]!;
}

static string? ReadNullableString(IConfiguration section, string key)
{
    return string.IsNullOrWhiteSpace(section[key]) ? null : section[key];
}

static bool ReadBool(IConfiguration section, string key, bool fallback)
{
    return bool.TryParse(section[key], out var value) ? value : fallback;
}

static int ReadInt(IConfiguration section, string key, int fallback)
{
    return int.TryParse(section[key], out var value) ? value : fallback;
}

static long ReadLong(IConfiguration section, string key, long fallback)
{
    return long.TryParse(section[key], out var value) ? value : fallback;
}

internal sealed record DynamicAppConsulIntegration(
    bool UsesConsul,
    bool LoadConfiguration,
    bool RegisterService,
    bool RegisterHealthCheck);
