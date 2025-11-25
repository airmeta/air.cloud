using Air.Cloud.Core;

using Swashbuckle.AspNetCore.SwaggerGen;

using System.Reflection;

namespace Air.Cloud.Plugins.SpecificationDocument.Extensions
{
    public static  class WebSpecificationDocumentInjectExtensions
    {
        /// <summary>
        /// Web Specification Document 注入
        /// </summary>
        /// <param name="services"></param>
        public static IServiceCollection WebSpecificationDocumentInject(this IServiceCollection services)
        {
            if (AppCore.Settings.InjectSpecificationDocument!=true) return services;
            var OperationFilterMethod = typeof(SwaggerGenOptionsExtensions)
              .GetMethods(BindingFlags.Static | BindingFlags.Public)
              .FirstOrDefault(mi => mi.Name == "OperationFilter");
            if (OperationFilterMethod != null)
            {
                AppRealization.AssemblyScanning.Add(new Core.Standard.Assemblies.Model.AssemblyScanningEvent()
                {
                    Key=nameof(IOperationFilter),
                    Description="Swagger Operation Filter 注入",
                    TargetType=typeof(IOperationFilter),
                    Action= (x) =>
                    {
                        services.AddSwaggerGen(options =>
                        {
                            try
                            {
                                var methods = OperationFilterMethod.MakeGenericMethod(x).Invoke(null, new object[] { options, new object[] { } });
                            }
                            catch (Exception ex)
                            {

                                throw;
                            }

                        });
                    }
                });
            }
            return services;
        }
       
    }
}
