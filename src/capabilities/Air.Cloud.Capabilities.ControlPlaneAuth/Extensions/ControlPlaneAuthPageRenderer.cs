using System.Reflection;
using System.Text.Json;

using Air.Cloud.Capabilities.ControlPlaneAuth.Options;

namespace Air.Cloud.Capabilities.ControlPlaneAuth.Extensions;

internal static class ControlPlaneAuthPageRenderer
{
    private const string AuthTemplateResourceName = "Air.Cloud.Capabilities.ControlPlaneAuth.PageTemplates.control-auth.html";
    private const string ShellTemplateResourceName = "Air.Cloud.Capabilities.ControlPlaneAuth.PageTemplates.control-plane.html";

    public static string CreateAuthPage(ControlPlaneAuthOptions options)
    {
        var shellTitle = string.IsNullOrWhiteSpace(options.Shell.Title) ? options.ProductName : options.Shell.Title;
        var policy = options.PasswordPolicy;
        return LoadTemplate(AuthTemplateResourceName)
            .Replace("__QR_CODE_JS__", LoadTemplate("Air.Cloud.Capabilities.ControlPlaneAuth.PageTemplates.qrcode-generator.js"), StringComparison.Ordinal)
            .Replace("__PRODUCT_NAME_HTML__", Html(options.ProductName), StringComparison.Ordinal)
            .Replace("__SHELL_TITLE_HTML__", Html(shellTitle), StringComparison.Ordinal)
            .Replace("__API_BASE_PATH_JS__", JavaScript(options.BasePath), StringComparison.Ordinal)
            .Replace("__AUTH_PATH_JS__", JavaScript(options.AuthPath), StringComparison.Ordinal)
            .Replace("__PLANE_PATH_JS__", JavaScript(options.PlanePath), StringComparison.Ordinal)
            .Replace("__DEFAULT_RETURN_URL_JS__", JavaScript(string.IsNullOrWhiteSpace(options.DefaultReturnUrl) ? options.PlanePath : options.DefaultReturnUrl), StringComparison.Ordinal)
            .Replace("__ENABLE_MFA_JS__", JavaScript(options.EnableMFA), StringComparison.Ordinal)
            .Replace("__PASSWORD_MIN_LENGTH_JS__", policy.MinLength.ToString(), StringComparison.Ordinal)
            .Replace("__REQUIRE_DIGIT_JS__", JavaScript(policy.RequireDigit), StringComparison.Ordinal)
            .Replace("__REQUIRE_LOWERCASE_JS__", JavaScript(policy.RequireLowercase), StringComparison.Ordinal)
            .Replace("__REQUIRE_UPPERCASE_JS__", JavaScript(policy.RequireUppercase), StringComparison.Ordinal)
            .Replace("__REQUIRE_SPECIAL_CHARACTER_JS__", JavaScript(policy.RequireSpecialCharacter), StringComparison.Ordinal);
    }

    public static string CreateShellPage(ControlPlaneAuthOptions options)
    {
        var shellTitle = string.IsNullOrWhiteSpace(options.Shell.Title) ? options.ProductName : options.Shell.Title;
        var frames = options.Shell.Frames
            .Where(item => !string.IsNullOrWhiteSpace(item.Url))
            .Select(item => new { item.Key, item.Title, item.Url, item.Embedded });
        return LoadTemplate(ShellTemplateResourceName)
            .Replace("__PRODUCT_NAME_HTML__", Html(options.ProductName), StringComparison.Ordinal)
            .Replace("__PRODUCT_NAME_JS__", JavaScript(options.ProductName), StringComparison.Ordinal)
            .Replace("__SHELL_TITLE_HTML__", Html(shellTitle), StringComparison.Ordinal)
            .Replace("__API_BASE_PATH_JS__", JavaScript(options.BasePath), StringComparison.Ordinal)
            .Replace("__AUTH_PATH_JS__", JavaScript(options.AuthPath), StringComparison.Ordinal)
            .Replace("__PLANE_PATH_JS__", JavaScript(options.PlanePath), StringComparison.Ordinal)
            .Replace("__FRAMES_JS__", JsonSerializer.Serialize(frames), StringComparison.Ordinal)
            .Replace("__DEFAULT_MENU_KEY_JS__", JavaScript(options.Shell.DefaultMenuKey), StringComparison.Ordinal)
            .Replace("__SHOW_SERVICE_TICKETS_JS__", JavaScript(options.Shell.ShowServiceTickets), StringComparison.Ordinal)
            .Replace("__SESSION_QUERY_NAME_JS__", JavaScript(options.Session.QueryName), StringComparison.Ordinal);
    }

    private static string LoadTemplate(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Cannot find embedded ControlPlaneAuth page template: {resourceName}");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static string Html(string value)
    {
        return value.Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal)
            .Replace("'", "&#39;", StringComparison.Ordinal);
    }

    private static string JavaScript(string value) => JsonSerializer.Serialize(value);

    private static string JavaScript(bool value) => value ? "true" : "false";
}
