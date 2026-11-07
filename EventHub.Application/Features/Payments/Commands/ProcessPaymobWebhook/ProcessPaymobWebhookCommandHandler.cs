using EventHub.Application.Common.Errors;
using EventHub.Application.Contracts;
using EventHub.Application.Contracts.Payments;
using EventHub.Application.Features.Payments.Commands.ProcessPaymobWebhook;
using EventHub.Application.Specifications.Bookings;
using EventHub.Application.Specifications.Payments;
using EventHub.Domain.Entities;
using EventHub.Domain.Enums;
using PaymentEntity = EventHub.Domain.Entities.Payment;
using MediatR;
using System.Globalization;
using System.Text.Json;

namespace EventHub.Application.Features.Payment.Commands.ProcessPaymobWebhook
{
    public sealed class ProcessPaymobWebhookCommandHandler:IRequestHandler<ProcessPaymobWebhookCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPaymobWebhookSignatureValidator _signatureValidator;
        private readonly IBookingConfirmationEmailScheduler _emailScheduler;

        public ProcessPaymobWebhookCommandHandler(IUnitOfWork unitOfWork,IPaymobWebhookSignatureValidator signatureValidator,
            IBookingConfirmationEmailScheduler emailScheduler)
        {
            _unitOfWork = unitOfWork;
            _signatureValidator = signatureValidator;
            _emailScheduler = emailScheduler;
        }

        public async Task<Result<bool>> Handle(ProcessPaymobWebhookCommand request,CancellationToken cancellationToken)
        {
            if (!_signatureValidator.IsValidTransactionCallback(
                    request.RequestBody,
                    request.Hmac))
            {
                return Fail(
                    "Payment.InvalidWebhookSignature",
                    "Invalid Paymob webhook signature.",
                    ErrorType.Validation);
            }

            JsonDocument document;

            try
            {
                document = JsonDocument.Parse(request.RequestBody);
            }
            catch (JsonException)
            {
                return Fail(
                    "Payment.InvalidWebhookPayload",
                    "Invalid JSON webhook payload.",
                    ErrorType.Validation);
            }

            using (document)
            {
                var root = document.RootElement;

                if (!root.TryGetProperty("obj", out var transaction) ||
                    transaction.ValueKind != JsonValueKind.Object)
                {
                    return Fail(
                        "Payment.InvalidWebhookPayload",
                        "Paymob transaction object is missing.",
                        ErrorType.Validation);
                }

                if (!TryGetScalar(transaction, "id", out var transactionId) ||
                    !TryGetNestedScalar(
                        transaction, "order", "id", out var orderId) ||
                    !TryGetScalar(
                        transaction, "amount_cents", out var amountText) ||
                    !TryGetScalar(
                        transaction, "currency", out var currency))
                {
                    return Fail(
                        "Payment.InvalidWebhookPayload",
                        "Required Paymob transaction fields are missing.",
                        ErrorType.Validation);
                }

                if (string.IsNullOrWhiteSpace(transactionId) ||
                    string.IsNullOrWhiteSpace(orderId) ||
                    string.IsNullOrWhiteSpace(currency))
                {
                    return Fail(
                        "Payment.InvalidWebhookPayload",
                        "Transaction ID, order ID and currency are required.",
                        ErrorType.Validation);
                }

                if (!long.TryParse(
                        amountText,
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out var amountCents) ||
                    amountCents < 0)
                {
                    return Fail(
                        "Payment.InvalidAmount",
                        "Invalid transaction amount.",
                        ErrorType.Validation);
                }

                if (!TryGetBoolean(transaction, "success", out var success) ||
                    !TryGetBoolean(transaction, "pending", out var pending) ||
                    !TryGetBoolean(transaction, "error_occured", out var errorOccurred) ||
                    !TryGetBoolean(transaction, "is_voided", out var isVoided) ||
                    !TryGetBoolean(transaction, "is_refunded", out var isRefunded))
                {
                    return Fail(
                        "Payment.InvalidWebhookPayload",
                        "Required Paymob transaction flags are missing.",
                        ErrorType.Validation);
                }

                if (pending)
                    return Result<bool>.Ok(true);

                var paymentRepository =
                    _unitOfWork.GetRepository<PaymentEntity, Guid>();

                var payment = await paymentRepository.GetByIdAsync(
                    new GetPaymentByPaymobOrderIdSpecification(orderId),
                    cancellationToken);

                if (payment is null)
                {
                    return Fail(
                        "Payment.NotFound",
                        "No payment matches this Paymob order.",
                        ErrorType.NotFound);
                }


                if (payment.Status == PaymentStatus.Succeeded ||payment.Status == PaymentStatus.Failed)
                {
                    if (payment.PaymobTransactionId != transactionId)
                    {
                        return Fail(
                            "Payment.TransactionMismatch",
                            "This payment was already processed with another transaction.",
                            ErrorType.Conflict);
                    }

                    if (payment.Status == PaymentStatus.Succeeded)
                    {
                        var duplicateBookingRepository = _unitOfWork.GetRepository<Booking, Guid>();

                        var duplicateBooking = await duplicateBookingRepository.GetByIdAsync(
                            new GetBookingForConfirmationEmailSpecification(payment.BookingId),
                            cancellationToken);

                        if (duplicateBooking is not null &&
                            duplicateBooking.Status == BookingStatus.Confirmed &&
                            !duplicateBooking.TicketsEmailSentAt.HasValue)
                        {
                            _emailScheduler.Schedule(duplicateBooking.Id);
                        }
                    }

                    return Result<bool>.Ok(true);
                }

                if (payment.Status != PaymentStatus.Pending)
                {
                    return Fail(
                        "Payment.InvalidStatus",
                        "This payment is not waiting for confirmation.",
                        ErrorType.Conflict);
                }

                if (payment.Amount * 100m != amountCents ||
                    !string.Equals(
                        payment.Currency,
                        currency,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return Fail(
                        "Payment.AmountOrCurrencyMismatch",
                        "The Paymob amount or currency does not match the payment.",
                        ErrorType.Conflict);
                }

                var now = DateTime.UtcNow;

                if (!success || errorOccurred || isVoided || isRefunded)
                {
                    payment.Status = PaymentStatus.Failed;
                    payment.PaymobTransactionId = transactionId;
                    payment.FailureReason =
                        "Paymob reported that the transaction did not complete successfully.";
                    payment.UpdatedAt = now;

                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                    return Result<bool>.Ok(true);
                }

                var bookingRepository =
                    _unitOfWork.GetRepository<Booking, Guid>();

                var booking = await bookingRepository.GetByIdAsync(
                    payment.BookingId,
                    cancellationToken);

                if (booking is null)
                {
                    return Fail(
                        "Booking.NotFound",
                        "The booking associated with this payment was not found.",
                        ErrorType.NotFound);
                }

                if (booking.Status != BookingStatus.Pending ||
                    !booking.ExpiresAt.HasValue ||
                    booking.ExpiresAt.Value <= now)
                {
                    payment.Status = PaymentStatus.Succeeded;
                    payment.PaymobTransactionId = transactionId;
                    payment.UpdatedAt = now;
                    payment.FailureReason =
                        "Payment succeeded after booking expiration or status change. Refund review is required.";

                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                    return Result<bool>.Ok(true);
                }

                var bookingItemRepository =
                    _unitOfWork.GetRepository<BookingItem, Guid>();

                var bookingItems = await bookingItemRepository.GetAllAsync(
                    new GetBookingItemsByBookingIdSpecification(booking.Id),
                    cancellationToken);

                if (bookingItems.Count == 0 ||
                    bookingItems.Any(item => item.Quantity <= 0))
                {
                    payment.Status = PaymentStatus.Succeeded;
                    payment.PaymobTransactionId = transactionId;
                    payment.UpdatedAt = now;
                    payment.FailureReason =
                        "Payment succeeded but booking items are missing or invalid. Manual review is required.";

                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                    return Result<bool>.Ok(true);
                }

                var ticketRepository = _unitOfWork.GetRepository<Ticket, Guid>();

                var ticketNumber = 0;

                foreach (var item in bookingItems)
                {
                    for (var i = 0; i < item.Quantity; i++)
                    {
                        var ticket = new Ticket
                        {
                            Id = Guid.NewGuid(),
                            BookingItemId = item.Id,
                            TicketCode = Guid.NewGuid()
                                .ToString("N")
                                .ToUpperInvariant(),
                            TicketNumber = ++ticketNumber,
                            Status = TicketStatus.Issued,
                            IssuedAt = now,
                            UsedAt = null
                        };

                        ticketRepository.Add(ticket);
                    }
                }

                payment.Status = PaymentStatus.Succeeded;
                payment.PaymobTransactionId = transactionId;
                payment.UpdatedAt = now;
                payment.FailureReason = null;

                booking.Status = BookingStatus.Confirmed;

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _emailScheduler.Schedule(booking.Id);

                return Result<bool>.Ok(true);
            }
        }

        private static Result<bool> Fail(
            string code,
            string description,
            ErrorType type)
        {
            return Result<bool>.Fail(
                new Error(code, description, type));
        }

        private static bool TryGetScalar(
            JsonElement element,
            string propertyName,
            out string value)
        {
            value = string.Empty;

            if (element.ValueKind != JsonValueKind.Object ||
                !element.TryGetProperty(propertyName, out var property))
            {
                return false;
            }

            switch (property.ValueKind)
            {
                case JsonValueKind.String:
                    value = property.GetString() ?? string.Empty;
                    return true;

                case JsonValueKind.Number:
                    value = property.GetRawText();
                    return true;

                default:
                    return false;
            }
        }

        private static bool TryGetNestedScalar(
            JsonElement element,
            string parentName,
            string propertyName,
            out string value)
        {
            value = string.Empty;

            if (element.ValueKind != JsonValueKind.Object ||
                !element.TryGetProperty(parentName, out var parent) ||
                parent.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            return TryGetScalar(parent, propertyName, out value);
        }

        private static bool TryGetBoolean(
            JsonElement element,
            string propertyName,
            out bool value)
        {
            value = false;

            if (element.ValueKind != JsonValueKind.Object ||
                !element.TryGetProperty(propertyName, out var property) ||
                (property.ValueKind != JsonValueKind.True &&
                 property.ValueKind != JsonValueKind.False))
            {
                return false;
            }

            value = property.GetBoolean();
            return true;
        }
    }
}
