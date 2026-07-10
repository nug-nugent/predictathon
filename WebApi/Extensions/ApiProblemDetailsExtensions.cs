using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using System.Text.Json;

namespace Predictathon.WebApi.Extensions;

public static class ApiProblemDetailsExtensions
{
    /// <summary>
    /// Register services required to produce RFC7807 ProblemDetails responses
    /// for model validation and allow controllers to return ProblemDetails easily.
    /// Call this after AddControllers().
    /// </summary>
    public static IServiceCollection AddApiProblemDetails(this IServiceCollection services)
    {
        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var vpd = new ValidationProblemDetails(context.ModelState)
                {
                    Type = "https://example.com/probs/validation",
                    Title = "One or more validation errors occurred.",
                    Status = StatusCodes.Status400BadRequest,
                    Instance = context.HttpContext.Request.Path
                };

                return new BadRequestObjectResult(vpd) { ContentTypes = { "application/problem+json" } };
            };
        });

        return services;
    }

    /// <summary>
    /// Add middleware for global exception handling and status code pages that
    /// emit application/problem+json bodies. Call this after app.Build() and before MapControllers().
    /// </summary>
    public static WebApplication UseApiProblemDetails(this WebApplication app)
    {
        var env = app.Environment;

        app.UseExceptionHandler(errorApp =>
        {
            errorApp.Run(async context =>
            {
                var feature = context.Features.Get<IExceptionHandlerFeature>();
                var ex = feature?.Error;

                var pd = new ProblemDetails
                {
                    Type = "https://example.com/probs/internal",
                    Title = "An unexpected error occurred.",
                    Status = StatusCodes.Status500InternalServerError,
                    Detail = env.IsDevelopment() ? ex?.Message : null,
                    Instance = context.Request.Path
                };

                context.Response.StatusCode = pd.Status.HasValue ? pd.Status.Value : StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/problem+json";
                await context.Response.WriteAsJsonAsync(pd, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            });
        });

        app.UseStatusCodePages(async ctx =>
        {
            var resp = ctx.HttpContext.Response;
            if (resp.ContentLength == null || resp.ContentLength == 0)
            {
                var pd = new ProblemDetails
                {
                    Status = resp.StatusCode,
                    Title = ReasonPhrases.GetReasonPhrase(resp.StatusCode),
                    Instance = ctx.HttpContext.Request.Path
                };

                resp.ContentType = "application/problem+json";
                await resp.WriteAsJsonAsync(pd);
            }
        });

        return app;
    }
}
