using Air.Cloud.Core.Extensions;
using Air.Cloud.Core.Modules.DynamicApp.Internal;
using Air.Cloud.WebApp.DynamicApiController.Options;

using Microsoft.Extensions.Options;

namespace Air.Cloud.WebApp.DynamicApiController.Internal;

internal sealed class DynamicApiVerbMap
{
    private readonly DynamicApiControllerSettingsOptions _settings;

    public DynamicApiVerbMap(IOptions<DynamicApiControllerSettingsOptions> settings)
        : this(settings.Value)
    {
    }

    public DynamicApiVerbMap(DynamicApiControllerSettingsOptions settings)
    {
        _settings = settings;
        VerbToHttpMethods = LoadVerbToHttpMethodsConfigure();
    }

    public IReadOnlyDictionary<string, string> VerbToHttpMethods { get; }

    public bool Contains(string verbKey)
    {
        return VerbToHttpMethods.ContainsKey(verbKey);
    }

    public string ResolveHttpMethod(string methodName)
    {
        var verbKey = ResolveVerbKey(methodName);
        return VerbToHttpMethods.TryGetValue(verbKey, out var verb)
            ? verb ?? _settings.DefaultHttpMethod.ToUpper()
            : _settings.DefaultHttpMethod.ToUpper();
    }

    public string RemoveVerbPrefix(string name)
    {
        var words = name.SplitCamelCase();
        var verbKey = words.First().ToLower();

        if (words.Length > 1 && Contains((words[0] + words[1]).ToLower()))
        {
            return name[(words[0] + words[1]).Length..];
        }

        return Contains(verbKey) ? name[verbKey.Length..] : name;
    }

    private string ResolveVerbKey(string methodName)
    {
        var words = methodName.SplitCamelCase();
        var verbKey = words.First().ToLower();

        if (words.Length > 1 && Contains((words[0] + words[1]).ToLower()))
        {
            verbKey = (words[0] + words[1]).ToLower();
        }

        return verbKey;
    }

    private IReadOnlyDictionary<string, string> LoadVerbToHttpMethodsConfigure()
    {
        var mergedVerbToHttpMethods = Penetrates.VerbToHttpMethods.ToDictionary(
            item => item.Key,
            item => item.Value,
            StringComparer.OrdinalIgnoreCase);

        var verbToHttpMethods = _settings.VerbToHttpMethods;
        if (verbToHttpMethods is null)
        {
            return mergedVerbToHttpMethods;
        }

        foreach (var verbToHttpMethod in verbToHttpMethods.Where(u => u?.Length > 1))
        {
            var verb = verbToHttpMethod[0]?.ToString()?.Trim().ToLowerInvariant();
            var httpMethod = verbToHttpMethod[1]?.ToString()?.Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(verb) || string.IsNullOrWhiteSpace(httpMethod))
            {
                continue;
            }

            mergedVerbToHttpMethods[verb] = httpMethod;
        }

        return mergedVerbToHttpMethods;
    }
}
