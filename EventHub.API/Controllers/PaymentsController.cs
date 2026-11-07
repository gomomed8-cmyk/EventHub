using EventHub.Application.DTOs.Payments;
using EventHub.Application.Features.Payment.Commands.InitiatePayment;
using EventHub.Application.Features.Payments.Commands.ProcessPaymobWebhook;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventHub.API.Controllers
{
    public class PaymentsController : ApiBaseController
    {
        private readonly IMediator _mediator;

        public PaymentsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [Authorize]
        [HttpPost("/api/bookings/{bookingId:guid}/payments")]
        public async Task<ActionResult<PaymentResponse>> Payment(Guid bookingId,[FromBody] PaymentRequest request,
            [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
            CancellationToken cancellationToken)
        {
            var command = new PaymentCommand(bookingId,request,idempotencyKey);

            var result = await _mediator.Send( command,cancellationToken);

            return ToActionResult(result);
        }

        [AllowAnonymous]
        [HttpPost("/api/payments/paymob/webhook")]
        [Consumes("application/json")]
        public async Task<ActionResult<bool>> PaymobWebhook(CancellationToken cancellationToken)
        {
            using var reader = new StreamReader( Request.Body, System.Text.Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false,
                leaveOpen: true);

            var requestBody = await reader.ReadToEndAsync(cancellationToken);

            var hmac = Request.Query["hmac"].ToString();

            var command = new ProcessPaymobWebhookCommand(requestBody,hmac);

            var result = await _mediator.Send(command,cancellationToken);

            return ToActionResult(result);
        }
    }
}
