using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace SuperShop.Api.OpenApi;

public class BearerSecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();

        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Access token devolvido por POST /api/auth/login."
        };

        document.Info = new OpenApiInfo
        {
            Title = "SuperShop API",
            Version = "v1",
            Description = "Loja online SuperShop. Linhas AXIS e CORE."
        };

        return Task.CompletedTask;
    }
}
