using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Predictathon.Application.Options;
using Predictathon.Application.Services;
using Predictathon.UnitTests.TestDoubles;
using System.Net;
using System.Reflection;

namespace Predictathon.UnitTests.Services;

public class PayPalServiceTests
{
    private const string TokenResponseJson = """{"access_token":"test-access-token","expires_in":32400}""";

    public PayPalServiceTests()
    {
        // PayPalService caches its OAuth2 token in private static fields (shared across every
        // instance/test in the process) - reset them before each test so tests don't leak state
        // into one another.
        typeof(PayPalService).GetField("_cachedAccessToken", BindingFlags.Static | BindingFlags.NonPublic)!.SetValue(null, null);
        typeof(PayPalService).GetField("_cachedAccessTokenExpiresAtUtc", BindingFlags.Static | BindingFlags.NonPublic)!.SetValue(null, default(DateTime));
    }

    private static (FakeHttpMessageHandler Handler, PayPalService Service) MakeService(string mode = "sandbox")
    {
        var handler = new FakeHttpMessageHandler();
        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory.Setup(f => f.CreateClient(nameof(PayPalService))).Returns(() => new HttpClient(handler));
        var options = Options.Create(new PayPalOptions { Mode = mode, ClientId = "client-id", ClientSecret = "client-secret" });
        var service = new PayPalService(httpClientFactory.Object, options);
        return (handler, service);
    }

    [Fact]
    public async Task CreateOrderAsync_FetchesTokenThenCreatesOrder()
    {
        var (handler, service) = MakeService();
        handler.Enqueue(HttpStatusCode.OK, TokenResponseJson);
        handler.Enqueue(HttpStatusCode.Created, """{"id":"ORDER123"}""");

        var order = await service.CreateOrderAsync(12.50m);

        order.OrderId.Should().Be("ORDER123");
        handler.Requests.Should().HaveCount(2);
        handler.Requests[0].RequestUri!.ToString().Should().Contain("/v1/oauth2/token");
        handler.Requests[1].RequestUri!.ToString().Should().Contain("/v2/checkout/orders");
        handler.Requests[1].Headers.Authorization!.Parameter.Should().Be("test-access-token");
    }

    [Fact]
    public async Task CaptureOrderAsync_FetchesTokenThenCapturesOrder()
    {
        var (handler, service) = MakeService();
        handler.Enqueue(HttpStatusCode.OK, TokenResponseJson);
        handler.Enqueue(HttpStatusCode.OK, """
            {
                "status": "COMPLETED",
                "purchase_units": [
                    { "payments": { "captures": [ { "id": "CAPTURE123", "amount": { "value": "12.50" } } ] } }
                ]
            }
            """);

        var capture = await service.CaptureOrderAsync("ORDER123");

        capture.Status.Should().Be("COMPLETED");
        capture.CaptureId.Should().Be("CAPTURE123");
        capture.AmountCaptured.Should().Be(12.50m);
        handler.Requests[1].RequestUri!.ToString().Should().Contain("/v2/checkout/orders/ORDER123/capture");
    }

    [Fact]
    public async Task GetAccessToken_CachedAcrossCalls_OnlyFetchesTokenOnce()
    {
        var (handler, service) = MakeService();
        handler.Enqueue(HttpStatusCode.OK, TokenResponseJson);
        handler.Enqueue(HttpStatusCode.Created, """{"id":"ORDER1"}""");
        handler.Enqueue(HttpStatusCode.Created, """{"id":"ORDER2"}""");

        await service.CreateOrderAsync(10m);
        await service.CreateOrderAsync(20m);

        handler.Requests.Should().HaveCount(3);
        handler.Requests.Count(r => r.RequestUri!.ToString().Contains("/v1/oauth2/token")).Should().Be(1);
    }

    [Fact]
    public async Task CreateOrderAsync_SandboxMode_CallsSandboxHost()
    {
        var (handler, service) = MakeService(mode: "sandbox");
        handler.Enqueue(HttpStatusCode.OK, TokenResponseJson);
        handler.Enqueue(HttpStatusCode.Created, """{"id":"ORDER1"}""");

        await service.CreateOrderAsync(10m);

        handler.Requests.Should().OnlyContain(r => r.RequestUri!.Host == "api-m.sandbox.paypal.com");
    }

    [Fact]
    public async Task CreateOrderAsync_LiveMode_CallsLiveHost()
    {
        var (handler, service) = MakeService(mode: "live");
        handler.Enqueue(HttpStatusCode.OK, TokenResponseJson);
        handler.Enqueue(HttpStatusCode.Created, """{"id":"ORDER1"}""");

        await service.CreateOrderAsync(10m);

        handler.Requests.Should().OnlyContain(r => r.RequestUri!.Host == "api-m.paypal.com");
    }

    [Fact]
    public async Task CreateOrderAsync_TokenRequestFails_Throws()
    {
        var (handler, service) = MakeService();
        handler.Enqueue(HttpStatusCode.Unauthorized);

        var act = () => service.CreateOrderAsync(10m);

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task CreateOrderAsync_OrderRequestFails_Throws()
    {
        var (handler, service) = MakeService();
        handler.Enqueue(HttpStatusCode.OK, TokenResponseJson);
        handler.Enqueue(HttpStatusCode.BadRequest);

        var act = () => service.CreateOrderAsync(10m);

        await act.Should().ThrowAsync<HttpRequestException>();
    }
}
