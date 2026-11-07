namespace EventHub.Application.Contracts.Payments
{
    public interface IPaymentGateway
    {
        Task<PaymentCheckoutResult> CreateCheckoutAsync(
            PaymentCheckoutRequest request,
            CancellationToken cancellationToken = default);
    }
}