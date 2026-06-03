using Air.Cloud.Core.Extensions;
using Air.Cloud.WebApp.DynamicApiController.Attributes;
using Air.Cloud.WebApp.DynamicApiController.Enums;
using Air.Cloud.WebApp.DynamicApiController.Options;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;

using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Air.Cloud.WebApp.DynamicApiController.Internal;

internal sealed class DynamicApiRouteBuilder
{
    private readonly DynamicApiControllerSettingsOptions _settings;

    public DynamicApiRouteBuilder(IOptions<DynamicApiControllerSettingsOptions> settings)
        : this(settings.Value)
    {
    }

    public DynamicApiRouteBuilder(DynamicApiControllerSettingsOptions settings)
    {
        _settings = settings;
    }

    private ConcurrentBag<Type> ForceWithDefaultPrefixRouteControllerTypes { get; } = new ConcurrentBag<Type>();

    public void ConfigureControllerRoute(ControllerModel controller, ApiDescriptionSettingsAttribute controllerApiDescriptionSettings)
    {
        if (CheckIsForceWithDefaultRoute(controllerApiDescriptionSettings)
            && !string.IsNullOrWhiteSpace(_settings.DefaultRoutePrefix)
            && DynamicApiSelectorAccessor.GetFirstSelector(controller)?.AttributeRouteModel != null
            && !ForceWithDefaultPrefixRouteControllerTypes.Contains(controller.ControllerType))
        {
            var selectorModel = DynamicApiSelectorAccessor.GetFirstSelector(controller);
            selectorModel.AttributeRouteModel = AttributeRouteModel.CombineAttributeRouteModel(
                new AttributeRouteModel(new RouteAttribute(_settings.DefaultRoutePrefix)),
                selectorModel.AttributeRouteModel);
            ForceWithDefaultPrefixRouteControllerTypes.Add(controller.ControllerType);
        }
    }

    public void ConfigureActionRoute(
        ActionModel action,
        ApiDescriptionSettingsAttribute apiDescriptionSettings,
        ApiDescriptionSettingsAttribute controllerApiDescriptionSettings,
        DynamicApiNameResult nameResult,
        bool hasApiControllerAttribute)
    {
        var selectorModel = DynamicApiSelectorAccessor.GetFirstSelector(action);
        if (selectorModel == null) return;

        if (selectorModel.AttributeRouteModel != null) return;

        var module = apiDescriptionSettings?.Module;

        string template;
        string controllerRouteTemplate = null;
        if (action.ActionName.Length == 0 && !nameResult.IsKeepName && action.Parameters.Count == 0)
        {
            template = GenerateControllerRouteTemplate(action.Controller, controllerApiDescriptionSettings);
        }
        else
        {
            var parameterRouteTemplate = GenerateParameterRouteTemplates(action, nameResult.IsLowercaseRoute, nameResult.IsLowerCamelCase, hasApiControllerAttribute);
            controllerRouteTemplate = GenerateControllerRouteTemplate(action.Controller, controllerApiDescriptionSettings, parameterRouteTemplate);
            var actionStartTemplate = parameterRouteTemplate != null ? parameterRouteTemplate.ActionStartTemplates.Count == 0 ? null : string.Join("/", parameterRouteTemplate.ActionStartTemplates) : null;
            var actionEndTemplate = parameterRouteTemplate != null ? parameterRouteTemplate.ActionEndTemplates.Count == 0 ? null : string.Join("/", parameterRouteTemplate.ActionEndTemplates) : null;

            template = string.IsNullOrWhiteSpace(controllerRouteTemplate)
                 ? $"{(string.IsNullOrWhiteSpace(module) ? "/" : $"{module}/")}{actionStartTemplate}/{(string.IsNullOrWhiteSpace(action.ActionName) ? null : "[action]")}/{actionEndTemplate}"
                 : $"{controllerRouteTemplate}/{(string.IsNullOrWhiteSpace(module) ? null : $"{module}/")}{actionStartTemplate}/{(string.IsNullOrWhiteSpace(action.ActionName) ? null : "[action]")}/{actionEndTemplate}";
        }

        AttributeRouteModel actionAttributeRouteModel = null;
        if (!string.IsNullOrWhiteSpace(template))
        {
            template = Regex.Replace(nameResult.IsLowercaseRoute ? template.ToLower() : nameResult.IsLowerCamelCase ? template.ToLowerCamelCase() : template, @"\/{2,}", "/");
            actionAttributeRouteModel = string.IsNullOrWhiteSpace(template) ? null : new AttributeRouteModel(new RouteAttribute(template));
        }

        selectorModel.AttributeRouteModel = string.IsNullOrWhiteSpace(controllerRouteTemplate)
            ? actionAttributeRouteModel == null ? null : AttributeRouteModel.CombineAttributeRouteModel(DynamicApiSelectorAccessor.GetFirstSelector(action.Controller)?.AttributeRouteModel, actionAttributeRouteModel)
            : actionAttributeRouteModel;
    }

    private string GenerateControllerRouteTemplate(ControllerModel controller, ApiDescriptionSettingsAttribute apiDescriptionSettings, ParameterRouteTemplate parameterRouteTemplate = default)
    {
        var selectorModel = DynamicApiSelectorAccessor.GetFirstSelector(controller);
        if (selectorModel == null) return default;

        if (selectorModel.AttributeRouteModel != null) return default;

        var module = apiDescriptionSettings?.Module ?? _settings.DefaultModule;
        var routePrefix = _settings.DefaultRoutePrefix;

        if (parameterRouteTemplate == null || parameterRouteTemplate.ControllerStartTemplates.Count == 0 && parameterRouteTemplate.ControllerEndTemplates.Count == 0)
        {
            return $"{(string.IsNullOrWhiteSpace(routePrefix) ? null : $"{routePrefix}/")}{(string.IsNullOrWhiteSpace(module) ? null : $"{module}/")}[controller]";
        }

        var controllerStartTemplate = parameterRouteTemplate.ControllerStartTemplates.Count == 0 ? null : string.Join("/", parameterRouteTemplate.ControllerStartTemplates);
        var controllerEndTemplate = parameterRouteTemplate.ControllerEndTemplates.Count == 0 ? null : string.Join("/", parameterRouteTemplate.ControllerEndTemplates);
        var template = $"{(string.IsNullOrWhiteSpace(routePrefix) ? null : $"{routePrefix}/")}{(string.IsNullOrWhiteSpace(module) ? null : $"{module}/")}{controllerStartTemplate}/[controller]/{controllerEndTemplate}";

        return template;
    }

    private ParameterRouteTemplate GenerateParameterRouteTemplates(ActionModel action, bool isLowercaseRoute, bool isLowerCamelCase, bool hasApiControllerAttribute)
    {
        if (action.Parameters.Count == 0) return default;

        var parameterRouteTemplate = new ParameterRouteTemplate();
        var parameters = action.Parameters;
        var isQueryParametersAction = action.Attributes.Any(u => u is QueryParametersAttribute);

        foreach (var parameterModel in parameters)
        {
            var parameterType = parameterModel.ParameterType;
            var parameterAttributes = parameterModel.Attributes;

            if (isLowercaseRoute) parameterModel.ParameterName = parameterModel.ParameterName.ToLower();

            if (isLowerCamelCase) parameterModel.ParameterName = parameterModel.ParameterName.ToLowerCamelCase();

            var hasFormAttribute = parameterAttributes.Any(u => typeof(IBindingSourceMetadata).IsAssignableFrom(u.GetType()));

            if (isQueryParametersAction && !hasFormAttribute)
            {
                parameterModel.BindingInfo = BindingInfo.GetBindingInfo(new[] { new FromQueryAttribute() });
                continue;
            }

            if (!parameterAttributes.Any(u => u is FromRouteAttribute)
                && (!parameterType.IsRichPrimitive() || hasFormAttribute)) continue;

            if (_settings?.UrlParameterization == true || parameterType.IsArray)
            {
                parameterModel.BindingInfo = BindingInfo.GetBindingInfo(new[] { new FromQueryAttribute() });
                continue;
            }

            if (!hasFormAttribute && hasApiControllerAttribute) continue;

            var canBeNull = parameterType.IsGenericType && parameterType.GetGenericTypeDefinition() == typeof(Nullable<>);

            string constraint = default;
            if (parameterAttributes.FirstOrDefault(u => u is RouteConstraintAttribute) is RouteConstraintAttribute routeConstraint && !string.IsNullOrWhiteSpace(routeConstraint.Constraint))
            {
                constraint = !routeConstraint.Constraint.StartsWith(":")
                    ? $":{routeConstraint.Constraint}" : routeConstraint.Constraint;
            }

            var template = $"{{{parameterModel.ParameterName}{(canBeNull ? "?" : string.Empty)}{constraint}}}";
            if (parameterAttributes.FirstOrDefault(u => u is ApiSeatAttribute) is not ApiSeatAttribute apiSeat)
            {
                parameterRouteTemplate.ActionEndTemplates.Add(template);
                continue;
            }

            switch (apiSeat.Seat)
            {
                case ApiSeats.ControllerStart:
                    parameterRouteTemplate.ControllerStartTemplates.Add(template);
                    break;
                case ApiSeats.ControllerEnd:
                    parameterRouteTemplate.ControllerEndTemplates.Add(template);
                    break;
                case ApiSeats.ActionStart:
                    parameterRouteTemplate.ActionStartTemplates.Add(template);
                    break;
                case ApiSeats.ActionEnd:
                    parameterRouteTemplate.ActionEndTemplates.Add(template);
                    break;
                default:
                    break;
            }
        }

        return parameterRouteTemplate;
    }

    private bool CheckIsForceWithDefaultRoute(ApiDescriptionSettingsAttribute controllerApiDescriptionSettings)
    {
        bool isForceWithRoutePrefix;

        if (controllerApiDescriptionSettings?.ForceWithRoutePrefix != null)
        {
            var canParse = bool.TryParse(controllerApiDescriptionSettings.ForceWithRoutePrefix.ToString(), out var value);
            isForceWithRoutePrefix = canParse && value;
        }
        else
        {
            isForceWithRoutePrefix = _settings?.ForceWithRoutePrefix == true;
        }

        return isForceWithRoutePrefix;
    }
}
