namespace Predictathon.WebApi.HealthChecks;

/// <summary>
/// Gates an endpoint behind a shared-secret API key, supplied via the "X-Api-Key" request header.
/// </summary>
public sealed class ApiKeyEndpointFilter : IEndpointFilter
{
    private readonly string? _apiKey;

    /// <summary>
    /// Initialises a new instance of the <see cref="ApiKeyEndpointFilter"/> class.
    /// </summary>
    /// <param name="apiKey">The expected API key. When null or empty, the filter allows every
    /// request through unchecked, so the endpoint is effectively unprotected.</param>
    public ApiKeyEndpointFilter(string? apiKey)
    {
        _apiKey = apiKey;
    }

    /// <summary>
    /// Rejects the request with 401 Unauthorized unless it carries an "X-Api-Key" header matching
    /// the configured key. Skipped entirely when no key is configured.
    /// </summary>
    /// <param name="context">The invocation context for the current request.</param>
    /// <param name="next">The next filter or endpoint delegate in the pipeline.</param>
    public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            return next(context);
        }

        var providedKey = context.HttpContext.Request.Headers["X-Api-Key"].ToString();
        if (!string.Equals(providedKey, _apiKey, StringComparison.Ordinal))
        {
            return ValueTask.FromResult<object?>(Results.Unauthorized());
        }

        return next(context);
    }
}
