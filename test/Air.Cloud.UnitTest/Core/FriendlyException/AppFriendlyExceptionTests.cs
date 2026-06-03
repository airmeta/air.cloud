using System.Reflection;
using System.Reflection.Emit;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;

namespace Air.Cloud.UnitTest.FriendlyException;

public class AppFriendlyExceptionTests
{
    [Fact]
    public void Constructor_should_preserve_message_error_code_and_inner_exception()
    {
        var innerException = new InvalidOperationException("inner");
        var exceptionType = LoadWebAppAssembly()
            .GetType("Air.Cloud.WebApp.FriendlyException.Exceptions.AppFriendlyException", throwOnError: true)!;

        var exception = (Exception)Activator.CreateInstance(
            exceptionType,
            "friendly message",
            "E001",
            innerException)!;

        Assert.Equal("friendly message", exception.Message);
        Assert.Equal("friendly message", exceptionType.GetProperty("ErrorMessage")!.GetValue(exception));
        Assert.Equal("E001", exceptionType.GetProperty("ErrorCode")!.GetValue(exception));
        Assert.Same(innerException, exception.InnerException);
        Assert.Equal(StatusCodes.Status500InternalServerError, exceptionType.GetProperty("StatusCode")!.GetValue(exception));
        Assert.False((bool)exceptionType.GetProperty("ValidationException")!.GetValue(exception)!);
    }

    [Fact]
    public void Default_constructor_should_keep_framework_defaults()
    {
        var exceptionType = LoadWebAppAssembly()
            .GetType("Air.Cloud.WebApp.FriendlyException.Exceptions.AppFriendlyException", throwOnError: true)!;

        var exception = (Exception)Activator.CreateInstance(exceptionType)!;

        Assert.Null(exceptionType.GetProperty("ErrorMessage")!.GetValue(exception));
        Assert.Null(exceptionType.GetProperty("ErrorCode")!.GetValue(exception));
        Assert.Equal(StatusCodes.Status500InternalServerError, exceptionType.GetProperty("StatusCode")!.GetValue(exception));
        Assert.False((bool)exceptionType.GetProperty("ValidationException")!.GetValue(exception)!);
    }

    [Fact]
    public void Constructor_without_inner_exception_should_preserve_message_and_code()
    {
        var exceptionType = LoadWebAppAssembly()
            .GetType("Air.Cloud.WebApp.FriendlyException.Exceptions.AppFriendlyException", throwOnError: true)!;

        var exception = (Exception)Activator.CreateInstance(exceptionType, "message", "CODE_001")!;

        Assert.Equal("message", exception.Message);
        Assert.Equal("message", exceptionType.GetProperty("ErrorMessage")!.GetValue(exception));
        Assert.Equal("CODE_001", exceptionType.GetProperty("ErrorCode")!.GetValue(exception));
        Assert.Null(exception.InnerException);
    }

    [Fact]
    public void StatusCode_extension_should_update_status_and_return_same_exception()
    {
        var exception = CreateAppFriendlyException("message", "CODE_001", new InvalidOperationException("inner"));
        var extensionType = LoadWebAppAssembly()
            .GetType("Air.Cloud.WebApp.FriendlyException.Extensions.AppFriendlyExceptionExtensions", throwOnError: true)!;
        var method = extensionType.GetMethod("StatusCode", BindingFlags.Public | BindingFlags.Static)!;

        var returned = method.Invoke(null, new object[] { exception, StatusCodes.Status409Conflict });

        Assert.Same(exception, returned);
        Assert.Equal(StatusCodes.Status409Conflict, GetProperty<int>(exception, "StatusCode"));
    }

    [Fact]
    public void Oops_Oh_should_create_formatted_friendly_exception()
    {
        var exception = InvokeOops("Oh", "friendly exception {0}", "A");
        var exceptionType = exception.GetType();

        Assert.Equal("friendly exception A", exception.Message);
        Assert.Equal("friendly exception A", exceptionType.GetProperty("ErrorMessage")!.GetValue(exception));
        Assert.Null(exceptionType.GetProperty("ErrorCode")!.GetValue(exception));
        Assert.Equal(StatusCodes.Status500InternalServerError, exceptionType.GetProperty("StatusCode")!.GetValue(exception));
        Assert.False((bool)exceptionType.GetProperty("ValidationException")!.GetValue(exception)!);
    }

    [Fact]
    public void Oops_Bah_should_create_business_validation_exception()
    {
        var exception = InvokeOops("Bah", "business exception {0}", "A");
        var exceptionType = exception.GetType();

        Assert.Equal("business exception A", exception.Message);
        Assert.Equal(StatusCodes.Status400BadRequest, exceptionType.GetProperty("StatusCode")!.GetValue(exception));
        Assert.True((bool)exceptionType.GetProperty("ValidationException")!.GetValue(exception)!);
        Assert.IsType<System.ComponentModel.DataAnnotations.ValidationException>(exception.InnerException);
    }

    [Fact]
    public void Oops_Bah_with_error_code_should_create_business_validation_exception()
    {
        var exception = InvokeOops("Bah", (object)"REQ_001");

        Assert.Equal("REQ_001", GetProperty<object>(exception, "ErrorCode"));
        Assert.Equal(StatusCodes.Status400BadRequest, GetProperty<int>(exception, "StatusCode"));
        Assert.True(GetProperty<bool>(exception, "ValidationException"));
        Assert.IsType<System.ComponentModel.DataAnnotations.ValidationException>(exception.InnerException);
    }

    [Fact]
    public void Oops_Oh_with_error_code_should_keep_code_and_use_default_message()
    {
        var exception = InvokeOops("Oh", (object)"E001");
        var exceptionType = exception.GetType();

        Assert.Equal("E001", exceptionType.GetProperty("ErrorCode")!.GetValue(exception));
        Assert.Contains("E001", exception.Message);
        Assert.Contains("Internal Server Error", exception.Message);
    }

    [Fact]
    public void Oops_Oh_with_exception_type_should_create_typed_inner_exception()
    {
        var exception = InvokeOopsWithExceptionType(
            "Oh",
            "typed message {0}",
            typeof(InvalidOperationException),
            "A");

        Assert.Equal("typed message A", exception.Message);
        Assert.IsType<InvalidOperationException>(exception.InnerException);
        Assert.Equal("typed message A", exception.InnerException!.Message);
        Assert.False(GetProperty<bool>(exception, "ValidationException"));
    }

    [Fact]
    public void Oops_Oh_generic_with_message_should_create_typed_inner_exception()
    {
        var exception = InvokeGenericOopsWithMessage(
            "Oh",
            typeof(ArgumentException),
            "generic message {0}",
            "A");

        Assert.Equal("generic message A", exception.Message);
        Assert.IsType<ArgumentException>(exception.InnerException);
    }

    [Fact]
    public void Oops_Oh_with_error_code_and_exception_type_should_preserve_code_and_inner_exception()
    {
        var exception = InvokeOopsWithErrorCodeAndExceptionType(
            "Oh",
            "E002",
            typeof(InvalidOperationException),
            "A");

        Assert.Equal("E002", GetProperty<object>(exception, "ErrorCode"));
        Assert.IsType<InvalidOperationException>(exception.InnerException);
        Assert.Contains("E002", exception.Message);
    }

    [Fact]
    public void Oops_Oh_generic_with_error_code_should_preserve_code_and_inner_exception()
    {
        var exception = InvokeGenericOopsWithErrorCode(
            "Oh",
            typeof(InvalidOperationException),
            "E003",
            "A");

        Assert.Equal("E003", GetProperty<object>(exception, "ErrorCode"));
        Assert.IsType<InvalidOperationException>(exception.InnerException);
        Assert.Contains("E003", exception.Message);
    }

    [Fact]
    public void IfExceptionAttribute_default_constructor_should_leave_matching_properties_empty()
    {
        var attributeType = LoadWebAppAssembly()
            .GetType("Air.Cloud.WebApp.FriendlyException.Attributes.IfExceptionAttribute", throwOnError: true)!;

        var attribute = Activator.CreateInstance(attributeType)!;

        Assert.Null(GetProperty<object>(attribute, "ErrorCode"));
        Assert.Null(GetProperty<Type>(attribute, "ExceptionType"));
        Assert.Null(GetProperty<string>(attribute, "ErrorMessage"));
    }

    [Fact]
    public void IfExceptionAttribute_error_code_constructor_should_store_code_and_args()
    {
        var attributeType = LoadWebAppAssembly()
            .GetType("Air.Cloud.WebApp.FriendlyException.Attributes.IfExceptionAttribute", throwOnError: true)!;

        var attribute = Activator.CreateInstance(attributeType, new object[] { "CODE_001", new object[] { "A", 1 } })!;

        Assert.Equal("CODE_001", GetProperty<object>(attribute, "ErrorCode"));
        Assert.Equal(new object[] { "A", 1 }, GetProperty<object[]>(attribute, "Args"));
    }

    [Fact]
    public void IfExceptionAttribute_exception_type_constructor_should_store_exception_type()
    {
        var attributeType = LoadWebAppAssembly()
            .GetType("Air.Cloud.WebApp.FriendlyException.Attributes.IfExceptionAttribute", throwOnError: true)!;

        var attribute = Activator.CreateInstance(attributeType, typeof(InvalidOperationException))!;

        Assert.Equal(typeof(InvalidOperationException), GetProperty<Type>(attribute, "ExceptionType"));
        Assert.Null(GetProperty<object>(attribute, "ErrorCode"));
    }

    [Fact]
    public void ErrorCodeItemMetadataAttribute_should_store_message_args_and_custom_code()
    {
        var attributeType = LoadWebAppAssembly()
            .GetType("Air.Cloud.WebApp.FriendlyException.Attributes.ErrorCodeItemMetadataAttribute", throwOnError: true)!;

        var attribute = Activator.CreateInstance(attributeType, new object[] { "message {0}", new object[] { "A" } })!;
        attributeType.GetProperty("ErrorCode")!.SetValue(attribute, "CUSTOM_CODE");

        Assert.Equal("message {0}", GetProperty<string>(attribute, "ErrorMessage"));
        Assert.Equal(new object[] { "A" }, GetProperty<object[]>(attribute, "Args"));
        Assert.Equal("CUSTOM_CODE", GetProperty<object>(attribute, "ErrorCode"));
    }

    [Fact]
    public void UnifyContext_should_apply_if_exception_message()
    {
        var methodInfo = CreateMethodWithIfExceptionAttribute(typeof(InvalidOperationException), "mapped exception message");
        var metadata = GetExceptionMetadata(methodInfo, new InvalidOperationException("original exception message"));

        Assert.Equal(StatusCodes.Status500InternalServerError, GetProperty<int>(metadata, "StatusCode"));
        Assert.Null(GetProperty<object>(metadata, "ErrorCode"));
        Assert.Equal("mapped exception message", GetProperty<object>(metadata, "Errors"));
    }

    [Fact]
    public void UnifyContext_should_match_friendly_exception_inner_exception_for_if_exception()
    {
        var methodInfo = CreateMethodWithIfExceptionAttribute(typeof(InvalidOperationException), "inner exception mapped message");
        var exception = CreateAppFriendlyException(
            "friendly original message",
            "E002",
            new InvalidOperationException("inner original message"));

        var metadata = GetExceptionMetadata(methodInfo, exception);

        Assert.Equal(StatusCodes.Status500InternalServerError, GetProperty<int>(metadata, "StatusCode"));
        Assert.Equal("E002", GetProperty<object>(metadata, "ErrorCode"));
        Assert.Equal("inner exception mapped message", GetProperty<object>(metadata, "Errors"));
    }

    [Fact]
    public void UnifyContext_should_use_inner_exception_message_for_plain_exception()
    {
        var exception = new InvalidOperationException(
            "outer message",
            new ArgumentException("inner message"));
        var metadata = GetExceptionMetadata(GetDummyActionMethod(), exception);

        Assert.Equal(StatusCodes.Status500InternalServerError, GetProperty<int>(metadata, "StatusCode"));
        Assert.Null(GetProperty<object>(metadata, "ErrorCode"));
        Assert.Equal("inner message", GetProperty<object>(metadata, "Errors"));
    }

    [Fact]
    public void UnifyContext_should_use_message_for_plain_exception_without_inner_exception()
    {
        var metadata = GetExceptionMetadata(
            GetDummyActionMethod(),
            new InvalidOperationException("outer message"));

        Assert.Equal("outer message", GetProperty<object>(metadata, "Errors"));
    }

    [Fact]
    public void UnifyContext_should_keep_validation_exception_error_message_object()
    {
        var errorMessage = new Dictionary<string, string[]>
        {
            ["Name"] = new[] { "Name required" }
        };
        var exception = CreateAppFriendlyException(
            "validation message",
            "VALIDATION_001",
            new System.ComponentModel.DataAnnotations.ValidationException("validation inner"));
        SetProperty(exception, "ValidationException", true);
        SetProperty(exception, "StatusCode", StatusCodes.Status422UnprocessableEntity);
        SetProperty(exception, "ErrorMessage", errorMessage);

        var metadata = GetExceptionMetadata(GetDummyActionMethod(), exception);

        Assert.Equal(StatusCodes.Status422UnprocessableEntity, GetProperty<int>(metadata, "StatusCode"));
        Assert.Equal("VALIDATION_001", GetProperty<object>(metadata, "ErrorCode"));
        Assert.Same(errorMessage, GetProperty<object>(metadata, "Errors"));
    }

    [Fact]
    public void UnifyContext_should_apply_default_if_exception_when_no_type_match_exists()
    {
        var methodInfo = CreateMethodWithIfExceptionAttributes(
            new IfExceptionDefinition(typeof(ArgumentException), "unmatched exception"),
            new IfExceptionDefinition((Type?)null, "default exception message"));

        var metadata = GetExceptionMetadata(methodInfo, new InvalidOperationException("original exception"));

        Assert.Equal("default exception message", GetProperty<object>(metadata, "Errors"));
    }

    [Fact]
    public void UnifyContext_should_ignore_error_code_if_exception_attribute_for_global_exception_metadata()
    {
        var methodInfo = CreateMethodWithIfExceptionAttributes(
            new IfExceptionDefinition("ERR_001", "error code mapped message"),
            new IfExceptionDefinition(typeof(InvalidOperationException), "type mapped message"));

        var metadata = GetExceptionMetadata(methodInfo, new InvalidOperationException("original exception"));

        Assert.Equal("type mapped message", GetProperty<object>(metadata, "Errors"));
    }

    [Fact]
    public void RestfulResultProvider_should_wrap_exception_metadata()
    {
        var methodInfo = CreateMethodWithIfExceptionAttribute(typeof(InvalidOperationException), "unified exception message");
        var exceptionContext = CreateExceptionContext(methodInfo, new InvalidOperationException("original exception message"));
        var metadata = InvokeStatic(
            "Air.Cloud.WebApp.UnifyResult.UnifyContext",
            "GetExceptionMetadata",
            exceptionContext);

        var provider = Activator.CreateInstance(
            LoadWebAppAssembly().GetType("Air.Cloud.WebApp.UnifyResult.Providers.RESTfulResultProvider", throwOnError: true)!)!;
        var result = Assert.IsType<JsonResult>(InvokeInstance(provider, "OnException", exceptionContext, metadata));
        var value = result.Value!;

        Assert.Equal(StatusCodes.Status500InternalServerError, GetProperty<int>(value, "Code"));
        Assert.False(GetProperty<bool>(value, "Succeeded"));
        Assert.Null(GetProperty<object>(value, "Data"));
        Assert.Equal("unified exception message", GetProperty<object>(value, "Errors"));
    }

    [Fact]
    public void RestfulResultProvider_should_wrap_success_data()
    {
        var provider = Activator.CreateInstance(
            LoadWebAppAssembly().GetType("Air.Cloud.WebApp.UnifyResult.Providers.RESTfulResultProvider", throwOnError: true)!)!;
        var data = new { Id = 1, Name = "demo" };
        var result = Assert.IsType<JsonResult>(InvokeInstance(provider, "OnSucceeded", CreateActionExecutedContext(), data));
        var value = result.Value!;

        Assert.Equal(StatusCodes.Status200OK, GetProperty<int>(value, "Code"));
        Assert.True(GetProperty<bool>(value, "Succeeded"));
        Assert.Same(data, GetProperty<object>(value, "Data"));
        Assert.Null(GetProperty<object>(value, "Errors"));
    }

    [Fact]
    public void RestfulResultProvider_should_keep_exception_status_code_in_result_code()
    {
        var exception = new InvalidOperationException("plain error");
        var context = CreateExceptionContext(GetDummyActionMethod(), exception);
        var metadata = InvokeStatic("Air.Cloud.WebApp.UnifyResult.UnifyContext", "GetExceptionMetadata", context);
        var provider = Activator.CreateInstance(
            LoadWebAppAssembly().GetType("Air.Cloud.WebApp.UnifyResult.Providers.RESTfulResultProvider", throwOnError: true)!)!;
        var result = Assert.IsType<JsonResult>(InvokeInstance(provider, "OnException", context, metadata));
        var value = result.Value!;

        Assert.Equal(StatusCodes.Status500InternalServerError, GetProperty<int>(value, "Code"));
        Assert.False(GetProperty<bool>(value, "Succeeded"));
        Assert.Equal("plain error", GetProperty<object>(value, "Errors"));
    }

    private void DummyAction()
    {
    }

    private static Exception InvokeOops(string methodName, string message, params object[] args)
    {
        var oopsType = LoadWebAppAssembly().GetType("Air.Cloud.WebApp.FriendlyException.Oops", throwOnError: true)!;
        var method = oopsType.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(candidate =>
            {
                var parameters = candidate.GetParameters();
                return candidate.Name == methodName
                       && !candidate.IsGenericMethodDefinition
                       && parameters.Length == 2
                       && parameters[0].ParameterType == typeof(string)
                       && parameters[1].ParameterType == typeof(object[]);
            });

        return (Exception)method.Invoke(null, new object[] { message, args })!;
    }

    private static Exception InvokeOops(string methodName, object errorCode, params object[] args)
    {
        var oopsType = LoadWebAppAssembly().GetType("Air.Cloud.WebApp.FriendlyException.Oops", throwOnError: true)!;
        var method = oopsType.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(candidate =>
            {
                var parameters = candidate.GetParameters();
                return candidate.Name == methodName
                       && !candidate.IsGenericMethodDefinition
                       && parameters.Length == 2
                       && parameters[0].ParameterType == typeof(object)
                       && parameters[1].ParameterType == typeof(object[]);
            });

        return (Exception)method.Invoke(null, new object[] { errorCode, args })!;
    }

    private static Exception InvokeOopsWithExceptionType(string methodName, string message, Type exceptionType, params object[] args)
    {
        var oopsType = LoadWebAppAssembly().GetType("Air.Cloud.WebApp.FriendlyException.Oops", throwOnError: true)!;
        var method = oopsType.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(candidate =>
            {
                var parameters = candidate.GetParameters();
                return candidate.Name == methodName
                       && !candidate.IsGenericMethodDefinition
                       && parameters.Length == 3
                       && parameters[0].ParameterType == typeof(string)
                       && parameters[1].ParameterType == typeof(Type)
                       && parameters[2].ParameterType == typeof(object[]);
            });

        return (Exception)method.Invoke(null, new object[] { message, exceptionType, args })!;
    }

    private static Exception InvokeOopsWithErrorCodeAndExceptionType(string methodName, object errorCode, Type exceptionType, params object[] args)
    {
        var oopsType = LoadWebAppAssembly().GetType("Air.Cloud.WebApp.FriendlyException.Oops", throwOnError: true)!;
        var method = oopsType.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(candidate =>
            {
                var parameters = candidate.GetParameters();
                return candidate.Name == methodName
                       && !candidate.IsGenericMethodDefinition
                       && parameters.Length == 3
                       && parameters[0].ParameterType == typeof(object)
                       && parameters[1].ParameterType == typeof(Type)
                       && parameters[2].ParameterType == typeof(object[]);
            });

        return (Exception)method.Invoke(null, new object[] { errorCode, exceptionType, args })!;
    }

    private static Exception InvokeGenericOopsWithMessage(string methodName, Type exceptionType, string message, params object[] args)
    {
        var method = GetGenericOopsMethod(methodName, typeof(string)).MakeGenericMethod(exceptionType);
        return (Exception)method.Invoke(null, new object[] { message, args })!;
    }

    private static Exception InvokeGenericOopsWithErrorCode(string methodName, Type exceptionType, object errorCode, params object[] args)
    {
        var method = GetGenericOopsMethod(methodName, typeof(object)).MakeGenericMethod(exceptionType);
        return (Exception)method.Invoke(null, new object[] { errorCode, args })!;
    }

    private static MethodInfo GetGenericOopsMethod(string methodName, Type firstParameterType)
    {
        var oopsType = LoadWebAppAssembly().GetType("Air.Cloud.WebApp.FriendlyException.Oops", throwOnError: true)!;
        return oopsType.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(candidate =>
            {
                var parameters = candidate.GetParameters();
                return candidate.Name == methodName
                       && candidate.IsGenericMethodDefinition
                       && parameters.Length == 2
                       && parameters[0].ParameterType == firstParameterType
                       && parameters[1].ParameterType == typeof(object[]);
            });
    }

    private static Exception CreateAppFriendlyException(string message, object errorCode, Exception innerException)
    {
        var exceptionType = LoadWebAppAssembly()
            .GetType("Air.Cloud.WebApp.FriendlyException.Exceptions.AppFriendlyException", throwOnError: true)!;

        return (Exception)Activator.CreateInstance(exceptionType, message, errorCode, innerException)!;
    }

    private static object GetExceptionMetadata(MethodInfo methodInfo, Exception exception)
    {
        return InvokeStatic(
            "Air.Cloud.WebApp.UnifyResult.UnifyContext",
            "GetExceptionMetadata",
            CreateExceptionContext(methodInfo, exception));
    }

    private static ExceptionContext CreateExceptionContext(MethodInfo methodInfo, Exception exception)
    {
        var actionDescriptor = new ControllerActionDescriptor
        {
            MethodInfo = methodInfo,
            ControllerTypeInfo = methodInfo.DeclaringType!.GetTypeInfo()
        };
        var actionContext = new ActionContext(new DefaultHttpContext(), new RouteData(), actionDescriptor);

        return new ExceptionContext(actionContext, new List<IFilterMetadata>())
        {
            Exception = exception
        };
    }

    private static ActionExecutedContext CreateActionExecutedContext()
    {
        var methodInfo = GetDummyActionMethod();
        var actionDescriptor = new ControllerActionDescriptor
        {
            MethodInfo = methodInfo,
            ControllerTypeInfo = methodInfo.DeclaringType!.GetTypeInfo()
        };
        var actionContext = new ActionContext(new DefaultHttpContext(), new RouteData(), actionDescriptor);

        return new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), new object());
    }

    private static MethodInfo CreateMethodWithIfExceptionAttribute(Type exceptionType, string errorMessage)
    {
        return CreateMethodWithIfExceptionAttributes(new IfExceptionDefinition(exceptionType, errorMessage));
    }

    private static MethodInfo CreateMethodWithIfExceptionAttributes(params IfExceptionDefinition[] definitions)
    {
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
            new AssemblyName($"AirCloudFriendlyExceptionTests_{Guid.NewGuid():N}"),
            AssemblyBuilderAccess.Run);
        var moduleBuilder = assemblyBuilder.DefineDynamicModule("Main");
        var typeBuilder = moduleBuilder.DefineType(
            $"AirCloudFriendlyExceptionTests.Controller_{Guid.NewGuid():N}",
            TypeAttributes.Public | TypeAttributes.Class,
            typeof(ControllerBase));
        var methodBuilder = typeBuilder.DefineMethod(
            "Action",
            MethodAttributes.Public,
            typeof(void),
            Type.EmptyTypes);

        foreach (var definition in definitions)
        {
            methodBuilder.SetCustomAttribute(CreateIfExceptionAttribute(definition));
        }

        methodBuilder.GetILGenerator().Emit(OpCodes.Ret);

        return typeBuilder.CreateType()!.GetMethod("Action")!;
    }

    private static CustomAttributeBuilder CreateIfExceptionAttribute(IfExceptionDefinition definition)
    {
        var attributeType = LoadWebAppAssembly()
            .GetType("Air.Cloud.WebApp.FriendlyException.Attributes.IfExceptionAttribute", throwOnError: true)!;
        var errorMessageProperty = attributeType.GetProperty("ErrorMessage")!;

        if (definition.ErrorCode != null)
        {
            var constructor = attributeType.GetConstructor(new[] { typeof(object), typeof(object[]) })!;
            return new CustomAttributeBuilder(
                constructor,
                new object[] { definition.ErrorCode, Array.Empty<object>() },
                new[] { errorMessageProperty },
                new object[] { definition.ErrorMessage });
        }

        if (definition.ExceptionType != null)
        {
            var constructor = attributeType.GetConstructor(new[] { typeof(Type) })!;
            return new CustomAttributeBuilder(
                constructor,
                new object[] { definition.ExceptionType },
                new[] { errorMessageProperty },
                new object[] { definition.ErrorMessage });
        }

        var defaultConstructor = attributeType.GetConstructor(Type.EmptyTypes)!;
        return new CustomAttributeBuilder(
            defaultConstructor,
            Array.Empty<object>(),
            new[] { errorMessageProperty },
            new object[] { definition.ErrorMessage });
    }

    private static object InvokeStatic(string typeFullName, string methodName, params object[] parameters)
    {
        var type = LoadWebAppAssembly().GetType(typeFullName, throwOnError: true)!;
        var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static)!;
        return method.Invoke(null, parameters)!;
    }

    private static object InvokeInstance(object instance, string methodName, params object[] parameters)
    {
        var method = instance.GetType().GetMethods()
            .Where(candidate => candidate.Name == methodName)
            .Single(candidate =>
            {
                var methodParameters = candidate.GetParameters();
                if (methodParameters.Length != parameters.Length) return false;

                for (var index = 0; index < methodParameters.Length; index++)
                {
                    if (!methodParameters[index].ParameterType.IsAssignableFrom(parameters[index].GetType()))
                    {
                        return false;
                    }
                }

                return true;
            });

        return method.Invoke(instance, parameters)!;
    }

    private static T GetProperty<T>(object value, string propertyName)
    {
        return (T)value.GetType().GetProperty(propertyName)!.GetValue(value)!;
    }

    private static void SetProperty(object value, string propertyName, object propertyValue)
    {
        value.GetType().GetProperty(propertyName)!.SetValue(value, propertyValue);
    }

    private static MethodInfo GetDummyActionMethod()
    {
        return typeof(AppFriendlyExceptionTests).GetMethod(
            nameof(DummyAction),
            BindingFlags.Instance | BindingFlags.NonPublic)!;
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

    private sealed record IfExceptionDefinition(Type? ExceptionType, object? ErrorCode, string ErrorMessage)
    {
        public IfExceptionDefinition(Type? exceptionType, string errorMessage)
            : this(exceptionType, null, errorMessage)
        {
        }

        public IfExceptionDefinition(object errorCode, string errorMessage)
            : this(null, errorCode, errorMessage)
        {
        }
    }
}
