using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

public class DragoesAdminOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var actionDescriptor = context.ApiDescription.ActionDescriptor;
        var controllerName = actionDescriptor.RouteValues["controller"];
        var httpMethod = context.ApiDescription.HttpMethod;

        var isAdminDragaoEndpoint =
            string.Equals(controllerName, "Dragoes", StringComparison.OrdinalIgnoreCase) &&
            httpMethod is "POST" or "PUT" or "DELETE";

        var isAdminUsuarioEndpoint =
            string.Equals(controllerName, "Usuarios", StringComparison.OrdinalIgnoreCase) &&
            httpMethod is "PUT" or "DELETE";

        var isAdminQuizEndpoint =
            string.Equals(controllerName, "Quiz", StringComparison.OrdinalIgnoreCase) &&
            httpMethod is "PUT";

        if (!isAdminDragaoEndpoint && !isAdminUsuarioEndpoint && !isAdminQuizEndpoint)
        {
            return;
        }

        operation.Parameters ??= [];
        if (!operation.Parameters.Any(x => string.Equals(x.Name, "X-Admin-Api-Key", StringComparison.OrdinalIgnoreCase)))
        {
            operation.Parameters.Add(new OpenApiParameter
            {
                Name = "X-Admin-Api-Key",
                In = ParameterLocation.Header,
                Required = true,
                Description = "Chave administrativa para operações protegidas."
            });
        }
    }
}
