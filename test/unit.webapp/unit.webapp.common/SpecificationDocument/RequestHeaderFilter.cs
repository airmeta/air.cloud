using Air.Cloud.Core.App;
using Air.Cloud.Core.Enums;

using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;

using Swashbuckle.AspNetCore.SwaggerGen;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace unit.webapp.common.SpecificationDocument
{
    public class RequestHeaderFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var actionAllowAnonymous = context.MethodInfo.GetCustomAttributes(true).OfType<AllowAnonymousAttribute>().Any();
            if (!actionAllowAnonymous )
            {
                operation.Parameters.Add(new OpenApiParameter
                {
                    AllowEmptyValue = true,
                    Description = "身份令牌",
                    Name = "Authorization",
                    Required = true,
                    In = ParameterLocation.Header
                });
            }
        }
    }
}
