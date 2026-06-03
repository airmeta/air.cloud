using Air.Cloud.Core.Extensions;
using Air.Cloud.WebApp.DynamicApiController.Attributes;
using Air.Cloud.WebApp.DynamicApiController.Options;

using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.Extensions.Options;

using System.Text.RegularExpressions;

namespace Air.Cloud.WebApp.DynamicApiController.Internal;

internal sealed class DynamicApiNameResolver
{
    private static readonly Regex NameVersionRegex = new(@"V(?<version>[0-9_]+$)", RegexOptions.Compiled);

    private readonly DynamicApiControllerSettingsOptions _settings;
    private readonly DynamicApiVerbMap _verbMap;

    public DynamicApiNameResolver(IOptions<DynamicApiControllerSettingsOptions> settings, DynamicApiVerbMap verbMap)
        : this(settings.Value, verbMap)
    {
    }

    public DynamicApiNameResolver(DynamicApiControllerSettingsOptions settings, DynamicApiVerbMap verbMap)
    {
        _settings = settings;
        _verbMap = verbMap;
    }

    public void ConfigureControllerName(ControllerModel controller, ApiDescriptionSettingsAttribute apiDescriptionSettings)
    {
        controller.ControllerName = ResolveControllerName(controller.ControllerType.Name, apiDescriptionSettings).Name;
    }

    public DynamicApiNameResult ConfigureActionName(
        ActionModel action,
        ApiDescriptionSettingsAttribute apiDescriptionSettings,
        ApiDescriptionSettingsAttribute controllerApiDescriptionSettings)
    {
        var result = ResolveActionName(action.ActionMethod.Name, apiDescriptionSettings, controllerApiDescriptionSettings);
        action.ActionName = result.Name;
        return result;
    }

    public DynamicApiNameResult ResolveControllerName(string originalName, ApiDescriptionSettingsAttribute apiDescriptionSettings)
    {
        return ConfigureControllerAndActionName(
            apiDescriptionSettings,
            originalName,
            _settings.AbandonControllerAffixes,
            name => name);
    }

    public DynamicApiNameResult ResolveActionName(
        string originalName,
        ApiDescriptionSettingsAttribute apiDescriptionSettings,
        ApiDescriptionSettingsAttribute controllerApiDescriptionSettings)
    {
        return ConfigureControllerAndActionName(
            apiDescriptionSettings,
            originalName,
            _settings.AbandonActionAffixes,
            name => CheckIsKeepVerb(apiDescriptionSettings, controllerApiDescriptionSettings)
                ? name
                : _verbMap.RemoveVerbPrefix(name),
            controllerApiDescriptionSettings);
    }

    private DynamicApiNameResult ConfigureControllerAndActionName(
        ApiDescriptionSettingsAttribute apiDescriptionSettings,
        string originalName,
        string[] affixes,
        Func<string, string> configure,
        ApiDescriptionSettingsAttribute controllerApiDescriptionSettings = default)
    {
        var apiVersion = apiDescriptionSettings?.Version;
        var isKeepName = false;
        var tempName = apiDescriptionSettings?.Name;

        if (string.IsNullOrWhiteSpace(tempName))
        {
            var (name, version) = ResolveNameVersion(originalName);
            tempName = name;
            apiVersion ??= version;
            tempName = tempName.ClearStringAffixes(affixes: affixes);
            isKeepName = CheckIsKeepName(controllerApiDescriptionSettings == null ? null : apiDescriptionSettings, controllerApiDescriptionSettings ?? apiDescriptionSettings);

            if (!isKeepName)
            {
                tempName = configure.Invoke(tempName);

                if (CheckIsSplitCamelCase(controllerApiDescriptionSettings == null ? null : apiDescriptionSettings, controllerApiDescriptionSettings ?? apiDescriptionSettings))
                {
                    tempName = string.Join(_settings.CamelCaseSeparator, tempName.SplitCamelCase());
                }
            }
        }

        var newName = $"{tempName}{(string.IsNullOrWhiteSpace(apiVersion) ? null : $"{_settings.VersionSeparator}{apiVersion}")}";
        var isLowercaseRoute = CheckIsLowercaseRoute(controllerApiDescriptionSettings == null ? null : apiDescriptionSettings, controllerApiDescriptionSettings ?? apiDescriptionSettings);
        var isLowerCamelCase = CheckIsLowerCamelCase(controllerApiDescriptionSettings == null ? null : apiDescriptionSettings, controllerApiDescriptionSettings ?? apiDescriptionSettings);

        return new DynamicApiNameResult(
            isLowercaseRoute ? newName.ToLower() : isLowerCamelCase ? newName.ToLowerCamelCase() : newName,
            isLowercaseRoute,
            isKeepName,
            isLowerCamelCase);
    }

    private bool CheckIsKeepName(ApiDescriptionSettingsAttribute apiDescriptionSettings, ApiDescriptionSettingsAttribute controllerApiDescriptionSettings)
    {
        bool isKeepName;

        if (apiDescriptionSettings?.KeepName != null)
        {
            var canParse = bool.TryParse(apiDescriptionSettings.KeepName.ToString(), out var value);
            isKeepName = canParse && value;
        }
        else if (controllerApiDescriptionSettings?.KeepName != null)
        {
            var canParse = bool.TryParse(controllerApiDescriptionSettings.KeepName.ToString(), out var value);
            isKeepName = canParse && value;
        }
        else
        {
            isKeepName = _settings?.KeepName == true;
        }

        return isKeepName;
    }

    private bool CheckIsKeepVerb(ApiDescriptionSettingsAttribute apiDescriptionSettings, ApiDescriptionSettingsAttribute controllerApiDescriptionSettings)
    {
        bool isKeepVerb;

        if (apiDescriptionSettings?.KeepVerb != null)
        {
            var canParse = bool.TryParse(apiDescriptionSettings.KeepVerb.ToString(), out var value);
            isKeepVerb = canParse && value;
        }
        else if (controllerApiDescriptionSettings?.KeepVerb != null)
        {
            var canParse = bool.TryParse(controllerApiDescriptionSettings.KeepVerb.ToString(), out var value);
            isKeepVerb = canParse && value;
        }
        else
        {
            isKeepVerb = _settings?.KeepVerb == true;
        }

        return isKeepVerb;
    }

    private bool CheckIsLowerCamelCase(ApiDescriptionSettingsAttribute apiDescriptionSettings, ApiDescriptionSettingsAttribute controllerApiDescriptionSettings)
    {
        bool isLowerCamelCase;

        if (apiDescriptionSettings?.AsLowerCamelCase != null)
        {
            var canParse = bool.TryParse(apiDescriptionSettings.AsLowerCamelCase.ToString(), out var value);
            isLowerCamelCase = canParse && value;
        }
        else if (controllerApiDescriptionSettings?.AsLowerCamelCase != null)
        {
            var canParse = bool.TryParse(controllerApiDescriptionSettings.AsLowerCamelCase.ToString(), out var value);
            isLowerCamelCase = canParse && value;
        }
        else
        {
            isLowerCamelCase = _settings?.AsLowerCamelCase == true;
        }

        return isLowerCamelCase;
    }

    private static bool CheckIsSplitCamelCase(ApiDescriptionSettingsAttribute apiDescriptionSettings, ApiDescriptionSettingsAttribute controllerApiDescriptionSettings)
    {
        bool isSplitCamelCase;

        if (apiDescriptionSettings?.SplitCamelCase != null)
        {
            var canParse = bool.TryParse(apiDescriptionSettings.SplitCamelCase.ToString(), out var value);
            isSplitCamelCase = !canParse || value;
        }
        else if (controllerApiDescriptionSettings?.SplitCamelCase != null)
        {
            var canParse = bool.TryParse(controllerApiDescriptionSettings.SplitCamelCase.ToString(), out var value);
            isSplitCamelCase = !canParse || value;
        }
        else
        {
            isSplitCamelCase = true;
        }

        return isSplitCamelCase;
    }

    private bool CheckIsLowercaseRoute(ApiDescriptionSettingsAttribute apiDescriptionSettings, ApiDescriptionSettingsAttribute controllerApiDescriptionSettings)
    {
        bool isLowercaseRoute;

        if (apiDescriptionSettings?.LowercaseRoute != null)
        {
            var canParse = bool.TryParse(apiDescriptionSettings.LowercaseRoute.ToString(), out var value);
            isLowercaseRoute = !canParse || value;
        }
        else if (controllerApiDescriptionSettings?.LowercaseRoute != null)
        {
            var canParse = bool.TryParse(controllerApiDescriptionSettings.LowercaseRoute.ToString(), out var value);
            isLowercaseRoute = !canParse || value;
        }
        else
        {
            isLowercaseRoute = (_settings?.LowercaseRoute) != false;
        }

        return isLowercaseRoute;
    }

    private static (string name, string version) ResolveNameVersion(string name)
    {
        if (!NameVersionRegex.IsMatch(name)) return (name, default);

        var version = NameVersionRegex.Match(name).Groups["version"].Value.Replace("_", ".");
        return (NameVersionRegex.Replace(name, ""), version);
    }
}
