using Air.Cloud.Core.Extensions;
using Air.Cloud.WebApp.DynamicApiController.Options;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;

namespace Air.Cloud.WebApp.DynamicApiController.Internal;

internal sealed class DynamicApiParameterBinder
{
    private readonly DynamicApiControllerSettingsOptions _settings;

    public DynamicApiParameterBinder(IOptions<DynamicApiControllerSettingsOptions> settings)
        : this(settings.Value)
    {
    }

    public DynamicApiParameterBinder(DynamicApiControllerSettingsOptions settings)
    {
        _settings = settings;
    }

    public void ConfigureClassTypeParameter(ActionModel action)
    {
        if (action.Parameters.Count == 0) return;

        if (_settings.ModelToQuery.Value)
        {
            var httpMethods = action.Selectors
                .SelectMany(u => u.ActionConstraints.Where(u => u is HttpMethodActionConstraint)
                    .SelectMany(u => (u as HttpMethodActionConstraint).HttpMethods));

            if (httpMethods.All(u => u.Equals("GET") || u.Equals("HEAD"))) return;
        }

        var parameters = action.Parameters;
        foreach (var parameterModel in parameters)
        {
            if (parameterModel.BindingInfo != null) continue;

            var parameterType = parameterModel.ParameterType;
            if (parameterType.IsRichPrimitive()) continue;

            if (typeof(IFormFile).IsAssignableFrom(parameterType) || typeof(IFormFileCollection).IsAssignableFrom(parameterType)) continue;

            parameterModel.BindingInfo = BindingInfo.GetBindingInfo(new[] { new FromBodyAttribute() });
        }
    }
}
