using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ExpenseFlow.Api.OpenApi;

/// <summary>
/// Configures OpenAPI/Swagger documentation generation for the ExpenseFlow API.
/// </summary>
public static class OpenApiConfiguration
{
    /// <summary>
    /// Adds OpenAPI documentation services with ExpenseFlow-specific configuration.
    /// </summary>
    public static IServiceCollection AddExpenseFlowOpenApi(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "ExpenseFlow API",
                Version = "v1",
                Description = "Enterprise expense management and reconciliation API",
                Contact = new OpenApiContact
                {
                    Name = "ExpenseFlow Team"
                }
            });

            // Configure security scheme for JWT Bearer authentication
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Enter your JWT token"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });

            // Include XML comments if available
            var xmlFiles = Directory.GetFiles(AppContext.BaseDirectory, "*.xml");
            foreach (var xmlFile in xmlFiles)
            {
                try
                {
                    options.IncludeXmlComments(xmlFile);
                }
                catch
                {
                    // Ignore files that aren't valid XML documentation
                }
            }

            // Configure schema generation
            options.UseAllOfToExtendReferenceSchemas();
            options.SupportNonNullableReferenceTypes();

            // Add operation filters for additional metadata
            options.OperationFilter<CategoryOperationFilter>();
        });

        return services;
    }

    /// <summary>
    /// Adds category tags to operations based on controller attributes.
    /// </summary>
    private class CategoryOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            // Add response types documentation
            if (!operation.Responses.ContainsKey("401"))
            {
                operation.Responses.Add("401", new OpenApiResponse
                {
                    Description = "Unauthorized - Invalid or missing authentication token"
                });
            }

            if (!operation.Responses.ContainsKey("500"))
            {
                operation.Responses.Add("500", new OpenApiResponse
                {
                    Description = "Internal Server Error"
                });
            }
        }
    }
}
