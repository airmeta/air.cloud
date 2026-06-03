using Air.Cloud.Core.Plugins.APIProbe;
using Air.Cloud.WebApp.DynamicApiController.Attributes;

using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Air.Cloud.WebApp.DynamicApiController.Internal;

internal sealed class DynamicApiProbeMetadataContributor
{
    public void Configure(
        ActionModel action,
        ApiDescriptionSettingsAttribute actionApiDescriptionSettings,
        ApiDescriptionSettingsAttribute controllerApiDescriptionSettings)
    {
        var metadata = CreateMetadata(actionApiDescriptionSettings, controllerApiDescriptionSettings);
        if (metadata == null) return;

        var selectorModel = DynamicApiSelectorAccessor.GetFirstSelector(action);
        if (selectorModel == null) return;

        selectorModel.EndpointMetadata.Add(metadata);
    }

    private static APIProbeEndpointMetadata CreateMetadata(
        ApiDescriptionSettingsAttribute actionApiDescriptionSettings,
        ApiDescriptionSettingsAttribute controllerApiDescriptionSettings)
    {
        var groupName = actionApiDescriptionSettings?.GroupName ?? controllerApiDescriptionSettings?.GroupName;
        var groups = actionApiDescriptionSettings?.Groups ?? controllerApiDescriptionSettings?.Groups;
        var tag = actionApiDescriptionSettings?.Tag ?? controllerApiDescriptionSettings?.Tag;
        var description = actionApiDescriptionSettings?.Description ?? controllerApiDescriptionSettings?.Description;
        var order = actionApiDescriptionSettings?.Order ?? controllerApiDescriptionSettings?.Order ?? 0;

        if (string.IsNullOrWhiteSpace(groupName)
            && (groups == null || groups.Length == 0)
            && string.IsNullOrWhiteSpace(tag)
            && string.IsNullOrWhiteSpace(description)
            && order == 0)
        {
            return null;
        }

        return new APIProbeEndpointMetadata
        {
            GroupName = groupName,
            Groups = groups,
            Tag = tag,
            Summary = description,
            Description = description,
            Order = order
        };
    }
}
