using Air.Cloud.Core.Standard;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

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
                AppStandardRealization.AssemblyScanning.Add(new KeyValuePair<string, Action<Type>>(nameof(IOperationFilter), (x) =>
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
                }));
            }
            return services;
        }
       
    }
}
