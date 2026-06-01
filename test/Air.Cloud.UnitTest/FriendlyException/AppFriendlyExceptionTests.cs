using System.Reflection;

namespace Air.Cloud.UnitTest.FriendlyException
{
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
            Assert.Equal(500, exceptionType.GetProperty("StatusCode")!.GetValue(exception));
            Assert.False((bool)exceptionType.GetProperty("ValidationException")!.GetValue(exception)!);
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
    }
}
