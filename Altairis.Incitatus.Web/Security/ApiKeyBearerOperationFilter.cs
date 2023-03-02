using Microsoft.AspNetCore.Authorization;
using System.Linq;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.Filters;

namespace Altairis.Incitatus.Web.Security;

public class ApiKeyBearerOperationFilter : SecurityRequirementsOperationFilter {
    
    public ApiKeyBearerOperationFilter() : base(securitySchemaName: ApiKeyBearerDefaults.Scheme) { }

}
