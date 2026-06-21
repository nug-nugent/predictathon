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
