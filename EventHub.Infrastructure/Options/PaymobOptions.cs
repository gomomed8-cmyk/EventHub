namespace EventHub.Infrastructure.Options
{
    public sealed class PaymobOptions
    {
        public const string SectionName = "Paymob";

        public string BaseUrl { get; set; } =
            "https://accept.paymob.com";

        public string SecretKey { get; set; } = string.Empty;

        public string PublicKey { get; set; } = string.Empty;

        public string ApiKey { get; set; } = string.Empty;

        public string HmacSecret { get; set; } = string.Empty;

        public int IntegrationId { get; set; }
        public string CheckoutBaseUrl { get; set; } = "https://eg.checkout.paymob.com/";
        public string? NotificationUrl { get; set; }

        public string? RedirectionUrl { get; set; }

        public string Currency { get; set; } = "EGP";
    }
}