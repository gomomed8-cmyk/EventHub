using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using EventHub.Application.Contracts.Payments;
using EventHub.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace EventHub.Infrastructure.Services.Payments
{
    public sealed class PaymobPaymentGateway : IPaymentGateway
    {
        private readonly HttpClient _httpClient;
        private readonly PaymobOptions _options;

        public PaymobPaymentGateway(
            HttpClient httpClient,
            IOptions<PaymobOptions> options)
        {
            _httpClient = httpClient;
            _options = options.Value;
        }

        public async Task<PaymentCheckoutResult> CreateCheckoutAsync(
            PaymentCheckoutRequest request,
            CancellationToken cancellationToken = default)
        {
            ValidateRequest(request);

            var secondsUntilExpiry = (request.ExpiresAt - DateTime.UtcNow)
                .TotalSeconds;

            if (secondsUntilExpiry <= 0)
            {
                throw new InvalidOperationException(
                    "The booking hold has expired.");
            }

            if (secondsUntilExpiry > int.MaxValue)
            {
                throw new InvalidOperationException(
                    "The booking expiration value is too large.");
            }

            var payload = new CreateIntentionPayload
            {
                Amount = ToMinorUnits(request.Amount),
                Currency = request.Currency,
                PaymentMethods = new[] { _options.IntegrationId },

                SpecialReference = request.PaymentId.ToString("N"),
                Expiration = (int)Math.Ceiling(secondsUntilExpiry),

                NotificationUrl = _options.NotificationUrl,
                RedirectionUrl = _options.RedirectionUrl,

                Items = request.Items.Select(item => new PaymobItem
                {
                    Name = item.Name,
                    Description = item.Description ?? string.Empty,
                    Amount = ToMinorUnits(item.UnitPrice),
                    Quantity = item.Quantity
                }).ToArray(),

                BillingData = MapBillingData(request.BillingData)
            };

            var endpoint =
                $"{_options.BaseUrl.TrimEnd('/')}/v1/intention/";

            using var httpRequest = new HttpRequestMessage(
                HttpMethod.Post,
                endpoint);

            httpRequest.Headers.Authorization =
                new AuthenticationHeaderValue(
                    "Token",
                    _options.SecretKey);

            httpRequest.Content = JsonContent.Create(payload);

            using var response = await _httpClient.SendAsync(
                httpRequest,
                cancellationToken);

            var responseBody = await response.Content.ReadAsStringAsync(
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"Paymob intention creation failed. " +
                    $"HTTP status: {(int)response.StatusCode}. " +
                    $"Response: {responseBody}",
                    null,
                    response.StatusCode);
            }

            var intention =
                JsonSerializer.Deserialize<PaymobIntentionResponse>(
                    responseBody,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

            if (intention is null ||
                string.IsNullOrWhiteSpace(intention.Id) ||
                string.IsNullOrWhiteSpace(intention.ClientSecret))
            {
                throw new InvalidOperationException(
                    "Paymob returned an invalid intention response.");
            }

            var orderId =
                intention.IntentionOrderId?.ToString(
                    CultureInfo.InvariantCulture)
                ?? intention.PaymentKeys?
                    .FirstOrDefault()?
                    .OrderId?
                    .ToString(CultureInfo.InvariantCulture);

            var checkoutUrl = BuildCheckoutUrl(
                intention.ClientSecret);

            return new PaymentCheckoutResult(
                intention.Id,
                orderId,
                checkoutUrl);
        }

        private void ValidateRequest(PaymentCheckoutRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (request.PaymentId == Guid.Empty)
            {
                throw new ArgumentException(
                    "PaymentId is required.");
            }

            if (request.BookingId == Guid.Empty)
            {
                throw new ArgumentException(
                    "BookingId is required.");
            }

            if (request.Amount <= 0)
            {
                throw new ArgumentException(
                    "Payment amount must be greater than zero.");
            }

            if (string.IsNullOrWhiteSpace(request.Currency) ||
                !string.Equals(
                    request.Currency,
                    _options.Currency,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    "Payment currency does not match Paymob configuration.");
            }

            if (request.Items is null || request.Items.Count == 0)
            {
                throw new ArgumentException(
                    "At least one checkout item is required.");
            }

            if (request.Items.Any(item =>
                    string.IsNullOrWhiteSpace(item.Name) ||
                    item.Quantity <= 0 ||
                    item.UnitPrice <= 0))
            {
                throw new ArgumentException(
                    "Checkout items must have a name, positive quantity " +
                    "and positive unit price.");
            }

            var itemsTotal = request.Items.Sum(
                item => item.UnitPrice * item.Quantity);

            if (decimal.Round(itemsTotal, 2) !=
                decimal.Round(request.Amount, 2))
            {
                throw new ArgumentException(
                    "Checkout item totals do not match the payment amount.");
            }

            ValidateBillingData(request.BillingData);
            ValidateConfiguration();
        }

        private void ValidateConfiguration()
        {
            if (string.IsNullOrWhiteSpace(_options.SecretKey))
            {
                throw new InvalidOperationException(
                    "Paymob SecretKey is required.");
            }

            if (string.IsNullOrWhiteSpace(_options.PublicKey))
            {
                throw new InvalidOperationException(
                    "Paymob PublicKey is required.");
            }

            if (string.IsNullOrWhiteSpace(_options.BaseUrl) ||
                !Uri.TryCreate(
                    _options.BaseUrl,
                    UriKind.Absolute,
                    out var baseUri) ||
                baseUri.Scheme != Uri.UriSchemeHttps)
            {
                throw new InvalidOperationException(
                    "Paymob BaseUrl must be a valid HTTPS URL.");
            }

            if (string.IsNullOrWhiteSpace(_options.CheckoutBaseUrl) ||
                !Uri.TryCreate(
                    _options.CheckoutBaseUrl,
                    UriKind.Absolute,
                    out var checkoutUri) ||
                checkoutUri.Scheme != Uri.UriSchemeHttps)
            {
                throw new InvalidOperationException(
                    "Paymob CheckoutBaseUrl must be a valid HTTPS URL.");
            }

            if (string.IsNullOrWhiteSpace(_options.NotificationUrl) ||
                !Uri.TryCreate(
                    _options.NotificationUrl,
                    UriKind.Absolute,
                    out var notificationUri) ||
                notificationUri.Scheme != Uri.UriSchemeHttps)
            {
                throw new InvalidOperationException(
                    "Paymob NotificationUrl must be a valid HTTPS URL.");
            }

            if (_options.IntegrationId <= 0)
            {
                throw new InvalidOperationException(
                    "Paymob IntegrationId is invalid.");
            }

            if (string.IsNullOrWhiteSpace(_options.Currency))
            {
                throw new InvalidOperationException(
                    "Paymob Currency is required.");
            }
        }

        private static void ValidateBillingData(
            PaymentBillingData billing)
        {
            ArgumentNullException.ThrowIfNull(billing);

            if (string.IsNullOrWhiteSpace(billing.FirstName) ||
                string.IsNullOrWhiteSpace(billing.LastName) ||
                string.IsNullOrWhiteSpace(billing.Email) ||
                string.IsNullOrWhiteSpace(billing.PhoneNumber) ||
                string.IsNullOrWhiteSpace(billing.Country))
            {
                throw new ArgumentException(
                    "Billing first name, last name, email, phone number " +
                    "and country are required.");
            }
        }

        private string BuildCheckoutUrl(string clientSecret)
        {
            var builder = new UriBuilder(_options.CheckoutBaseUrl);

            var query = new Dictionary<string, string>
            {
                ["publicKey"] = _options.PublicKey,
                ["clientSecret"] = clientSecret
            };

            builder.Query = string.Join(
                "&",
                query.Select(item =>
                    $"{Uri.EscapeDataString(item.Key)}=" +
                    $"{Uri.EscapeDataString(item.Value)}"));

            return builder.Uri.ToString();
        }

        private static int ToMinorUnits(decimal amount)
        {
            var minorUnits = decimal.Round(
                amount * 100m,
                0,
                MidpointRounding.AwayFromZero);

            return checked((int)minorUnits);
        }

        private static PaymobBillingData MapBillingData(
            PaymentBillingData billing)
        {
            ArgumentNullException.ThrowIfNull(billing);

            return new PaymobBillingData
            {
                FirstName = billing.FirstName,
                LastName = billing.LastName,
                Email = billing.Email,
                PhoneNumber = billing.PhoneNumber,

                Apartment = billing.Apartment ?? string.Empty,
                Floor = billing.Floor ?? string.Empty,
                Street = billing.Street ?? string.Empty,
                Building = billing.Building ?? string.Empty,

                City = billing.City ?? string.Empty,
                State = billing.State ?? string.Empty,
                Country = billing.Country,
                PostalCode = billing.PostalCode ?? string.Empty
            };
        }

        private sealed class CreateIntentionPayload
        {
            [JsonPropertyName("amount")]
            public int Amount { get; init; }

            [JsonPropertyName("currency")]
            public string Currency { get; init; } = string.Empty;

            [JsonPropertyName("payment_methods")]
            public int[] PaymentMethods { get; init; } = [];

            [JsonPropertyName("items")]
            public PaymobItem[] Items { get; init; } = [];

            [JsonPropertyName("billing_data")]
            public PaymobBillingData BillingData { get; init; } = new();

            [JsonPropertyName("special_reference")]
            public string SpecialReference { get; init; } = string.Empty;

            [JsonPropertyName("expiration")]
            public int Expiration { get; init; }

            [JsonPropertyName("notification_url")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string? NotificationUrl { get; init; }

            [JsonPropertyName("redirection_url")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string? RedirectionUrl { get; init; }
        }

        private sealed class PaymobItem
        {
            [JsonPropertyName("name")]
            public string Name { get; init; } = string.Empty;

            [JsonPropertyName("description")]
            public string Description { get; init; } = string.Empty;

            [JsonPropertyName("amount")]
            public int Amount { get; init; }

            [JsonPropertyName("quantity")]
            public int Quantity { get; init; }
        }

        private sealed class PaymobBillingData
        {
            [JsonPropertyName("first_name")]
            public string FirstName { get; init; } = string.Empty;

            [JsonPropertyName("last_name")]
            public string LastName { get; init; } = string.Empty;

            [JsonPropertyName("email")]
            public string Email { get; init; } = string.Empty;

            [JsonPropertyName("phone_number")]
            public string PhoneNumber { get; init; } = string.Empty;

            [JsonPropertyName("apartment")]
            public string Apartment { get; init; } = string.Empty;

            [JsonPropertyName("floor")]
            public string Floor { get; init; } = string.Empty;

            [JsonPropertyName("street")]
            public string Street { get; init; } = string.Empty;

            [JsonPropertyName("building")]
            public string Building { get; init; } = string.Empty;

            [JsonPropertyName("city")]
            public string City { get; init; } = string.Empty;

            [JsonPropertyName("state")]
            public string State { get; init; } = string.Empty;

            [JsonPropertyName("country")]
            public string Country { get; init; } = "EG";

            [JsonPropertyName("postal_code")]
            public string PostalCode { get; init; } = string.Empty;
        }

        private sealed class PaymobIntentionResponse
        {
            [JsonPropertyName("id")]
            public string Id { get; init; } = string.Empty;

            [JsonPropertyName("intention_order_id")]
            public long? IntentionOrderId { get; init; }

            [JsonPropertyName("client_secret")]
            public string ClientSecret { get; init; } = string.Empty;

            [JsonPropertyName("payment_keys")]
            public List<PaymobPaymentKey>? PaymentKeys { get; init; }
        }

        private sealed class PaymobPaymentKey
        {
            [JsonPropertyName("order_id")]
            public long? OrderId { get; init; }
        }
    }
}
