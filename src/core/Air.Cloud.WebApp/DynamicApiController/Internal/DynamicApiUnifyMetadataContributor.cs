using Air.Cloud.Core.Extensions;
using Air.Cloud.WebApp.UnifyResult.Attributes;
using Air.Cloud.WebApp.UnifyResult.Internal;
using Air.Cloud.WebApp.UnifyResult.Options;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.Extensions.Options;

namespace Air.Cloud.WebApp.DynamicApiController.Internal;

internal sealed class DynamicApiUnifyMetadataContributor
{
    private readonly UnifyResultRuntimeOptions _runtimeOptions;

    public DynamicApiUnifyMetadataContributor(IOptions<UnifyResultRuntimeOptions> runtimeOptions)
        : this(runtimeOptions.Value)
    {
    }

    public DynamicApiUnifyMetadataContributor(UnifyResultRuntimeOptions runtimeOptions)
    {
        _runtimeOptions = runtimeOptions;
    }

    public void Configure(ActionModel action)
    {
        if (UnifyResultPolicy.IsSucceededSkip(action.ActionMethod, _runtimeOptions)) return;

        var returnType = action.ActionMethod.GetRealReturnType();
        if (returnType == typeof(void)) return;

        action.Filters.Add(new UnifyResultAttribute(returnType, StatusCodes.Status200OK));
    }
}
