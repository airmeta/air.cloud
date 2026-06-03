using Air.Cloud.WebApp.DynamicApiController.Options;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Options;

namespace Air.Cloud.WebApp.DynamicApiController.Internal;

internal sealed class DynamicApiHttpMethodResolver
{
    private readonly DynamicApiVerbMap _verbMap;

    public DynamicApiHttpMethodResolver(IOptions<DynamicApiControllerSettingsOptions> settings, DynamicApiVerbMap verbMap)
        : this(settings.Value, verbMap)
    {
    }

    public DynamicApiHttpMethodResolver(DynamicApiControllerSettingsOptions settings, DynamicApiVerbMap verbMap)
    {
        _verbMap = verbMap;
    }

    public void Configure(ActionModel action)
    {
        var selectorModel = DynamicApiSelectorAccessor.GetFirstSelector(action);
        if (selectorModel == null) return;

        if (selectorModel.ActionConstraints.Count > 0) return;

        var verb = _verbMap.ResolveHttpMethod(action.ActionMethod.Name);
        selectorModel.ActionConstraints.Add(new HttpMethodActionConstraint(new[] { verb }));

        HttpMethodAttribute httpMethodAttribute = verb switch
        {
            "GET" => new HttpGetAttribute(),
            "POST" => new HttpPostAttribute(),
            "PUT" => new HttpPutAttribute(),
            "DELETE" => new HttpDeleteAttribute(),
            "PATCH" => new HttpPatchAttribute(),
            "HEAD" => new HttpHeadAttribute(),
            _ => throw new NotSupportedException($"{verb}")
        };

        selectorModel.EndpointMetadata.Add(httpMethodAttribute);
    }
}
