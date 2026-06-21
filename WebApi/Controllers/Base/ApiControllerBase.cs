using FluentResults;
using Microsoft.AspNetCore.Mvc;

namespace Predictathon.WebApi.Controllers.Base
{
    [ApiController]
    [Route("api/[controller]")]
    public abstract class ApiControllerBase : ControllerBase
    {
        /// <summary>
        /// Return 200 with the model or a ProblemDetails 404 when the model is null.
        /// </summary>
        protected ActionResult<T?> OkOrNotFound<T>(T? model) where T : class
            => ToActionResult(model);

        /// <summary>
        /// Convert a nullable model into an ActionResult. When null this returns
        /// a ProblemDetails body with status 404 and when non-null returns 200 + value.
        /// </summary>
        protected ActionResult<T?> ToActionResult<T>(T? model,
                string? notFoundType = null,
                string? notFoundTitle = null,
                string? notFoundDetail = null)
                where T : class
        {
            if (model is null)
            {
                var pd = BuildProblemDetails(
                    status: StatusCodes.Status404NotFound,
                    type: notFoundType ?? "https://httpstatuses.com/404",
                    title: notFoundTitle ?? "Not Found",
                    detail: notFoundDetail);

                return ProblemResult(pd);
            }

            return Ok(model);
        }

        /// <summary>
        /// Convert a FluentResults Result<T> into an ActionResult. On success returns 200 with the value.
        /// On failure returns either a ValidationProblemDetails (400) when errors are property-specific,
        /// or a ProblemDetails with 404 when the failure indicates not found, or a generic 400 otherwise.
        /// </summary>
        protected ActionResult<T?> FromResult<T>(Result<T> result) where T : class
        {
            if (result.IsSuccess)
            {
                return Ok(result.Value);
            }

            // If the only error is an "Entity not found" style message, return 404
            if (result.Errors.Count == 1 && string.Equals(result.Errors[0].Message, "Entity not found", StringComparison.OrdinalIgnoreCase))
            {
                var pd = new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Type = "https://httpstatuses.com/404",
                    Title = "Not Found",
                    Detail = result.Errors[0].Message,
                    Instance = Request?.Path
                };

                return new ObjectResult(pd)
                {
                    StatusCode = pd.Status,
                    ContentTypes = { "application/problem+json" }
                };
            }

            // Map errors into ValidationProblemDetails if they look like property errors (format: "Property: message")
            var modelErrors = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            foreach (var err in result.Errors)
            {
                var msg = err.Message ?? string.Empty;
                var idx = msg.IndexOf(':');
                if (idx > 0)
                {
                    var prop = msg.Substring(0, idx).Trim();
                    var em = msg.Substring(idx + 1).Trim();
                    if (!modelErrors.TryGetValue(prop, out var list))
                    {
                        list = new List<string>();
                        modelErrors[prop] = list;
                    }
                    list.Add(em);
                }
                else
                {
                    // General error - put under an empty key
                    if (!modelErrors.TryGetValue(string.Empty, out var list))
                    {
                        list = new List<string>();
                        modelErrors[string.Empty] = list;
                    }
                    list.Add(msg);
                }
            }

            if (modelErrors.Any())
            {
                var vpd = new ValidationProblemDetails(modelErrors.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToArray()))
                {
                    Type = "https://example.com/probs/validation",
                    Title = "One or more validation errors occurred.",
                    Status = StatusCodes.Status400BadRequest,
                    Instance = Request?.Path
                };

                return new ObjectResult(vpd)
                {
                    StatusCode = vpd.Status,
                    ContentTypes = { "application/problem+json" }
                };
            }

            // Fallback generic bad request
            var pdFallback = new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Type = "https://httpstatuses.com/400",
                Title = "Bad Request",
                Detail = string.Join(';', result.Errors.Select(e => e.Message)),
                Instance = Request?.Path
            };

            return new ObjectResult(pdFallback)
            {
                StatusCode = pdFallback.Status,
                ContentTypes = { "application/problem+json" }
            };
        }

        /// <summary>
        /// Create and return a BadRequest ProblemDetails result.
        /// </summary>
        protected ActionResult BadRequestProblem(string? detail = null, string? type = null, string? title = null)
        {
            var pd = BuildProblemDetails(status: StatusCodes.Status400BadRequest, type: type ?? "https://httpstatuses.com/400", title: title ?? "Bad Request", detail: detail);
            return ProblemResult(pd);
        }

        /// <summary>
        /// Create and return ValidationProblemDetails from the current ModelState.
        /// </summary>
        protected ActionResult ValidationProblemFromModelState(string? type = null, string? title = null)
        {
            var vpd = new ValidationProblemDetails(ModelState)
            {
                Type = type ?? "https://example.com/probs/validation",
                Title = title ?? "One or more validation errors occurred.",
                Status = StatusCodes.Status400BadRequest,
                Instance = Request?.Path
            };

            return new ObjectResult(vpd)
            {
                StatusCode = vpd.Status,
                ContentTypes = { "application/problem+json" }
            };
        }

        /// <summary>
        /// Helper to produce a ProblemDetails-based ObjectResult with correct status and content-type.
        /// </summary>
        private ActionResult ProblemResult(ProblemDetails pd)
        {
            pd.Instance ??= Request?.Path;
            var status = pd.Status ?? StatusCodes.Status500InternalServerError;
            return new ObjectResult(pd)
            {
                StatusCode = status,
                ContentTypes = { "application/problem+json" }
            };
        }

        /// <summary>
        /// Build a ProblemDetails instance with the provided values.
        /// </summary>
        private ProblemDetails BuildProblemDetails(int status, string? type = null, string? title = null, string? detail = null)
            => new ProblemDetails
            {
                Status = status,
                Type = type,
                Title = title,
                Detail = detail,
                Instance = Request?.Path
            };
    }
}
