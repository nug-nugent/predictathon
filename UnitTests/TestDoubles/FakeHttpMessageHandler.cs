using System.Net;

namespace Predictathon.UnitTests.TestDoubles;

/// <summary>
/// Queue-based <see cref="HttpMessageHandler"/> stand-in for tests that exercise services calling
/// out over <see cref="HttpClient"/> (e.g. PayPalService). Responses are dequeued in request order;
/// every sent request is recorded for assertions.
/// </summary>
public class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<Func<HttpResponseMessage>> _responses = new();

    public List<HttpRequestMessage> Requests { get; } = [];

    public void Enqueue(HttpStatusCode statusCode, string? jsonContent = null)
    {
        _responses.Enqueue(() =>
        {
            var response = new HttpResponseMessage(statusCode);
            if (jsonContent is not null)
            {
                response.Content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");
            }

            return response;
        });
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);

        if (_responses.Count == 0)
        {
            throw new InvalidOperationException($"No queued response for request to {request.RequestUri}.");
        }

        return Task.FromResult(_responses.Dequeue()());
    }
}
