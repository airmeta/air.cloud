using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using System.Reflection;
using System.Reflection.Emit;

namespace Air.Cloud.UnitTest.Core.WebApp;

public class WebAppReflectionTests
{
    [Fact]
    public void AddWebAppCore_should_register_core_filters_and_dynamic_api_without_unify_provider()
    {
        ResetUnifyContext();
        var services = new ServiceCollection();
        var mvcBuilder = services.AddControllers();

        InvokeMvcExtension("AddWebAppCore", mvcBuilder);

        Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == GetWebAppType("Air.Cloud.WebApp.UnifyResult.Providers.IUnifyResultProvider"));
        Assert.False(GetUnifyEnabled(services));

        var mvcOptions = BuildMvcOptions(services);
        Assert.Contains(mvcOptions.Conventions, IsDynamicApiConvention);
        AssertContainsFilter(mvcOptions, "Air.Cloud.WebApp.DataValidation.Filters.DataValidationFilter");
        AssertContainsFilter(mvcOptions, "Air.Cloud.WebApp.FriendlyException.Filters.FriendlyExceptionFilter");
        AssertDoesNotContainFilter(mvcOptions, "Air.Cloud.WebApp.UnifyResult.Filters.SucceededUnifyResultFilter");
    }

    [Fact]
    public void AddWebAppUnifyResult_should_register_default_provider_and_enable_unify_only()
    {
        ResetUnifyContext();
        var services = new ServiceCollection();
        var mvcBuilder = services.AddControllers();

        InvokeMvcExtension("AddWebAppUnifyResult", mvcBuilder);

        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == GetWebAppType("Air.Cloud.WebApp.UnifyResult.Providers.IUnifyResultProvider")
            && descriptor.ImplementationType == GetWebAppType("Air.Cloud.WebApp.UnifyResult.Providers.RESTfulResultProvider")
            && descriptor.Lifetime == ServiceLifetime.Transient);
        Assert.True(GetUnifyEnabled(services));
        Assert.Equal(GetWebAppType("Air.Cloud.WebApp.UnifyResult.Internal.RESTfulResult`1"), GetRestfulResultType(services));

        var mvcOptions = BuildMvcOptions(services);
        AssertContainsFilter(mvcOptions, "Air.Cloud.WebApp.UnifyResult.Filters.SucceededUnifyResultFilter");
        Assert.Contains(mvcOptions.Conventions, IsUnifyResultConvention);
        Assert.DoesNotContain(mvcOptions.Conventions, IsDynamicApiConvention);
        AssertDoesNotContainFilter(mvcOptions, "Air.Cloud.WebApp.DataValidation.Filters.DataValidationFilter");
        AssertDoesNotContainFilter(mvcOptions, "Air.Cloud.WebApp.FriendlyException.Filters.FriendlyExceptionFilter");
    }

    [Fact]
    public void AddWebApp_should_register_core_capabilities_and_default_unify_result()
    {
        ResetUnifyContext();
        var services = new ServiceCollection();
        var mvcBuilder = services.AddControllers();

        InvokeMvcExtension("AddWebApp", mvcBuilder);

        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == GetWebAppType("Air.Cloud.WebApp.UnifyResult.Providers.IUnifyResultProvider")
            && descriptor.ImplementationType == GetWebAppType("Air.Cloud.WebApp.UnifyResult.Providers.RESTfulResultProvider"));
        Assert.True(GetUnifyEnabled(services));

        var mvcOptions = BuildMvcOptions(services);
        Assert.Contains(mvcOptions.Conventions, IsDynamicApiConvention);
        AssertContainsConventionAfter(mvcOptions, IsDynamicApiConvention, IsUnifyResultConvention);
        AssertContainsFilter(mvcOptions, "Air.Cloud.WebApp.DataValidation.Filters.DataValidationFilter");
        AssertContainsFilter(mvcOptions, "Air.Cloud.WebApp.FriendlyException.Filters.FriendlyExceptionFilter");
        AssertContainsFilter(mvcOptions, "Air.Cloud.WebApp.UnifyResult.Filters.SucceededUnifyResultFilter");
    }

    [Fact]
    public void AddWebAppUnifyResult_before_core_should_keep_unify_convention_after_dynamic_api()
    {
        var services = new ServiceCollection();
        var mvcBuilder = services.AddControllers();

        InvokeMvcExtension("AddWebAppUnifyResult", mvcBuilder);
        InvokeMvcExtension("AddWebAppCore", mvcBuilder);

        var mvcOptions = BuildMvcOptions(services);
        AssertContainsConventionAfter(mvcOptions, IsDynamicApiConvention, IsUnifyResultConvention);
    }

    [Fact]
    public void AddWebAppUnifyResult_should_register_unify_convention_as_post_configure_only()
    {
        var services = new ServiceCollection();
        var mvcBuilder = services.AddControllers();

        InvokeMvcExtension("AddWebAppUnifyResult", mvcBuilder);

        var setupType = GetWebAppType("Air.Cloud.WebApp.UnifyResult.Conventions.UnifyResultMvcOptionsSetup");
        Assert.DoesNotContain(services, descriptor =>
            descriptor.ServiceType == typeof(IConfigureOptions<MvcOptions>)
            && descriptor.ImplementationType == setupType);
        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(IPostConfigureOptions<MvcOptions>)
            && descriptor.ImplementationType == setupType
            && descriptor.Lifetime == ServiceLifetime.Transient);
    }

    [Fact]
    public void AddWebAppCore_should_not_add_unify_convention_when_unify_is_disabled()
    {
        var services = new ServiceCollection();
        var mvcBuilder = services.AddControllers();

        InvokeMvcExtension("AddWebAppCore", mvcBuilder);

        var mvcOptions = BuildMvcOptions(services);
        Assert.Contains(mvcOptions.Conventions, IsDynamicApiConvention);
        Assert.DoesNotContain(mvcOptions.Conventions, IsUnifyResultConvention);
    }

    [Fact]
    public void AddWebApp_with_custom_provider_should_register_provider_and_custom_result_model()
    {
        ResetUnifyContext();
        var services = new ServiceCollection();
        var mvcBuilder = services.AddControllers();
        var providerType = CreateDynamicUnifyProviderType(includeUnifyModelAttribute: true);

        InvokeMvcExtension("AddWebApp", mvcBuilder, providerType);

        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == GetWebAppType("Air.Cloud.WebApp.UnifyResult.Providers.IUnifyResultProvider")
            && descriptor.ImplementationType == providerType
            && descriptor.Lifetime == ServiceLifetime.Transient);
        Assert.True(GetUnifyEnabled(services));
        Assert.Equal(typeof(TestApiResult<>), GetRestfulResultType(services));
    }

    [Fact]
    public void AddWebAppUnifyResult_should_throw_clear_exception_when_provider_has_no_unify_model()
    {
        ResetUnifyContext();
        var services = new ServiceCollection();
        var mvcBuilder = services.AddControllers();
        var providerType = CreateDynamicUnifyProviderType(includeUnifyModelAttribute: false);

        var exception = Assert.Throws<TargetInvocationException>(() =>
            InvokeMvcExtension("AddWebAppUnifyResult", mvcBuilder, providerType));
        var innerException = Assert.IsType<InvalidOperationException>(exception.InnerException);

        Assert.Contains("UnifyModelAttribute", innerException.Message);
        Assert.Contains(providerType.FullName!, innerException.Message);
    }

    [Fact]
    public void AddWebAppUnifyResult_should_throw_clear_exception_when_provider_model_is_not_open_single_generic()
    {
        var invalidModelTypes = new[]
        {
            typeof(NonGenericApiResult),
            typeof(TestApiResult<string>),
            typeof(TwoGenericApiResult<,>)
        };

        foreach (var invalidModelType in invalidModelTypes)
        {
            var services = new ServiceCollection();
            var mvcBuilder = services.AddControllers();
            var providerType = CreateDynamicUnifyProviderType(includeUnifyModelAttribute: true, invalidModelType);

            var exception = Assert.Throws<TargetInvocationException>(() =>
                InvokeMvcExtension("AddWebAppUnifyResult", mvcBuilder, providerType));
            var innerException = Assert.IsType<InvalidOperationException>(exception.InnerException);

            Assert.Contains(providerType.FullName!, innerException.Message);
            Assert.Contains(invalidModelType.FullName!, innerException.Message);
            Assert.Contains("open generic type definition", innerException.Message);
            Assert.DoesNotContain(services, descriptor =>
                descriptor.ServiceType == GetWebAppType("Air.Cloud.WebApp.UnifyResult.Providers.IUnifyResultProvider"));
            Assert.DoesNotContain(mvcBuilder.Services, descriptor =>
                IsFilterServiceDescriptor(descriptor, "Air.Cloud.WebApp.UnifyResult.Filters.SucceededUnifyResultFilter"));
        }
    }

    [Fact]
    public void AddWebAppUnifyResult_should_accept_open_single_generic_model_type()
    {
        var services = new ServiceCollection();
        var mvcBuilder = services.AddControllers();
        var providerType = CreateDynamicUnifyProviderType(includeUnifyModelAttribute: true, typeof(TestApiResult<>));

        InvokeMvcExtension("AddWebAppUnifyResult", mvcBuilder, providerType);

        Assert.Equal(typeof(TestApiResult<>), GetRestfulResultType(services));
        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == GetWebAppType("Air.Cloud.WebApp.UnifyResult.Providers.IUnifyResultProvider")
            && descriptor.ImplementationType == providerType
            && descriptor.Lifetime == ServiceLifetime.Transient);
    }

    [Fact]
    public void AddWebAppUnifyResult_should_keep_runtime_options_per_service_provider()
    {
        var customServices = new ServiceCollection();
        var customMvcBuilder = customServices.AddControllers();
        var customProviderType = CreateDynamicUnifyProviderType(includeUnifyModelAttribute: true);

        InvokeMvcExtension("AddWebAppUnifyResult", customMvcBuilder, customProviderType);

        var defaultServices = new ServiceCollection();
        var defaultMvcBuilder = defaultServices.AddControllers();

        InvokeMvcExtension("AddWebAppUnifyResult", defaultMvcBuilder);

        Assert.Equal(typeof(TestApiResult<>), GetRestfulResultType(customServices));
        Assert.Equal(GetWebAppType("Air.Cloud.WebApp.UnifyResult.Internal.RESTfulResult`1"), GetRestfulResultType(defaultServices));
    }

    [Fact]
    public void WebAppAssembly_should_not_reference_swagger_or_openapi_packages()
    {
        var referencedAssemblyNames = LoadWebAppAssembly().GetReferencedAssemblies()
            .Select(assembly => assembly.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToArray();

        Assert.DoesNotContain(referencedAssemblyNames, name => name!.Contains("Swashbuckle", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(referencedAssemblyNames, name => name!.Contains("Swagger", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(referencedAssemblyNames, name => name!.Contains("OpenApi", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void UnifyResultAttribute_should_wrap_boolean_result_model()
    {
        var attributeType = GetWebAppType("Air.Cloud.WebApp.UnifyResult.Attributes.UnifyResultAttribute");
        var restfulResultType = GetWebAppType("Air.Cloud.WebApp.UnifyResult.Internal.RESTfulResult`1");
        var attribute = Activator.CreateInstance(attributeType, typeof(bool), StatusCodes.Status200OK)!;

        InvokeInstance(attribute, "ConfigureResultModel", restfulResultType);

        Assert.Equal(restfulResultType.MakeGenericType(typeof(bool)), GetProperty<Type>(attribute, "Type"));
    }

    [Fact]
    public void UnifyResultAttribute_should_clear_type_when_result_model_is_already_wrapped()
    {
        var attributeType = GetWebAppType("Air.Cloud.WebApp.UnifyResult.Attributes.UnifyResultAttribute");
        var restfulResultType = GetWebAppType("Air.Cloud.WebApp.UnifyResult.Internal.RESTfulResult`1");
        var attribute = Activator.CreateInstance(attributeType, restfulResultType.MakeGenericType(typeof(string)), StatusCodes.Status200OK)!;

        InvokeInstance(attribute, "ConfigureResultModel", restfulResultType);

        Assert.Null(GetProperty<Type>(attribute, "Type"));
    }

    [Fact]
    public void DynamicApiConvention_should_merge_custom_verbs_without_mutating_global_verb_map()
    {
        var customVerb = $"codexverb{Guid.NewGuid():N}".ToLowerInvariant();
        var globalVerbMap = GetGlobalVerbToHttpMethods();
        globalVerbMap.TryRemove(customVerb, out _);

        try
        {
            var convention = CreateDynamicApiControllerConvention(new object[][]
            {
                new object[] { customVerb, "PATCH" }
            });
            var conventionVerbMap = GetConventionVerbToHttpMethods(convention);

            Assert.Equal("PATCH", conventionVerbMap[customVerb]);
            Assert.False(globalVerbMap.ContainsKey(customVerb));
        }
        finally
        {
            globalVerbMap.TryRemove(customVerb, out _);
        }
    }

    [Fact]
    public void DynamicApiConvention_should_keep_custom_verbs_per_convention_instance()
    {
        var customVerb = $"codexverb{Guid.NewGuid():N}".ToLowerInvariant();
        var globalVerbMap = GetGlobalVerbToHttpMethods();
        globalVerbMap.TryRemove(customVerb, out _);

        try
        {
            var customConvention = CreateDynamicApiControllerConvention(new object[][]
            {
                new object[] { customVerb, "PUT" }
            });
            var defaultConvention = CreateDynamicApiControllerConvention();

            Assert.Equal("PUT", GetConventionVerbToHttpMethods(customConvention)[customVerb]);
            Assert.False(GetConventionVerbToHttpMethods(defaultConvention).ContainsKey(customVerb));
            Assert.False(globalVerbMap.ContainsKey(customVerb));
        }
        finally
        {
            globalVerbMap.TryRemove(customVerb, out _);
        }
    }

    [Fact]
    public void DynamicApiConvention_should_delegate_to_internal_components()
    {
        var convention = CreateDynamicApiControllerConvention();
        var componentFieldNames = new[]
        {
            "_nameResolver",
            "_httpMethodResolver",
            "_parameterBinder",
            "_routeBuilder",
            "_unifyMetadataContributor",
            "_apiProbeMetadataContributor"
        };
        var componentTypeNames = new[]
        {
            "Air.Cloud.WebApp.DynamicApiController.Internal.DynamicApiVerbMap",
            "Air.Cloud.WebApp.DynamicApiController.Internal.DynamicApiNameResolver",
            "Air.Cloud.WebApp.DynamicApiController.Internal.DynamicApiHttpMethodResolver",
            "Air.Cloud.WebApp.DynamicApiController.Internal.DynamicApiParameterBinder",
            "Air.Cloud.WebApp.DynamicApiController.Internal.DynamicApiRouteBuilder",
            "Air.Cloud.WebApp.DynamicApiController.Internal.DynamicApiUnifyMetadataContributor",
            "Air.Cloud.WebApp.DynamicApiController.Internal.DynamicApiProbeMetadataContributor"
        };

        foreach (var componentTypeName in componentTypeNames)
        {
            Assert.NotNull(GetWebAppType(componentTypeName));
        }

        foreach (var componentFieldName in componentFieldNames)
        {
            Assert.NotNull(convention.GetType().GetField(componentFieldName, BindingFlags.Instance | BindingFlags.NonPublic));
        }
    }

    [Fact]
    public void AddWebAppCore_should_register_dynamic_api_components_for_di_composition()
    {
        var services = new ServiceCollection();
        var mvcBuilder = services.AddControllers();

        InvokeMvcExtension("AddWebAppCore", mvcBuilder);

        Assert.Contains(services, descriptor => descriptor.ImplementationType == GetWebAppType("Air.Cloud.WebApp.DynamicApiController.Internal.DynamicApiVerbMap"));
        Assert.Contains(services, descriptor => descriptor.ImplementationType == GetWebAppType("Air.Cloud.WebApp.DynamicApiController.Internal.DynamicApiNameResolver"));
        Assert.Contains(services, descriptor => descriptor.ImplementationType == GetWebAppType("Air.Cloud.WebApp.DynamicApiController.Internal.DynamicApiHttpMethodResolver"));
        Assert.Contains(services, descriptor => descriptor.ImplementationType == GetWebAppType("Air.Cloud.WebApp.DynamicApiController.Internal.DynamicApiParameterBinder"));
        Assert.Contains(services, descriptor => descriptor.ImplementationType == GetWebAppType("Air.Cloud.WebApp.DynamicApiController.Internal.DynamicApiRouteBuilder"));
        Assert.Contains(services, descriptor => descriptor.ImplementationType == GetWebAppType("Air.Cloud.WebApp.DynamicApiController.Internal.DynamicApiUnifyMetadataContributor"));
        Assert.Contains(services, descriptor => descriptor.ImplementationType == GetWebAppType("Air.Cloud.WebApp.DynamicApiController.Internal.DynamicApiProbeMetadataContributor"));
        Assert.Contains(services, descriptor => descriptor.ImplementationType == GetWebAppType("Air.Cloud.WebApp.DynamicApiController.Conventions.DynamicApiControllerApplicationModelConvention"));
    }

    [Fact]
    public void DynamicApiConvention_should_write_api_probe_metadata_without_controller_order_global_state()
    {
        var controllerType = CreateDynamicControllerTypeWithApiDescriptionSettings();
        var application = CreateDynamicApiApplicationModel(controllerType, new[] { "GetOrderedAsync" });
        var convention = CreateDynamicApiControllerConvention(configureSettings: settings =>
        {
            SetProperty(settings, "SupportedMvcController", true);
        });
        var controllerOrderCollection = GetControllerOrderCollection();
        controllerOrderCollection.TryRemove("dynamic-api-probe", out _);

        ApplyDynamicApiConvention(convention, application);

        Assert.False(controllerOrderCollection.ContainsKey("dynamic-api-probe"));
        var endpointMetadata = application.Controllers.Single().Actions.Single().Selectors.Single().EndpointMetadata;
        var apiProbeMetadata = Assert.Single(endpointMetadata, metadata =>
            metadata.GetType().FullName == "Air.Cloud.Core.Plugins.APIProbe.APIProbeEndpointMetadata");
        Assert.Equal("ProbeGroup", GetProperty<string>(apiProbeMetadata, "GroupName"));
        Assert.Equal("ProbeTag", GetProperty<string>(apiProbeMetadata, "Tag"));
        Assert.Equal("Probe description", GetProperty<string>(apiProbeMetadata, "Description"));
        Assert.Equal(42, GetProperty<int>(apiProbeMetadata, "Order"));
    }

    [Fact]
    public void DynamicApiConvention_should_prefer_action_api_probe_metadata_over_controller_metadata()
    {
        var controllerType = CreateDynamicControllerTypeWithActionApiDescriptionSettings();
        var application = CreateDynamicApiApplicationModel(controllerType, new[] { "GetOrderedAsync" });
        var convention = CreateDynamicApiControllerConvention(configureSettings: settings =>
        {
            SetProperty(settings, "SupportedMvcController", true);
        });

        ApplyDynamicApiConvention(convention, application);

        var apiProbeMetadata = Assert.Single(application.Controllers.Single().Actions.Single().Selectors.Single().EndpointMetadata, metadata =>
            metadata.GetType().FullName == "Air.Cloud.Core.Plugins.APIProbe.APIProbeEndpointMetadata");
        Assert.Equal("ActionGroup", GetProperty<string>(apiProbeMetadata, "GroupName"));
        Assert.Equal("ActionTag", GetProperty<string>(apiProbeMetadata, "Tag"));
        Assert.Equal("Action description", GetProperty<string>(apiProbeMetadata, "Description"));
        Assert.Equal(7, GetProperty<int>(apiProbeMetadata, "Order"));
    }

    [Fact]
    public void DynamicApiConvention_should_not_write_api_probe_metadata_when_description_settings_absent()
    {
        var application = CreateDynamicApiApplicationModel(typeof(DynamicApiBehaviorController), new[]
        {
            nameof(DynamicApiBehaviorController.GetEnabledAsync)
        });
        var convention = CreateDynamicApiControllerConvention(configureSettings: settings =>
        {
            SetProperty(settings, "SupportedMvcController", true);
        });

        ApplyDynamicApiConvention(convention, application);

        Assert.DoesNotContain(application.Controllers.Single().Actions.Single().Selectors.Single().EndpointMetadata, metadata =>
            metadata.GetType().FullName == "Air.Cloud.Core.Plugins.APIProbe.APIProbeEndpointMetadata");
    }

    [Fact]
    public void DynamicApiConvention_should_infer_http_methods_and_trim_action_names()
    {
        var application = CreateDynamicApiApplicationModel(typeof(DynamicApiBehaviorController), new[]
        {
            nameof(DynamicApiBehaviorController.GetEnabledAsync),
            nameof(DynamicApiBehaviorController.CreateBodyAsync),
            nameof(DynamicApiBehaviorController.UpdateBodyAsync),
            nameof(DynamicApiBehaviorController.DeleteBodyAsync),
            nameof(DynamicApiBehaviorController.PatchBodyAsync)
        });
        var convention = CreateDynamicApiControllerConvention(configureSettings: settings =>
        {
            SetProperty(settings, "SupportedMvcController", true);
        });

        ApplyDynamicApiConvention(convention, application);

        var actions = application.Controllers.Single().Actions.ToDictionary(action => action.ActionMethod.Name);
        Assert.Equal("enabled", actions[nameof(DynamicApiBehaviorController.GetEnabledAsync)].ActionName);
        Assert.Equal("body", actions[nameof(DynamicApiBehaviorController.CreateBodyAsync)].ActionName);
        Assert.Equal("body", actions[nameof(DynamicApiBehaviorController.UpdateBodyAsync)].ActionName);
        Assert.Equal("body", actions[nameof(DynamicApiBehaviorController.DeleteBodyAsync)].ActionName);
        Assert.Equal("body", actions[nameof(DynamicApiBehaviorController.PatchBodyAsync)].ActionName);
        Assert.Equal("GET", GetHttpMethod(actions[nameof(DynamicApiBehaviorController.GetEnabledAsync)]));
        Assert.Equal("POST", GetHttpMethod(actions[nameof(DynamicApiBehaviorController.CreateBodyAsync)]));
        Assert.Equal("PUT", GetHttpMethod(actions[nameof(DynamicApiBehaviorController.UpdateBodyAsync)]));
        Assert.Equal("DELETE", GetHttpMethod(actions[nameof(DynamicApiBehaviorController.DeleteBodyAsync)]));
        Assert.Equal("PATCH", GetHttpMethod(actions[nameof(DynamicApiBehaviorController.PatchBodyAsync)]));
    }

    [Fact]
    public void DynamicApiConvention_should_support_custom_two_word_verb_for_method_and_name()
    {
        var application = CreateDynamicApiApplicationModel(typeof(DynamicApiBehaviorController), new[]
        {
            nameof(DynamicApiBehaviorController.GetListUsersAsync)
        });
        var convention = CreateDynamicApiControllerConvention(
            verbToHttpMethods: new object[][]
            {
                new object[] { "getlist", "GET" }
            },
            configureSettings: settings =>
            {
                SetProperty(settings, "SupportedMvcController", true);
            });

        ApplyDynamicApiConvention(convention, application);

        var action = application.Controllers.Single().Actions.Single();
        Assert.Equal("users", action.ActionName);
        Assert.Equal("GET", GetHttpMethod(action));
    }

    [Fact]
    public void DynamicApiConvention_should_keep_verb_in_action_name_when_configured()
    {
        var application = CreateDynamicApiApplicationModel(typeof(DynamicApiBehaviorController), new[]
        {
            nameof(DynamicApiBehaviorController.GetEnabledAsync)
        });
        var convention = CreateDynamicApiControllerConvention(configureSettings: settings =>
        {
            SetProperty(settings, "SupportedMvcController", true);
            SetProperty(settings, "KeepVerb", true);
        });

        ApplyDynamicApiConvention(convention, application);

        var action = application.Controllers.Single().Actions.Single();
        Assert.Equal("get-enabled", action.ActionName);
        Assert.Equal("GET", GetHttpMethod(action));
    }

    [Fact]
    public void DynamicApiConvention_should_bind_complex_post_parameter_from_body()
    {
        var application = CreateDynamicApiApplicationModel(typeof(DynamicApiBehaviorController), new[]
        {
            nameof(DynamicApiBehaviorController.CreateBodyAsync)
        });
        var convention = CreateDynamicApiControllerConvention(configureSettings: settings =>
        {
            SetProperty(settings, "SupportedMvcController", true);
        });

        ApplyDynamicApiConvention(convention, application);

        var parameter = application.Controllers.Single().Actions.Single().Parameters.Single();
        Assert.Equal(BindingSource.Body, parameter.BindingInfo?.BindingSource);
    }

    [Fact]
    public void DynamicApiConvention_should_not_bind_get_complex_parameter_from_body_when_model_to_query_enabled()
    {
        var application = CreateDynamicApiApplicationModel(typeof(DynamicApiBehaviorController), new[]
        {
            nameof(DynamicApiBehaviorController.GetBodyAsync)
        });
        var convention = CreateDynamicApiControllerConvention(configureSettings: settings =>
        {
            SetProperty(settings, "SupportedMvcController", true);
            SetProperty(settings, "ModelToQuery", true);
        });

        ApplyDynamicApiConvention(convention, application);

        var parameter = application.Controllers.Single().Actions.Single().Parameters.Single();
        Assert.Null(parameter.BindingInfo);
    }

    [Fact]
    public void DynamicApiConvention_should_bind_query_parameters_attribute_parameters_from_query()
    {
        var queryParametersAttribute = Activator.CreateInstance(GetWebAppType("Air.Cloud.WebApp.DynamicApiController.Attributes.QueryParametersAttribute"))!;
        var application = CreateDynamicApiApplicationModel(
            typeof(DynamicApiBehaviorController),
            new[] { nameof(DynamicApiBehaviorController.SearchAsync) },
            actionAttributes: new Dictionary<string, object[]>
            {
                [nameof(DynamicApiBehaviorController.SearchAsync)] = new[] { queryParametersAttribute }
            });
        var convention = CreateDynamicApiControllerConvention(configureSettings: settings =>
        {
            SetProperty(settings, "SupportedMvcController", true);
        });

        ApplyDynamicApiConvention(convention, application);

        var parameters = application.Controllers.Single().Actions.Single().Parameters.ToDictionary(parameter => parameter.ParameterName);
        Assert.Equal(BindingSource.Query, parameters["filter"].BindingInfo?.BindingSource);
        Assert.Equal(BindingSource.Query, parameters["pageindex"].BindingInfo?.BindingSource);
    }

    [Fact]
    public void DynamicApiConvention_should_bind_array_and_url_parameterization_parameters_from_query()
    {
        var application = CreateDynamicApiApplicationModel(typeof(DynamicApiBehaviorController), new[]
        {
            nameof(DynamicApiBehaviorController.GetTagsAsync),
            nameof(DynamicApiBehaviorController.GetByCodeAsync)
        });
        var convention = CreateDynamicApiControllerConvention(configureSettings: settings =>
        {
            SetProperty(settings, "SupportedMvcController", true);
            SetProperty(settings, "UrlParameterization", true);
        });

        ApplyDynamicApiConvention(convention, application);

        var actions = application.Controllers.Single().Actions.ToDictionary(action => action.ActionMethod.Name);
        Assert.Equal(BindingSource.Query, actions[nameof(DynamicApiBehaviorController.GetTagsAsync)].Parameters.Single().BindingInfo?.BindingSource);
        Assert.Equal(BindingSource.Query, actions[nameof(DynamicApiBehaviorController.GetByCodeAsync)].Parameters.Single().BindingInfo?.BindingSource);
    }

    [Fact]
    public void DynamicApiConvention_should_generate_parameterized_route_with_seats_and_constraints()
    {
        var apiSeatType = GetWebAppType("Air.Cloud.WebApp.DynamicApiController.Attributes.ApiSeatAttribute");
        var apiSeatsType = GetWebAppType("Air.Cloud.WebApp.DynamicApiController.Enums.ApiSeats");
        var routeConstraintType = GetWebAppType("Air.Cloud.WebApp.DynamicApiController.Attributes.RouteConstraintAttribute");
        var application = CreateDynamicApiApplicationModel(
            typeof(DynamicApiBehaviorController),
            new[] { nameof(DynamicApiBehaviorController.GetRouteAsync) },
            parameterAttributes: new Dictionary<string, Dictionary<string, object[]>>
            {
                [nameof(DynamicApiBehaviorController.GetRouteAsync)] = new Dictionary<string, object[]>
                {
                    ["tenantId"] = new[]
                    {
                        Activator.CreateInstance(apiSeatType, Enum.Parse(apiSeatsType, "ControllerStart"))!,
                        Activator.CreateInstance(routeConstraintType, "int")!
                    },
                    ["category"] = new[]
                    {
                        Activator.CreateInstance(apiSeatType, Enum.Parse(apiSeatsType, "ControllerEnd"))!
                    },
                    ["itemId"] = new[]
                    {
                        Activator.CreateInstance(routeConstraintType, ":guid")!
                    }
                }
            });
        var convention = CreateDynamicApiControllerConvention(configureSettings: settings =>
        {
            SetProperty(settings, "SupportedMvcController", true);
        });

        ApplyDynamicApiConvention(convention, application);

        var template = GetRouteTemplate(application.Controllers.Single().Actions.Single());
        Assert.Equal("api/{tenantid:int}/[controller]/{category}/[action]/{itemid:guid}", template);
    }

    [Fact]
    public void DynamicApiConvention_should_add_unify_metadata_for_bool_and_skip_void_return()
    {
        var application = CreateDynamicApiApplicationModel(typeof(DynamicApiBehaviorController), new[]
        {
            nameof(DynamicApiBehaviorController.GetEnabledAsync),
            nameof(DynamicApiBehaviorController.CreateNothingAsync)
        });
        var convention = CreateDynamicApiControllerConvention(
            configureSettings: settings =>
            {
                SetProperty(settings, "SupportedMvcController", true);
            },
            unifyEnabled: true);

        ApplyDynamicApiConvention(convention, application);

        var actions = application.Controllers.Single().Actions.ToDictionary(action => action.ActionMethod.Name);
        var boolUnifyAttribute = Assert.Single(actions[nameof(DynamicApiBehaviorController.GetEnabledAsync)].Filters, filter =>
            filter.GetType().FullName == "Air.Cloud.WebApp.UnifyResult.Attributes.UnifyResultAttribute");
        Assert.Equal(typeof(bool), GetProperty<Type>(boolUnifyAttribute, "Type"));
        Assert.DoesNotContain(actions[nameof(DynamicApiBehaviorController.CreateNothingAsync)].Filters, filter =>
            filter.GetType().FullName == "Air.Cloud.WebApp.UnifyResult.Attributes.UnifyResultAttribute");
    }

    [Fact]
    public void ValidatorContext_should_convert_model_state_into_stable_validation_failure_result()
    {
        var modelState = new ModelStateDictionary();
        modelState.AddModelError("Name", "名称不能为空");
        modelState.AddModelError("Name", "名称长度不能超过 32");
        modelState.AddModelError("Age", "年龄必须在 1 到 120 之间");

        var metadata = GetValidationMetadata(modelState);
        var failure = GetValidationFailure(metadata);

        Assert.Equal(StatusCodes.Status400BadRequest, GetProperty<int>(failure, "StatusCode"));
        Assert.Null(GetProperty<object>(failure, "ErrorCode"));
        Assert.Equal("请求参数验证失败", GetProperty<string>(failure, "Message"));
        Assert.Equal("请求参数验证失败", GetProperty<string>(metadata, "Message"));
        Assert.Equal(new[] { "名称不能为空", "名称长度不能超过 32" }, GetFields(failure)["Name"]);
        Assert.Equal(new[] { "年龄必须在 1 到 120 之间" }, GetFields(failure)["Age"]);
        Assert.Contains("名称不能为空", GetProperty<string[]>(failure, "Errors"));
        Assert.Contains("年龄必须在 1 到 120 之间", GetProperty<string[]>(failure, "Errors"));
        Assert.Contains("Fields", GetProperty<string>(metadata, "DetailMessage"));
        Assert.Same(modelState, GetProperty<ModelStateDictionary>(metadata, "ModelState"));
    }

    [Fact]
    public void ValidatorContext_should_convert_validation_problem_details_into_field_errors()
    {
        var problemDetails = new ValidationProblemDetails(new Dictionary<string, string[]>
        {
            ["Email"] = new[] { "邮箱格式不正确" },
            ["Password"] = new[] { "密码不能为空", "密码长度不能小于 8 位" }
        });

        var metadata = GetValidationMetadata(problemDetails);
        var failure = GetValidationFailure(metadata);

        Assert.Equal("请求参数验证失败", GetProperty<string>(failure, "Message"));
        Assert.Equal(new[] { "邮箱格式不正确" }, GetFields(failure)["Email"]);
        Assert.Equal(new[] { "密码不能为空", "密码长度不能小于 8 位" }, GetFields(failure)["Password"]);
        Assert.Contains("邮箱格式不正确", GetProperty<string[]>(failure, "Errors"));
        Assert.Contains("密码长度不能小于 8 位", GetProperty<string[]>(failure, "Errors"));
        Assert.Null(GetProperty<ModelStateDictionary>(metadata, "ModelState"));
    }

    [Fact]
    public void ValidatorContext_should_normalize_dictionary_messages()
    {
        var dictionary = new Dictionary<string, string[]>
        {
            ["Code"] = new[] { " 编码不能为空 ", "", "   ", "编码不能重复" }
        };

        var metadata = GetValidationMetadata(dictionary);
        var failure = GetValidationFailure(metadata);

        Assert.Equal("请求参数验证失败", GetProperty<string>(failure, "Message"));
        Assert.Equal(new[] { "编码不能为空", "编码不能重复" }, GetFields(failure)["Code"]);
        Assert.Equal(new[] { "编码不能为空", "编码不能重复" }, GetProperty<string[]>(failure, "Errors"));
    }

    [Fact]
    public void ValidatorContext_should_convert_plain_string_into_global_error()
    {
        var metadata = GetValidationMetadata("业务规则验证失败");
        var failure = GetValidationFailure(metadata);

        Assert.Equal(StatusCodes.Status400BadRequest, GetProperty<int>(failure, "StatusCode"));
        Assert.Equal("业务规则验证失败", GetProperty<string>(failure, "Message"));
        Assert.Empty(GetFields(failure));
        Assert.Equal(new[] { "业务规则验证失败" }, GetProperty<string[]>(failure, "Errors"));
        Assert.Equal("业务规则验证失败", GetProperty<string>(metadata, "Message"));
    }

    [Fact]
    public void ValidationMetadata_should_sync_error_code_and_status_code_to_failure_result()
    {
        var metadata = GetValidationMetadata("业务规则验证失败");
        var failure = GetValidationFailure(metadata);

        SetProperty(metadata, "ErrorCode", "USER_001");
        SetProperty(metadata, "StatusCode", StatusCodes.Status422UnprocessableEntity);

        Assert.Equal("USER_001", GetProperty<object>(metadata, "ErrorCode"));
        Assert.Equal(StatusCodes.Status422UnprocessableEntity, GetProperty<int?>(metadata, "StatusCode"));
        Assert.Equal("USER_001", GetProperty<object>(failure, "ErrorCode"));
        Assert.Equal(StatusCodes.Status422UnprocessableEntity, GetProperty<int>(failure, "StatusCode"));
    }

    [Fact]
    public void RestfulResultProvider_should_wrap_validation_failure_with_message_and_http_status_code()
    {
        var metadata = GetValidationMetadata(new Dictionary<string, string[]>
        {
            ["Name"] = new[] { "名称不能为空" }
        });
        SetProperty(metadata, "ErrorCode", "REQ_001");
        SetProperty(metadata, "StatusCode", StatusCodes.Status422UnprocessableEntity);

        var provider = Activator.CreateInstance(GetWebAppType("Air.Cloud.WebApp.UnifyResult.Providers.RESTfulResultProvider"))!;
        var result = Assert.IsType<JsonResult>(InvokeInstance(provider, "OnValidateFailed", null!, metadata));
        var value = result.Value!;
        var failure = GetValidationFailure(metadata);

        Assert.Equal(StatusCodes.Status422UnprocessableEntity, result.StatusCode);
        Assert.Equal(StatusCodes.Status422UnprocessableEntity, GetProperty<int?>(value, "Code"));
        Assert.False(GetProperty<bool>(value, "Succeeded"));
        Assert.Null(GetProperty<object>(value, "Data"));
        Assert.Equal("请求参数验证失败", GetProperty<string>(value, "Message"));
        Assert.Same(failure, GetProperty<object>(value, "Errors"));
        Assert.Equal("REQ_001", GetProperty<object>(failure, "ErrorCode"));
        Assert.Equal(StatusCodes.Status422UnprocessableEntity, GetProperty<int>(failure, "StatusCode"));
    }

    [Fact]
    public void ValidationFailureResult_should_have_frontend_friendly_defaults()
    {
        var result = Activator.CreateInstance(GetWebAppType("Air.Cloud.WebApp.DataValidation.Internal.ValidationFailureResult"))!;

        Assert.Equal(StatusCodes.Status400BadRequest, GetProperty<int>(result, "StatusCode"));
        Assert.Null(GetProperty<object>(result, "ErrorCode"));
        Assert.Equal("请求参数验证失败", GetProperty<string>(result, "Message"));
        Assert.Empty(GetFields(result));
        Assert.Empty(GetProperty<string[]>(result, "Errors"));
    }

    private static Assembly LoadWebAppAssembly()
    {
        const string assemblyName = "Air.Cloud.WebApp";
        var loadedAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(assembly => assembly.GetName().Name == assemblyName);
        if (loadedAssembly != null)
        {
            return loadedAssembly;
        }

        var repositoryRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var assemblyPath = Path.Combine(
            repositoryRoot,
            "src",
            "core",
            "Air.Cloud.WebApp",
            "bin",
            "Debug",
            "net10.0",
            $"{assemblyName}.dll");

        return Assembly.LoadFrom(assemblyPath);
    }

    private static Type GetWebAppType(string fullName)
    {
        return LoadWebAppAssembly().GetType(fullName, throwOnError: true)!;
    }

    private static Type GetCoreType(string fullName)
    {
        LoadWebAppAssembly();
        return AppDomain.CurrentDomain.GetAssemblies()
            .First(assembly => assembly.GetName().Name == "Air.Cloud.Core")
            .GetType(fullName, throwOnError: true)!;
    }

    private static object InvokeMvcExtension(string methodName, IMvcBuilder mvcBuilder, Type? genericArgument = null)
    {
        var extensionType = GetWebAppType("Air.Cloud.WebApp.Extensions.AppServiceCollectionExtensions");
        var method = extensionType.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(candidate => candidate.Name == methodName)
            .Where(candidate => candidate.GetParameters().FirstOrDefault()?.ParameterType == typeof(IMvcBuilder))
            .Where(candidate => genericArgument == null ? !candidate.IsGenericMethodDefinition : candidate.IsGenericMethodDefinition)
            .OrderBy(candidate => candidate.GetParameters().Length)
            .First();

        if (genericArgument != null)
        {
            method = method.MakeGenericMethod(genericArgument);
        }

        var parameters = method.GetParameters().Length == 1
            ? new object?[] { mvcBuilder }
            : new object?[] { mvcBuilder, null };

        return method.Invoke(null, parameters)!;
    }

    private static MvcOptions BuildMvcOptions(IServiceCollection services)
    {
        using var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IOptions<MvcOptions>>().Value;
    }

    private static bool IsDynamicApiConvention(IApplicationModelConvention convention)
    {
        return convention.GetType().FullName == "Air.Cloud.WebApp.DynamicApiController.Conventions.DynamicApiControllerApplicationModelConvention";
    }

    private static bool IsUnifyResultConvention(IApplicationModelConvention convention)
    {
        return convention.GetType().FullName == "Air.Cloud.WebApp.UnifyResult.Conventions.UnifyResultApplicationModelConvention";
    }

    private static void AssertContainsFilter(MvcOptions mvcOptions, string filterTypeFullName)
    {
        Assert.Contains(mvcOptions.Filters, filter => IsFilter(filter, filterTypeFullName));
    }

    private static void AssertDoesNotContainFilter(MvcOptions mvcOptions, string filterTypeFullName)
    {
        Assert.DoesNotContain(mvcOptions.Filters, filter => IsFilter(filter, filterTypeFullName));
    }

    private static bool IsFilter(IFilterMetadata filter, string filterTypeFullName)
    {
        return filter.GetType().FullName == filterTypeFullName
               || filter is TypeFilterAttribute typeFilter && typeFilter.ImplementationType.FullName == filterTypeFullName;
    }

    private static bool IsFilterServiceDescriptor(ServiceDescriptor descriptor, string filterTypeFullName)
    {
        return descriptor.ImplementationType?.FullName == filterTypeFullName;
    }

    private static void AssertContainsConventionAfter(
        MvcOptions mvcOptions,
        Predicate<IApplicationModelConvention> beforePredicate,
        Predicate<IApplicationModelConvention> afterPredicate)
    {
        var conventions = mvcOptions.Conventions.ToList();
        var beforeIndex = conventions.FindIndex(beforePredicate);
        var afterIndex = conventions.FindIndex(afterPredicate);

        Assert.True(beforeIndex >= 0);
        Assert.True(afterIndex >= 0);
        Assert.True(afterIndex > beforeIndex);
    }

    private static bool GetUnifyEnabled(IServiceCollection services)
    {
        return (bool)GetRuntimeOptions(services).GetType().GetProperty("Enabled")!.GetValue(GetRuntimeOptions(services))!;
    }

    private static Type GetRestfulResultType(IServiceCollection services)
    {
        return (Type)GetRuntimeOptions(services).GetType().GetProperty("ResultModelType")!.GetValue(GetRuntimeOptions(services))!;
    }

    private static void ResetUnifyContext()
    {
    }

    private static object GetRuntimeOptions(IServiceCollection services)
    {
        using var provider = services.BuildServiceProvider();
        var optionsType = GetWebAppType("Air.Cloud.WebApp.UnifyResult.Options.UnifyResultRuntimeOptions");
        var serviceType = typeof(IOptions<>).MakeGenericType(optionsType);
        var options = provider.GetService(serviceType);
        return options == null
            ? Activator.CreateInstance(optionsType)!
            : options.GetType().GetProperty("Value")!.GetValue(options)!;
    }

    private static object GetValidationMetadata(object errors)
    {
        var validatorContextType = GetWebAppType("Air.Cloud.WebApp.DataValidation.Internal.ValidatorContext");
        return validatorContextType.GetMethod("GetValidationMetadata", BindingFlags.Static | BindingFlags.NonPublic)!
            .Invoke(null, new[] { errors })!;
    }

    private static object GetValidationFailure(object metadata)
    {
        var failure = GetProperty<object>(metadata, "ValidationResult")!;
        Assert.Equal(GetWebAppType("Air.Cloud.WebApp.DataValidation.Internal.ValidationFailureResult"), failure.GetType());
        return failure;
    }

    private static Dictionary<string, string[]> GetFields(object validationFailure)
    {
        return GetProperty<Dictionary<string, string[]>>(validationFailure, "Fields");
    }

    private static T GetProperty<T>(object value, string propertyName)
    {
        return (T)value.GetType().GetProperty(propertyName)!.GetValue(value)!;
    }

    private static void SetProperty(object value, string propertyName, object propertyValue)
    {
        value.GetType().GetProperty(propertyName)!.SetValue(value, propertyValue);
    }

    private static object InvokeInstance(object instance, string methodName, params object?[] parameters)
    {
        var parameterTypes = parameters.Select(parameter => parameter?.GetType()).ToArray();
        var method = instance.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(candidate => candidate.Name == methodName)
            .Single(candidate =>
            {
                var methodParameters = candidate.GetParameters();
                if (methodParameters.Length != parameters.Length) return false;

                for (var index = 0; index < methodParameters.Length; index++)
                {
                    if (parameterTypes[index] != null && !methodParameters[index].ParameterType.IsAssignableFrom(parameterTypes[index]!))
                    {
                        return false;
                    }
                }

                return true;
            });

        return method.Invoke(instance, parameters)!;
    }

    private static object CreateDynamicApiControllerConvention(
        object[][]? verbToHttpMethods = null,
        Action<object>? configureSettings = null,
        bool unifyEnabled = false)
    {
        var settingsType = GetWebAppType("Air.Cloud.WebApp.DynamicApiController.Options.DynamicApiControllerSettingsOptions");
        var runtimeOptionsType = GetWebAppType("Air.Cloud.WebApp.UnifyResult.Options.UnifyResultRuntimeOptions");
        var conventionType = GetWebAppType("Air.Cloud.WebApp.DynamicApiController.Conventions.DynamicApiControllerApplicationModelConvention");
        var settings = Activator.CreateInstance(settingsType)!;
        settingsType.GetMethod("PostConfigure")!.Invoke(settings, new object?[] { settings, null });
        settingsType.GetProperty("VerbToHttpMethods")!.SetValue(settings, verbToHttpMethods);
        configureSettings?.Invoke(settings);

        var runtimeOptions = Activator.CreateInstance(runtimeOptionsType)!;
        runtimeOptionsType.GetProperty("Enabled")!.SetValue(runtimeOptions, unifyEnabled);

        return Activator.CreateInstance(
            conventionType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: new[] { settings, runtimeOptions },
            culture: null)!;
    }

    private static ApplicationModel CreateDynamicApiApplicationModel(
        Type controllerType,
        IReadOnlyCollection<string> actionNames,
        Dictionary<string, object[]>? actionAttributes = null,
        Dictionary<string, Dictionary<string, object[]>>? parameterAttributes = null)
    {
        var controller = new ControllerModel(controllerType.GetTypeInfo(), Array.Empty<object>())
        {
            ControllerName = controllerType.Name
        };
        controller.Selectors.Add(new SelectorModel());

        foreach (var method in controllerType.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                     .Where(method => actionNames.Contains(method.Name)))
        {
            var attributes = method.GetCustomAttributes(inherit: true).ToList();
            if (actionAttributes != null && actionAttributes.TryGetValue(method.Name, out var extraActionAttributes))
            {
                attributes.AddRange(extraActionAttributes);
            }

            var action = new ActionModel(method, attributes)
            {
                ActionName = method.Name,
                Controller = controller
            };
            action.Selectors.Add(new SelectorModel());

            foreach (var parameter in method.GetParameters())
            {
                var parameterAttributeList = parameter.GetCustomAttributes(inherit: true).ToList();
                if (parameterAttributes != null
                    && parameterAttributes.TryGetValue(method.Name, out var actionParameterAttributes)
                    && actionParameterAttributes.TryGetValue(parameter.Name!, out var extraParameterAttributes))
                {
                    parameterAttributeList.AddRange(extraParameterAttributes);
                }

                action.Parameters.Add(new ParameterModel(parameter, parameterAttributeList)
                {
                    Action = action,
                    ParameterName = parameter.Name!
                });
            }

            controller.Actions.Add(action);
        }

        var application = new ApplicationModel();
        application.Controllers.Add(controller);
        return application;
    }

    private static void ApplyDynamicApiConvention(object convention, ApplicationModel application)
    {
        ((IApplicationModelConvention)convention).Apply(application);
    }

    private static string GetHttpMethod(ActionModel action)
    {
        var constraint = Assert.IsType<HttpMethodActionConstraint>(action.Selectors.Single().ActionConstraints.Single());
        return Assert.Single(constraint.HttpMethods);
    }

    private static string GetRouteTemplate(ActionModel action)
    {
        return action.Selectors.Single().AttributeRouteModel?.Template!;
    }

    private static IReadOnlyDictionary<string, string> GetConventionVerbToHttpMethods(object convention)
    {
        return (IReadOnlyDictionary<string, string>)convention.GetType()
            .GetField("_verbToHttpMethods", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(convention)!;
    }

    private static System.Collections.Concurrent.ConcurrentDictionary<string, string> GetGlobalVerbToHttpMethods()
    {
        var penetratesType = GetCoreType("Air.Cloud.Core.Modules.DynamicApp.Internal.Penetrates");
        return (System.Collections.Concurrent.ConcurrentDictionary<string, string>)penetratesType
            .GetProperty("VerbToHttpMethods")!
            .GetValue(null)!;
    }

    private static System.Collections.Concurrent.ConcurrentDictionary<string, int> GetControllerOrderCollection()
    {
        var penetratesType = GetCoreType("Air.Cloud.Core.Modules.DynamicApp.Internal.Penetrates");
        return (System.Collections.Concurrent.ConcurrentDictionary<string, int>)penetratesType
            .GetProperty("ControllerOrderCollection")!
            .GetValue(null)!;
    }

    private static Type CreateDynamicControllerTypeWithApiDescriptionSettings()
    {
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
            new AssemblyName($"AirCloudWebAppDynamicController_{Guid.NewGuid():N}"),
            AssemblyBuilderAccess.Run);
        var moduleBuilder = assemblyBuilder.DefineDynamicModule("Main");
        var typeBuilder = moduleBuilder.DefineType(
            $"AirCloudWebAppDynamicController.DynamicApiProbeController_{Guid.NewGuid():N}",
            TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Class,
            typeof(ControllerBase));
        var attributeType = GetWebAppType("Air.Cloud.WebApp.DynamicApiController.Attributes.ApiDescriptionSettingsAttribute");
        var attributeConstructor = attributeType.GetConstructor(new[] { typeof(string[]) })!;
        typeBuilder.SetCustomAttribute(new CustomAttributeBuilder(
            attributeConstructor,
            new object[] { new[] { "ProbeGroup" } },
            new[]
            {
                attributeType.GetProperty("Name")!,
                attributeType.GetProperty("Tag")!,
                attributeType.GetProperty("Description")!,
                attributeType.GetProperty("Order")!
            },
            new object[]
            {
                "dynamic-api-probe",
                "ProbeTag",
                "Probe description",
                42
            }));

        var methodBuilder = typeBuilder.DefineMethod(
            "GetOrderedAsync",
            MethodAttributes.Public | MethodAttributes.HideBySig,
            typeof(string),
            Type.EmptyTypes);
        var il = methodBuilder.GetILGenerator();
        il.Emit(OpCodes.Ldstr, "ok");
        il.Emit(OpCodes.Ret);

        return typeBuilder.CreateType()!;
    }

    private static Type CreateDynamicControllerTypeWithActionApiDescriptionSettings()
    {
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
            new AssemblyName($"AirCloudWebAppDynamicController_{Guid.NewGuid():N}"),
            AssemblyBuilderAccess.Run);
        var moduleBuilder = assemblyBuilder.DefineDynamicModule("Main");
        var typeBuilder = moduleBuilder.DefineType(
            $"AirCloudWebAppDynamicController.DynamicApiProbeController_{Guid.NewGuid():N}",
            TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Class,
            typeof(ControllerBase));
        var attributeType = GetWebAppType("Air.Cloud.WebApp.DynamicApiController.Attributes.ApiDescriptionSettingsAttribute");
        var attributeConstructor = attributeType.GetConstructor(new[] { typeof(string[]) })!;
        typeBuilder.SetCustomAttribute(new CustomAttributeBuilder(
            attributeConstructor,
            new object[] { new[] { "ProbeGroup" } },
            new[]
            {
                attributeType.GetProperty("Name")!,
                attributeType.GetProperty("Tag")!,
                attributeType.GetProperty("Description")!,
                attributeType.GetProperty("Order")!
            },
            new object[]
            {
                "dynamic-api-probe",
                "ProbeTag",
                "Probe description",
                42
            }));

        var methodBuilder = typeBuilder.DefineMethod(
            "GetOrderedAsync",
            MethodAttributes.Public | MethodAttributes.HideBySig,
            typeof(string),
            Type.EmptyTypes);
        methodBuilder.SetCustomAttribute(new CustomAttributeBuilder(
            attributeConstructor,
            new object[] { new[] { "ActionGroup" } },
            new[]
            {
                attributeType.GetProperty("Tag")!,
                attributeType.GetProperty("Description")!,
                attributeType.GetProperty("Order")!
            },
            new object[]
            {
                "ActionTag",
                "Action description",
                7
            }));
        var il = methodBuilder.GetILGenerator();
        il.Emit(OpCodes.Ldstr, "ok");
        il.Emit(OpCodes.Ret);

        return typeBuilder.CreateType()!;
    }

    private static object CreateApiDescriptionSettingsAttribute(string groupName, string tag, string description, int order)
    {
        var attributeType = GetWebAppType("Air.Cloud.WebApp.DynamicApiController.Attributes.ApiDescriptionSettingsAttribute");
        var attribute = Activator.CreateInstance(attributeType, new object[] { new[] { groupName } })!;
        SetProperty(attribute, "Tag", tag);
        SetProperty(attribute, "Description", description);
        SetProperty(attribute, "Order", order);
        return attribute;
    }

    private static Type CreateDynamicUnifyProviderType(bool includeUnifyModelAttribute, Type? modelType = null)
    {
        var providerInterface = GetWebAppType("Air.Cloud.WebApp.UnifyResult.Providers.IUnifyResultProvider");
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
            new AssemblyName($"AirCloudWebAppUnitTestDynamic_{Guid.NewGuid():N}"),
            AssemblyBuilderAccess.Run);
        var moduleBuilder = assemblyBuilder.DefineDynamicModule("Main");
        var typeBuilder = moduleBuilder.DefineType(
            $"AirCloudWebAppUnitTestDynamic.Provider_{Guid.NewGuid():N}",
            TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Class);
        typeBuilder.AddInterfaceImplementation(providerInterface);

        if (includeUnifyModelAttribute)
        {
            var attributeConstructor = GetWebAppType("Air.Cloud.WebApp.UnifyResult.Attributes.UnifyModelAttribute")
                .GetConstructor(new[] { typeof(Type) })!;
            typeBuilder.SetCustomAttribute(new CustomAttributeBuilder(attributeConstructor, new object[] { modelType ?? typeof(TestApiResult<>) }));
        }

        foreach (var interfaceMethod in providerInterface.GetMethods())
        {
            ImplementInterfaceMethod(typeBuilder, interfaceMethod);
        }

        return typeBuilder.CreateType()!;
    }

    private static void ImplementInterfaceMethod(TypeBuilder typeBuilder, MethodInfo interfaceMethod)
    {
        var parameterTypes = interfaceMethod.GetParameters().Select(parameter => parameter.ParameterType).ToArray();
        var methodBuilder = typeBuilder.DefineMethod(
            interfaceMethod.Name,
            MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot,
            interfaceMethod.ReturnType,
            parameterTypes);

        var il = methodBuilder.GetILGenerator();
        if (interfaceMethod.ReturnType == typeof(Task))
        {
            il.EmitCall(OpCodes.Call, typeof(Task).GetProperty(nameof(Task.CompletedTask))!.GetMethod!, null);
        }
        else
        {
            il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Newobj, typeof(JsonResult).GetConstructor(new[] { typeof(object) })!);
        }

        il.Emit(OpCodes.Ret);
        typeBuilder.DefineMethodOverride(methodBuilder, interfaceMethod);
    }

    public sealed class TestApiResult<T>
    {
        public int Code { get; set; }

        public T? Data { get; set; }
    }

    public sealed class NonGenericApiResult
    {
        public int Code { get; set; }
    }

    public sealed class TwoGenericApiResult<TData, TError>
    {
        public TData? Data { get; set; }

        public TError? Error { get; set; }
    }
}

public sealed class DynamicApiBehaviorController : ControllerBase
{
    public bool GetEnabledAsync()
    {
        return true;
    }

    public DynamicApiBodyDto CreateBodyAsync(DynamicApiBodyDto input)
    {
        return input;
    }

    public DynamicApiBodyDto UpdateBodyAsync(DynamicApiBodyDto input)
    {
        return input;
    }

    public DynamicApiBodyDto DeleteBodyAsync(DynamicApiBodyDto input)
    {
        return input;
    }

    public DynamicApiBodyDto PatchBodyAsync(DynamicApiBodyDto input)
    {
        return input;
    }

    public DynamicApiBodyDto GetBodyAsync(DynamicApiBodyDto input)
    {
        return input;
    }

    public IReadOnlyCollection<string> GetListUsersAsync()
    {
        return Array.Empty<string>();
    }

    public object SearchAsync(DynamicApiBodyDto filter, int pageIndex)
    {
        return new object();
    }

    public object GetTagsAsync(string[] tags)
    {
        return tags;
    }

    public object GetByCodeAsync(string code)
    {
        return code;
    }

    public object GetRouteAsync(int tenantId, string category, Guid itemId)
    {
        return new { tenantId, category, itemId };
    }

    public void CreateNothingAsync()
    {
    }
}

public sealed class DynamicApiBodyDto
{
    public string? Name { get; set; }
}
