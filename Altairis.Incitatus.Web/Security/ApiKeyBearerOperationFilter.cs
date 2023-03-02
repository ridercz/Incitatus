using Swashbuckle.AspNetCore.Filters;

namespace Altairis.Incitatus.Web.Security;

public class ApiKeyBearerOperationFilter : SecurityRequirementsOperationFilter {

    public ApiKeyBearerOperationFilter() : base(securitySchemaName: ApiKeyBearerDefaults.Scheme) { }

}
