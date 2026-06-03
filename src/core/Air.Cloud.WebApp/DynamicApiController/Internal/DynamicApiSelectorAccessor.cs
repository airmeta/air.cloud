using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Air.Cloud.WebApp.DynamicApiController.Internal;

internal static class DynamicApiSelectorAccessor
{
    public static SelectorModel GetFirstSelector(ControllerModel controller)
    {
        return controller.Selectors.Count == 0 ? null : controller.Selectors[0];
    }

    public static SelectorModel GetFirstSelector(ActionModel action)
    {
        return action.Selectors.Count == 0 ? null : action.Selectors[0];
    }
}
