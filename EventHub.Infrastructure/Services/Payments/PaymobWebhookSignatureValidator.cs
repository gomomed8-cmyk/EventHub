
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using EventHub.Application.Contracts.Payments;
using EventHub.Infrastructure.Options;

using Microsoft.Extensions.Options;

namespace EventHub.Infrastructure.Services.Payments
{
    public sealed class PaymobWebhookSignatureValidator
        : IPaymobWebhookSignatureValidator
    {
        private readonly PaymobOptions _options;

        private static readonly string[] HmacFields =
        {
            "amount_cents",
            "created_at",
            "currency",
            "error_occured",
            "has_parent_transaction",
            "id",
            "integration_id",
            "is_3d_secure",
            "is_auth",
            "is_capture",
            "is_refunded",
            "is_standalone_payment",
            "is_voided",
            "order.id",
            "owner",
            "pending",
            "source_data.pan",
            "source_data.sub_type",
            "source_data.type",
            "success"
        };

        public PaymobWebhookSignatureValidator(
            IOptions<PaymobOptions> options)
        {
            _options = options.Value;
        }

        public bool IsValidTransactionCallback(
            string requestBody,
            string? receivedHmac)
        {
            if (string.IsNullOrWhiteSpace(requestBody) ||
                string.IsNullOrWhiteSpace(receivedHmac) ||
                string.IsNullOrWhiteSpace(_options.HmacSecret))
            {
                return false;
            }

            try
            {
                using var document =
                    JsonDocument.Parse(requestBody);

                var root = document.RootElement;

                if (root.ValueKind != JsonValueKind.Object)
                    return false;

                if (!root.TryGetProperty("type", out var type) ||
                    type.GetString() != "TRANSACTION")
                {
                    return false;
                }

                if (!root.TryGetProperty("obj", out var transaction) ||
                    transaction.ValueKind != JsonValueKind.Object)
                {
                    return false;
                }

                var concatenatedValues = new StringBuilder();

                foreach (var field in HmacFields)
                {
                    if (!TryGetFieldValue(
                            transaction,
                            field,
                            out var value))
                    {
                        return false;
                    }

                    concatenatedValues.Append(value);
                }

                var secretBytes = Encoding.UTF8.GetBytes(
                    _options.HmacSecret);

                var messageBytes = Encoding.UTF8.GetBytes(
                    concatenatedValues.ToString());

                var calculatedHash = HMACSHA512.HashData(
                    secretBytes,
                    messageBytes);

                var receivedHash =
                    Convert.FromHexString(receivedHmac);

                if (receivedHash.Length != calculatedHash.Length)
                    return false;

                return CryptographicOperations.FixedTimeEquals(
                    calculatedHash,
                    receivedHash);
            }
            catch (JsonException)
            {
                return false;
            }
            catch (FormatException)
            {
                return false;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        private static bool TryGetFieldValue(
            JsonElement transaction,
            string fieldPath,
            out string value)
        {
            value = string.Empty;

            var current = transaction;

            foreach (var part in fieldPath.Split('.'))
            {
                if (current.ValueKind != JsonValueKind.Object ||
                    !current.TryGetProperty(part, out current))
                {
                    return false;
                }
            }

            switch (current.ValueKind)
            {
                case JsonValueKind.String:
                    value = current.GetString() ?? string.Empty;
                    return true;

                case JsonValueKind.True:
                    value = "true";
                    return true;

                case JsonValueKind.False:
                    value = "false";
                    return true;

                case JsonValueKind.Number:
                    value = current.GetRawText();
                    return true;

                case JsonValueKind.Null:
                    value = string.Empty;
                    return true;

                default:
                    return false;
            }
        }
    }
}
