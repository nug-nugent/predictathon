using FluentResults;
using Microsoft.AspNetCore.Mvc;
using Predictathon.Application.Errors;
using System.Security.Claims;

namespace Predictathon.WebApi.Controllers.Base
{
    [ApiController]
    [Route("api/[controller]")]
    public abstract class ApiControllerBase : ControllerBase
    {
        /// <summary>
        /// The authenticated caller's user id, from the JWT's NameIdentifier claim. Only valid to
        /// read from an [Authorize]-protected action, which guarantees the claim is present.
        /// </summary>
        protected Guid CurrentUserId =>
            Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

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
        /// Convert a FluentResults Result&lt;T&gt; into an ActionResult. On success returns 200 with the value.
        /// On failure returns either a ValidationProblemDetails (400) when errors are property-specific,
        /// or a ProblemDetails with 404 when a <see cref="NotFoundError"/> is present, or a generic 400 otherwise.
        /// </summary>
        protected ActionResult<T?> FromResult<T>(Result<T> result) where T : class
        {
            if (result.IsSuccess)
            {
                return Ok(result.Value);
            }

            return BuildErrorResult(result.Errors);
        }

        /// <summary>
        /// Convert a non-generic FluentResults Result into an ActionResult. On success returns 204 No Content.
        /// On failure this follows the same error mapping as <see cref="FromResult{T}"/>.
        /// </summary>
        protected ActionResult FromResult(Result result)
        {
            if (result.IsSuccess)
            {
                return NoContent();
            }

            return BuildErrorResult(result.Errors);
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
        /// Map a failed Result's errors onto a ProblemDetails ActionResult. A <see cref="NotFoundError"/>
        /// takes priority and produces a 404; otherwise errors are grouped by <see cref="PropertyValidationError.PropertyName"/>
        /// (falling back to a general key for non-property errors) into a 400 ValidationProblemDetails.
        /// </summary>
        private ActionResult BuildErrorResult(IReadOnlyList<IError> errors)
        {
            var notFound = errors.OfType<NotFoundError>().FirstOrDefault();
            if (notFound is not null)
            {
                var pd = BuildProblemDetails(
                    status: StatusCodes.Status404NotFound,
                    type: "https://httpstatuses.com/404",
                    title: "Not Found",
                    detail: notFound.Message);

                return ProblemResult(pd);
            }

            var conflict = errors.OfType<ConflictError>().FirstOrDefault();
            if (conflict is not null)
            {
                var pd = BuildProblemDetails(
                    status: StatusCodes.Status409Conflict,
                    type: "https://httpstatuses.com/409",
                    title: "Conflict",
                    detail: conflict.Message);

                return ProblemResult(pd);
            }

            var unauthorized = errors.OfType<UnauthorizedError>().FirstOrDefault();
            if (unauthorized is not null)
            {
                var pd = BuildProblemDetails(
                    status: StatusCodes.Status401Unauthorized,
                    type: "https://httpstatuses.com/401",
                    title: "Unauthorized",
                    detail: unauthorized.Message);

                return ProblemResult(pd);
            }

            var modelErrors = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            foreach (var err in errors)
            {
                var key = err is PropertyValidationError propertyError ? propertyError.PropertyName : string.Empty;

                if (!modelErrors.TryGetValue(key, out var list))
                {
                    list = new List<string>();
                    modelErrors[key] = list;
                }

                list.Add(err.Message);
            }

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
