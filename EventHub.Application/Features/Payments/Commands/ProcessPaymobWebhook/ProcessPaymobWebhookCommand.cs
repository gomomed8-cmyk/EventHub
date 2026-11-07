

using MediatR;

namespace EventHub.Application.Features.Payments.Commands.ProcessPaymobWebhook
{
    public sealed record ProcessPaymobWebhookCommand(string RequestBody, string? Hmac) : IRequest<Result<bool>>
    {

    }
}
