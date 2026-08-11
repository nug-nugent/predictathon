using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Predictathon.Application.Attributes;
using Predictathon.Application.Interfaces;
using Predictathon.Application.Options;

namespace Predictathon.Application.Services;

[ScopedService]
public class PayPalService : IPayPalService
{
    private const string CurrencyCode = "GBP";

    // OAuth2 client-credentials token, cached across requests (not per-scope) since it's valid for
    // ~9 hours and re-fetching it on every order would be wasteful. A static lock keeps concurrent
    // refreshes from racing each other.
    private static string? _cachedAccessToken;
    private static DateTime _cachedAccessTokenExpiresAtUtc;
    private static readonly SemaphoreSlim TokenLock = new(1, 1);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptions<PayPalOptions> _options;

    public PayPalService(IHttpClientFactory httpClientFactory, IOptions<PayPalOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _options = options;
    }

    private string BaseUrl =>
        string.Equals(_options.Value.Mode, "live", StringComparison.OrdinalIgnoreCase)
            ? "https://api-m.paypal.com"
            : "https://api-m.sandbox.paypal.com";

    /// <inheritdoc />
    public async Task<PayPalOrder> CreateOrderAsync(decimal amount, CancellationToken cancellationToken = default)
    {
        var client = await CreateAuthorizedClientAsync(cancellationToken);

        var payload = new
        {
            intent = "CAPTURE",
            purchase_units = new[]
            {
                new { amount = new { currency_code = CurrencyCode, value = amount.ToString("F2", CultureInfo.InvariantCulture) } }
            },
            // Entrance fees are a digital purchase - nothing is shipped, so suppress PayPal's
            // address-collection UI rather than defaulting to GET_FROM_FILE.
            experience_context = new { shipping_preference = "NO_SHIPPING" }
        };

        var response = await client.PostAsJsonAsync($"{BaseUrl}/v2/checkout/orders", payload, cancellationToken);
        response.EnsureSuccessStatusCode();

        var order = await response.Content.ReadFromJsonAsync<OrderResponse>(cancellationToken: cancellationToken);
        return new PayPalOrder(order!.Id);
    }

    /// <inheritdoc />
    public async Task<PayPalCapture> CaptureOrderAsync(string orderId, CancellationToken cancellationToken = default)
    {
        var client = await CreateAuthorizedClientAsync(cancellationToken);

        // PayPal requires a JSON content-type on this request even though the body is empty -
        // a null HttpContent (no Content-Type header at all) gets rejected with 415.
        var response = await client.PostAsJsonAsync($"{BaseUrl}/v2/checkout/orders/{orderId}/capture", new { }, cancellationToken);
        response.EnsureSuccessStatusCode();

        var capture = await response.Content.ReadFromJsonAsync<CaptureResponse>(cancellationToken: cancellationToken);
        var captureDetail = capture!.PurchaseUnits.First().Payments.Captures.First();

        return new PayPalCapture(
            capture.Status,
            captureDetail.Id,
            decimal.Parse(captureDetail.Amount.Value, CultureInfo.InvariantCulture));
    }

    private async Task<HttpClient> CreateAuthorizedClientAsync(CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient(nameof(PayPalService));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await GetAccessTokenAsync(cancellationToken));
        return client;
    }

    private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        if (_cachedAccessToken is not null && DateTime.UtcNow < _cachedAccessTokenExpiresAtUtc)
        {
            return _cachedAccessToken;
        }

        await TokenLock.WaitAsync(cancellationToken);
        try
        {
            if (_cachedAccessToken is not null && DateTime.UtcNow < _cachedAccessTokenExpiresAtUtc)
            {
                return _cachedAccessToken;
            }

            var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/v1/oauth2/token")
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string> { ["grant_type"] = "client_credentials" })
            };
            request.Headers.Authorization = new AuthenticationHeaderValue(
                "Basic",
                Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_options.Value.ClientId}:{_options.Value.ClientSecret}")));

            var client = _httpClientFactory.CreateClient(nameof(PayPalService));
            var response = await client.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var token = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: cancellationToken);

            _cachedAccessToken = token!.AccessToken;
            // Refresh a minute early so a request never lands right on the expiry boundary.
            _cachedAccessTokenExpiresAtUtc = DateTime.UtcNow.AddSeconds(token.ExpiresIn - 60);

            return _cachedAccessToken;
        }
        finally
        {
            TokenLock.Release();
        }
    }

    private class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = "";

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }

    private class OrderResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";
    }

    private class CaptureResponse
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = "";

        [JsonPropertyName("purchase_units")]
        public List<PurchaseUnit> PurchaseUnits { get; set; } = [];

        public class PurchaseUnit
        {
            [JsonPropertyName("payments")]
            public PaymentsBlock Payments { get; set; } = new();
        }

        public class PaymentsBlock
        {
            [JsonPropertyName("captures")]
            public List<CaptureDetail> Captures { get; set; } = [];
        }

        public class CaptureDetail
        {
            [JsonPropertyName("id")]
            public string Id { get; set; } = "";

            [JsonPropertyName("amount")]
            public AmountBlock Amount { get; set; } = new();
        }

        public class AmountBlock
        {
            [JsonPropertyName("value")]
            public string Value { get; set; } = "";
        }
    }
}
