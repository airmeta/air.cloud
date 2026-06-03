namespace Air.Cloud.WebApp.DynamicApiController.Internal;

internal readonly record struct DynamicApiNameResult(
    string Name,
    bool IsLowercaseRoute,
    bool IsKeepName,
    bool IsLowerCamelCase);
