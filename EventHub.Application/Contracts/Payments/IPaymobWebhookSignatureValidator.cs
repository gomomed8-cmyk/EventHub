
namespace EventHub.Application.Contracts.Payments
{
    public interface IPaymobWebhookSignatureValidator
    {
        bool IsValidTransactionCallback( string requestBody,string? receivedHmac);
    }
}
