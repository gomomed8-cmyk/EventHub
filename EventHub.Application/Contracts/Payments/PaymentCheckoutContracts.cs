namespace EventHub.Application.Contracts.Payments
{
    public sealed record PaymentCheckoutItem(
        string Name,
        string Description,
        decimal UnitPrice,
        int Quantity);

    public sealed record PaymentBillingData(
        string FirstName,
        string LastName,
        string Email,
        string PhoneNumber,
        string? Street = null,
        string? Building = null,
        string? Apartment = null,
        string? Floor = null,
        string? City = null,
        string? State = null,
        string Country = "EG",
        string? PostalCode = null);

    public sealed record PaymentCheckoutRequest(
      Guid PaymentId,
      Guid BookingId,
      decimal Amount,
      string Currency,
      IReadOnlyCollection<PaymentCheckoutItem> Items,
      PaymentBillingData BillingData,
      DateTime ExpiresAt);

    public sealed record PaymentCheckoutResult(
        string IntentionId,
        string? OrderId,
        string CheckoutUrl);
}