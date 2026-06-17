using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

public class DragoesAdminOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var actionDescriptor = context.ApiDescription.ActionDescriptor;
        var controllerName = actionDescriptor.RouteValues["controller"];

        if (!string.Equals(controllerName, "Dragoes", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var httpMethod = context.ApiDescription.HttpMethod;
        if (httpMethod is not ("POST" or "PUT" or "DELETE"))
        {
            return;
        }

        operation.Security ??= [];
        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [
                new OpenApiSecuritySchemeReference("AdminApiKey", null, null)
            ] = []
        });
    }
}
