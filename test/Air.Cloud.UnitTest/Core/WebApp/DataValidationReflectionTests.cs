using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Air.Cloud.UnitTest.Core.WebApp;

public class DataValidationReflectionTests
{
    [Fact]
    public void DataValidator_should_validate_value_by_explicit_validation_types()
    {
        var result = InvokeDataValidator(
            "TryValidateByTypes",
            "11010119900307001X",
            new[] { GetValidationTypeValue("IDCard") });

        Assert.True(GetProperty<bool>(result, "Passed"));
        Assert.False(GetProperty<bool>(result, "Failed"));
        Assert.False(HasProperty(result, "IsValid"));
    }

    [Fact]
    public void DataValidator_should_report_failed_validation_by_explicit_validation_types()
    {
        var result = InvokeDataValidator(
            "TryValidateByTypes",
            "not-id-card",
            new[] { GetValidationTypeValue("IDCard") });

        Assert.False(GetProperty<bool>(result, "Passed"));
        Assert.True(GetProperty<bool>(result, "Failed"));
        Assert.Contains(
            "idcard",
            GetValidationMessages(result).Single(),
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DataValidator_should_validate_phone_number_by_explicit_validation_types()
    {
        var result = InvokeDataValidator(
            "TryValidateByTypes",
            "13800138000",
            new[] { GetValidationTypeValue("PhoneNumber") });

        Assert.True(GetProperty<bool>(result, "Passed"));
        Assert.Empty(GetValidationMessages(result));
    }

    [Fact]
    public void DataValidator_should_validate_email_by_explicit_validation_types()
    {
        var result = InvokeDataValidator(
            "TryValidateByTypes",
            "demo@example.com",
            new[] { GetValidationTypeValue("EmailAddress") });

        Assert.True(GetProperty<bool>(result, "Passed"));
    }

    [Fact]
    public void DataValidator_should_pass_at_least_one_validation_pattern()
    {
        var result = InvokeDataValidator(
            "TryValidateByTypes",
            "demo@example.com",
            GetValidationPatternValue("AtLeastOne"),
            new[] { GetValidationTypeValue("PhoneNumber"), GetValidationTypeValue("EmailAddress") });

        Assert.True(GetProperty<bool>(result, "Passed"));
        Assert.Empty(GetValidationMessages(result));
    }

    [Fact]
    public void DataValidator_should_collect_failures_when_at_least_one_pattern_has_no_match()
    {
        var result = InvokeDataValidator(
            "TryValidateByTypes",
            "bad-value",
            GetValidationPatternValue("AtLeastOne"),
            new[] { GetValidationTypeValue("PhoneNumber"), GetValidationTypeValue("EmailAddress") });

        Assert.True(GetProperty<bool>(result, "Failed"));
        Assert.Equal(2, GetValidationMessages(result).Length);
    }

    [Fact]
    public void DataValidator_should_fail_all_of_them_when_one_rule_does_not_match()
    {
        var result = InvokeDataValidator(
            "TryValidateByTypes",
            "demo@example.com",
            GetValidationPatternValue("AllOfThem"),
            new[] { GetValidationTypeValue("EmailAddress"), GetValidationTypeValue("PhoneNumber") });

        Assert.True(GetProperty<bool>(result, "Failed"));
        Assert.Single(GetValidationMessages(result));
    }

    [Fact]
    public void DataValidator_should_fail_null_value_for_type_validation()
    {
        var result = InvokeDataValidator(
            "TryValidateByTypes",
            null!,
            new[] { GetValidationTypeValue("IDCard") });

        Assert.True(GetProperty<bool>(result, "Failed"));
        Assert.Equal("The value is required", GetValidationMessages(result).Single());
    }

    [Fact]
    public void DataValidator_should_throw_clear_exception_for_unknown_validation_type()
    {
        var exception = Assert.Throws<TargetInvocationException>(() =>
            InvokeDataValidator("TryValidateByTypes", "value", new object[] { DayOfWeek.Monday }));

        Assert.IsType<InvalidOperationException>(exception.InnerException);
        Assert.Contains("is not a valid validation type", exception.InnerException!.Message);
    }

    [Fact]
    public void DataValidator_should_validate_value_by_data_annotations_attributes()
    {
        var result = InvokeDataValidator(
            "TryValidateByAttributes",
            "",
            new ValidationAttribute[] { new RequiredAttribute { ErrorMessage = "value is required" } });

        Assert.True(GetProperty<bool>(result, "Failed"));
        Assert.Equal("value is required", GetValidationMessages(result).Single());
    }

    [Fact]
    public void DataValidator_should_pass_value_by_data_annotations_attributes()
    {
        var result = InvokeDataValidator(
            "TryValidateByAttributes",
            18,
            new ValidationAttribute[] { new RangeAttribute(1, 120) });

        Assert.True(GetProperty<bool>(result, "Passed"));
        Assert.Empty(GetValidationMessages(result));
    }

    [Fact]
    public void DataValidator_should_fail_string_length_attribute()
    {
        var result = InvokeDataValidator(
            "TryValidateByAttributes",
            "abcdef",
            new ValidationAttribute[] { new StringLengthAttribute(3) { ErrorMessage = "too long" } });

        Assert.True(GetProperty<bool>(result, "Failed"));
        Assert.Equal("too long", GetValidationMessages(result).Single());
    }

    [Fact]
    public void DataValidator_should_return_clear_pattern_result()
    {
        var result = InvokeDataValidator("TryMatchPattern", "abc123", "^[a-z]+$", RegexOptions.None);

        Assert.True(GetProperty<bool>(result, "Failed"));
        Assert.False((bool)InvokeDataValidator("IsMatchPattern", "abc123", "^[a-z]+$", RegexOptions.None));
    }

    [Fact]
    public void DataValidator_should_return_successful_pattern_result()
    {
        var result = InvokeDataValidator("TryMatchPattern", "abc", "^[a-z]+$", RegexOptions.None);

        Assert.True(GetProperty<bool>(result, "Passed"));
        Assert.Empty(GetValidationMessages(result));
    }

    [Fact]
    public void DataValidator_should_match_pattern_with_regex_options()
    {
        var matched = (bool)InvokeDataValidator("IsMatchPattern", "ABC", "^[a-z]+$", RegexOptions.IgnoreCase);

        Assert.True(matched);
    }

    [Fact]
    public void DataValidator_should_validate_object_model_successfully()
    {
        var result = InvokeDataValidator(
            "TryValidateObjectModel",
            new RequiredModel { Name = "demo" },
            true);

        Assert.True(GetProperty<bool>(result, "Passed"));
    }

    [Fact]
    public void DataValidator_should_validate_object_model_with_required_failure()
    {
        var result = InvokeDataValidator(
            "TryValidateObjectModel",
            new RequiredModel(),
            true);

        Assert.True(GetProperty<bool>(result, "Failed"));
        Assert.Contains("Name is required", GetValidationMessages(result));
    }

    [Fact]
    public void DataValidator_should_validate_object_model_with_ivalidatable_object_failure()
    {
        var result = InvokeDataValidator(
            "TryValidateObjectModel",
            new ObjectModelValidationSample { ShouldFail = true },
            true);

        Assert.True(GetProperty<bool>(result, "Failed"));
        Assert.Equal("custom model failure", GetValidationMessages(result).Single());
    }

    [Fact]
    public void DataValidationAttribute_should_call_explicit_validator_without_extension_alias()
    {
        var attribute = CreateDataValidationAttribute(GetValidationTypeValue("PhoneNumber"));
        var validationResult = InvokeValidationAttribute(attribute, "PhoneNumber", "13800138000");

        Assert.Same(ValidationResult.Success, validationResult);
    }

    [Fact]
    public void DataValidationAttribute_should_return_validation_result_when_rule_fails()
    {
        var attribute = CreateDataValidationAttribute(GetValidationTypeValue("PhoneNumber"));
        var validationResult = InvokeValidationAttribute(attribute, "PhoneNumber", "10086");

        Assert.NotSame(ValidationResult.Success, validationResult);
        Assert.Contains("phone", validationResult!.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DataValidationAttribute_should_allow_null_when_configured()
    {
        var attribute = CreateDataValidationAttribute(GetValidationTypeValue("PhoneNumber"));
        SetProperty(attribute, "AllowNullValue", true);

        var validationResult = InvokeValidationAttribute(attribute, "PhoneNumber", null!);

        Assert.Same(ValidationResult.Success, validationResult);
    }

    [Fact]
    public void DataValidationAttribute_should_allow_empty_string_when_configured()
    {
        var attribute = CreateDataValidationAttribute(GetValidationTypeValue("PhoneNumber"));
        SetProperty(attribute, "AllowEmptyStrings", true);

        var validationResult = InvokeValidationAttribute(attribute, "PhoneNumber", "");

        Assert.Same(ValidationResult.Success, validationResult);
    }

    [Fact]
    public void DataValidationAttribute_should_use_custom_error_message()
    {
        var attribute = CreateDataValidationAttribute(GetValidationTypeValue("PhoneNumber"));
        SetProperty(attribute, "ErrorMessage", "{0} custom error");

        var validationResult = InvokeValidationAttribute(attribute, "PhoneNumber", "10086");

        Assert.Equal("PhoneNumber custom error", validationResult!.ErrorMessage);
    }

    [Fact]
    public void DataValidationAttribute_should_support_at_least_one_pattern()
    {
        var attribute = CreateDataValidationAttribute(
            GetValidationPatternValue("AtLeastOne"),
            GetValidationTypeValue("PhoneNumber"),
            GetValidationTypeValue("EmailAddress"));

        var validationResult = InvokeValidationAttribute(attribute, "Contact", "demo@example.com");

        Assert.Same(ValidationResult.Success, validationResult);
    }

    [Fact]
    public void IValidatableObject_should_use_instance_validator_accessor()
    {
        var accessor = InvokeValidatorAccessor(new InstanceValidationSample());
        var result = InvokeInstance(accessor, "ValidateValue", "11010119900307001X", new[] { GetValidationTypeValue("IDCard") });

        Assert.True(GetProperty<bool>(result, "Passed"));
        Assert.False(HasMethod(accessor.GetType(), "TryValidate"));
    }

    [Fact]
    public void DataValidationAccessor_should_validate_self()
    {
        var accessor = InvokeValidatorAccessor(new ObjectModelValidationSample { ShouldFail = false });
        var result = InvokeInstance(accessor, "ValidateSelf", true);

        Assert.True(GetProperty<bool>(result, "Passed"));
    }

    [Fact]
    public void DataValidationAccessor_should_match_pattern()
    {
        var accessor = InvokeValidatorAccessor(new InstanceValidationSample());
        var result = InvokeInstance(accessor, "MatchPattern", "abc", "^[a-z]+$", RegexOptions.None);

        Assert.True(GetProperty<bool>(result, "Passed"));
    }

    [Fact]
    public void DataValidationAccessor_should_return_boolean_pattern_match()
    {
        var accessor = InvokeValidatorAccessor(new InstanceValidationSample());
        var matched = (bool)InvokeInstance(accessor, "IsMatch", "ABC", "^[a-z]+$", RegexOptions.IgnoreCase);

        Assert.True(matched);
    }

    [Fact]
    public void DataValidationAccessor_should_throw_unified_exception_when_ensure_value_fails()
    {
        var accessor = InvokeValidatorAccessor(new InstanceValidationSample());
        var exception = Assert.Throws<TargetInvocationException>(() =>
            InvokeInstance(accessor, "EnsureValue", "bad-id-card", new[] { GetValidationTypeValue("IDCard") }));

        Assert.Equal("Air.Cloud.WebApp.FriendlyException.Exceptions.AppFriendlyException", exception.InnerException!.GetType().FullName);
        Assert.True(GetProperty<bool>(exception.InnerException, "ValidationException"));
    }

    [Fact]
    public void ThrowValidateFailedModel_should_do_nothing_when_result_passed()
    {
        var result = InvokeDataValidator("TryMatchPattern", "abc", "^[a-z]+$", RegexOptions.None);

        InvokeDataValidationExtension("ThrowValidateFailedModel", result);
    }

    [Fact]
    public void DataValidationResult_should_live_in_public_results_namespace()
    {
        var resultType = GetWebAppType("Air.Cloud.WebApp.DataValidation.Results.DataValidationResult");

        Assert.NotNull(resultType);
        Assert.Null(LoadWebAppAssembly().GetType("Air.Cloud.WebApp.DataValidation.Internal.DataValidationResult"));
    }

    [Fact]
    public void DataValidation_should_remove_ambiguous_legacy_entrypoints()
    {
        var validatorType = GetWebAppType("Air.Cloud.WebApp.DataValidation.Validators.DataValidator");
        var extensionType = GetWebAppType("Air.Cloud.WebApp.DataValidation.Extensions.DataValidationExtensions");

        Assert.DoesNotContain(validatorType.GetMethods(BindingFlags.Public | BindingFlags.Static), method => method.Name == "TryValidateValue");
        Assert.DoesNotContain(validatorType.GetMethods(BindingFlags.Public | BindingFlags.Static), method => method.Name == "TryValidateObject");
        Assert.DoesNotContain(extensionType.GetMethods(BindingFlags.Public | BindingFlags.Static), method => method.Name == "TryValidate");
        Assert.DoesNotContain(extensionType.GetMethods(BindingFlags.Public | BindingFlags.Static), method => method.Name == "Validate");
    }

    private static object InvokeDataValidationExtension(string methodName, params object[] parameters)
    {
        var extensionType = GetWebAppType("Air.Cloud.WebApp.DataValidation.Extensions.DataValidationExtensions");
        var method = extensionType.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(candidate => candidate.Name == methodName)
            .Single(candidate => IsMethodMatch(candidate, parameters));

        return method.Invoke(null, parameters)!;
    }

    private static object InvokeDataValidator(string methodName, params object[] parameters)
    {
        var validatorType = GetWebAppType("Air.Cloud.WebApp.DataValidation.Validators.DataValidator");
        var method = validatorType.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(candidate => candidate.Name == methodName)
            .Single(candidate => IsMethodMatch(candidate, parameters));

        return method.Invoke(null, parameters)!;
    }

    private static object InvokeValidatorAccessor(IValidatableObject instance)
    {
        var extensionType = GetWebAppType("Air.Cloud.WebApp.DataValidation.Extensions.DataValidationExtensions");
        var method = extensionType.GetMethod(
            "Validator",
            BindingFlags.Public | BindingFlags.Static,
            new[] { typeof(IValidatableObject) })!;

        return method.Invoke(null, new object[] { instance })!;
    }

    private static object CreateDataValidationAttribute(object validationType)
    {
        var attributeType = GetWebAppType("Air.Cloud.WebApp.DataValidation.Attributes.DataValidationAttribute");
        return Activator.CreateInstance(attributeType, new object[] { new[] { validationType } })!;
    }

    private static object CreateDataValidationAttribute(object validationPattern, params object[] validationTypes)
    {
        var attributeType = GetWebAppType("Air.Cloud.WebApp.DataValidation.Attributes.DataValidationAttribute");
        return Activator.CreateInstance(attributeType, new object[] { validationPattern, validationTypes })!;
    }

    private static ValidationResult InvokeValidationAttribute(object attribute, string memberName, object value)
    {
        var method = attribute.GetType().GetMethod("IsValid", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var context = new ValidationContext(new object())
        {
            MemberName = memberName
        };

        return (ValidationResult)method.Invoke(attribute, new[] { value, context })!;
    }

    private static object GetValidationTypeValue(string name)
    {
        var validationTypes = GetWebAppType("Air.Cloud.WebApp.DataValidation.Enums.ValidationTypes");
        return Enum.Parse(validationTypes, name);
    }

    private static object GetValidationPatternValue(string name)
    {
        var validationPattern = GetWebAppType("Air.Cloud.WebApp.DataValidation.Enums.ValidationPattern");
        return Enum.Parse(validationPattern, name);
    }

    private static string[] GetValidationMessages(object result)
    {
        var validationResults = GetProperty<ICollection<ValidationResult>>(result, "ValidationResults");
        return validationResults.Select(item => item.ErrorMessage!).ToArray();
    }

    private static bool IsMethodMatch(MethodInfo method, object[] parameters)
    {
        var methodParameters = method.GetParameters();
        if (methodParameters.Length != parameters.Length)
        {
            return false;
        }

        for (var index = 0; index < parameters.Length; index++)
        {
            var parameterType = methodParameters[index].ParameterType;
            var value = parameters[index];

            if (value == null)
            {
                continue;
            }

            if (parameterType.IsAssignableFrom(value.GetType()))
            {
                continue;
            }

            if (parameterType.IsArray && value.GetType().IsArray)
            {
                var parameterElementType = parameterType.GetElementType()!;
                var valueElementType = value.GetType().GetElementType()!;

                if (parameterElementType.IsAssignableFrom(valueElementType))
                {
                    continue;
                }
            }

            return false;
        }

        return true;
    }

    private static bool HasProperty(object value, string propertyName)
    {
        return value.GetType().GetProperty(propertyName) != null;
    }

    private static bool HasMethod(Type type, string methodName)
    {
        return type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
            .Any(method => method.Name == methodName);
    }

    private static T GetProperty<T>(object value, string propertyName)
    {
        return (T)value.GetType().GetProperty(propertyName)!.GetValue(value)!;
    }

    private static void SetProperty(object value, string propertyName, object propertyValue)
    {
        value.GetType().GetProperty(propertyName)!.SetValue(value, propertyValue);
    }

    private static object InvokeInstance(object instance, string methodName, params object[] parameters)
    {
        var method = instance.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(candidate => candidate.Name == methodName)
            .Single(candidate => IsMethodMatch(candidate, parameters));

        return method.Invoke(instance, parameters)!;
    }

    private static Type GetWebAppType(string fullName)
    {
        return LoadWebAppAssembly().GetType(fullName, throwOnError: true)!;
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

    private sealed class InstanceValidationSample : IValidatableObject
    {
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            yield break;
        }
    }

    private sealed class RequiredModel
    {
        [Required(ErrorMessage = "Name is required")]
        public string? Name { get; set; }
    }

    private sealed class ObjectModelValidationSample : IValidatableObject
    {
        public bool ShouldFail { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (ShouldFail)
            {
                yield return new ValidationResult("custom model failure", new[] { nameof(ShouldFail) });
            }
        }
    }
}
