using EventHub.Application.Common.Errors;
using EventHub.Application.Contracts;
using EventHub.Application.Contracts.Payments;
using EventHub.Application.DTOs.Payments;
using EventHub.Application.Specifications.Payments;
using EventHub.Domain.Entities;
using EventHub.Domain.Enums;
using MediatR;
using System.ComponentModel.DataAnnotations;
using PaymentEntity = EventHub.Domain.Entities.Payment;

namespace EventHub.Application.Features.Payment.Commands.InitiatePayment
{
    public sealed class PaymentCommandHandler
        : IRequestHandler<
            PaymentCommand,
            Result<PaymentResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IPaymentGateway _paymentGateway;

        public PaymentCommandHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IPaymentGateway paymentGateway)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _paymentGateway = paymentGateway;
        }

        public async Task<Result<PaymentResponse>> Handle(
            PaymentCommand request,
            CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;

            if (userId == Guid.Empty)
                return UserErrors.Unauthorized;

            var idempotencyKey = request.IdempotencyKey?.Trim();

            if (string.IsNullOrWhiteSpace(idempotencyKey) ||
                idempotencyKey.Length > 100)
            {
                return Result<PaymentResponse>.Fail(
                    new Error(
                        "Payment.InvalidIdempotencyKey",
                        "A valid Idempotency-Key is required; maximum length is 100 characters.",
                        ErrorType.Validation));
            }

            if (request.BookingId == Guid.Empty ||
                request.Request is null)
            {
                return Result<PaymentResponse>.Fail(
                    new Error(
                        "Payment.InvalidRequest",
                        "A valid booking ID and billing request are required.",
                        ErrorType.Validation));
            }

            var billing = request.Request;

            if (string.IsNullOrWhiteSpace(billing.FirstName) ||
                string.IsNullOrWhiteSpace(billing.LastName) ||
                string.IsNullOrWhiteSpace(billing.Email) ||
                !new EmailAddressAttribute().IsValid(billing.Email) ||
                string.IsNullOrWhiteSpace(billing.PhoneNumber))
            {
                return Result<PaymentResponse>.Fail(
                    new Error(
                        "Payment.InvalidBillingData",
                        "Valid name, email and phone number are required.",
                        ErrorType.Validation));
            }

            var now = DateTime.UtcNow;

            var bookingSpecification =
                new GetBookingForPaymentSpecification(
                    request.BookingId,
                    userId);

            var booking = await _unitOfWork
                .GetRepository<Booking, Guid>()
                .GetByIdAsync(
                    bookingSpecification,
                    cancellationToken);

            if (booking is null)
            {
                return Result<PaymentResponse>.Fail(
                    new Error(
                        "Booking.NotFound",
                        "The booking was not found.",
                        ErrorType.NotFound));
            }

            var paymentRepository =
                _unitOfWork.GetRepository<PaymentEntity, Guid>();

            var existingPaymentSpecification =
                new GetPaymentByIdempotencyKeySpecification(
                    booking.Id,
                    idempotencyKey);

            var existingPayment = await paymentRepository.GetByIdAsync(
                existingPaymentSpecification,
                cancellationToken);

            if (existingPayment is not null)
            {
                if (existingPayment.Status == PaymentStatus.Succeeded &&
                    !string.IsNullOrWhiteSpace(existingPayment.CheckoutUrl))
                {
                    return Result<PaymentResponse>.Ok(
                        MapResponse(existingPayment, booking));
                }

                if (existingPayment.Status == PaymentStatus.Pending &&
                    !string.IsNullOrWhiteSpace(existingPayment.CheckoutUrl) &&
                    booking.Status == BookingStatus.Pending &&
                    booking.ExpiresAt.HasValue &&
                    booking.ExpiresAt.Value > now)
                {
                    return Result<PaymentResponse>.Ok(
                        MapResponse(existingPayment, booking));
                }

                if (existingPayment.Status == PaymentStatus.Failed)
                {
                    return Result<PaymentResponse>.Fail(
                        new Error(
                            "Payment.PreviousAttemptFailed",
                            "This idempotency key belongs to a failed payment attempt. Use a new key for a new attempt.",
                            ErrorType.Conflict));
                }

                return Result<PaymentResponse>.Fail(
                    new Error(
                        "Payment.AttemptUnresolved",
                        "A previous attempt exists without a usable checkout URL. Reconcile its status before starting another attempt.",
                        ErrorType.Conflict));
            }

            if (booking.Status != BookingStatus.Pending)
            {
                return Result<PaymentResponse>.Fail(
                    new Error(
                        "Booking.InvalidStatus",
                        "Only pending bookings can begin payment.",
                        ErrorType.Conflict));
            }

            if (!booking.ExpiresAt.HasValue ||
                booking.ExpiresAt.Value <= now.AddSeconds(30))
            {
                return Result<PaymentResponse>.Fail(
                    new Error(
                        "Booking.Expired",
                        "The booking has expired or is too close to expiry. Create a new booking.",
                        ErrorType.Conflict));
            }

            if (booking.Event is null ||
                booking.Event.Status != EventStatus.Published ||
                booking.Event.StartDate <= now ||
                booking.Event.EndDate <= now)
            {
                return Result<PaymentResponse>.Fail(
                    new Error(
                        "Event.BookingClosed",
                        "Payment is unavailable because the event is not open for booking.",
                        ErrorType.Conflict));
            }

            if (booking.TotalAmount <= 0)
            {
                return Result<PaymentResponse>.Fail(
                    new Error(
                        "Payment.InvalidAmount",
                        "The booking amount must be greater than zero.",
                        ErrorType.Validation));
            }

            var checkoutItems = new List<PaymentCheckoutItem>();

            foreach (var item in booking.BookingItems)
            {
                var itemName = "General Admission";

                if (item.TicketCategoryId.HasValue)
                {
                    var category = booking.Event.TicketCategories
                        .FirstOrDefault(c =>
                            c.Id == item.TicketCategoryId.Value);

                    if (category is null)
                    {
                        return Result<PaymentResponse>.Fail(
                            new Error(
                                "Payment.InvalidTicketCategory",
                                "A booking item references a missing ticket category.",
                                ErrorType.Conflict));
                    }

                    itemName = category.Name;
                }

                checkoutItems.Add(
                    new PaymentCheckoutItem(
                        itemName,
                        $"Tickets for {booking.Event.Name}",
                        item.UnitPrice,
                        item.Quantity));
            }

            if (checkoutItems.Count == 0)
            {
                return Result<PaymentResponse>.Fail(
                    new Error(
                        "Payment.EmptyBooking",
                        "The booking has no items to pay for.",
                        ErrorType.Conflict));
            }

            var payment = new PaymentEntity
            {
                Id = Guid.NewGuid(),
                BookingId = booking.Id,
                Amount = booking.TotalAmount,
                Currency = "EGP",
                Status = PaymentStatus.Pending,
                CreatedAt = now,
                UpdatedAt = now,
                IdempotencyKey = idempotencyKey,
                PaymentMethod = null
            };

            paymentRepository.Add(payment);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var billingData = new PaymentBillingData(
                FirstName: billing.FirstName.Trim(),
                LastName: billing.LastName.Trim(),
                Email: billing.Email.Trim(),
                PhoneNumber: billing.PhoneNumber.Trim(),
                Street: billing.Street,
                Building: billing.Building,
                Apartment: billing.Apartment,
                Floor: billing.Floor,
                City: billing.City,
                State: billing.State,
                Country: string.IsNullOrWhiteSpace(billing.Country)
                    ? "EG"
                    : billing.Country.Trim().ToUpperInvariant(),
                PostalCode: billing.PostalCode);

            var gatewayRequest = new PaymentCheckoutRequest(
                PaymentId: payment.Id,
                BookingId: booking.Id,
                Amount: booking.TotalAmount,
                Currency: payment.Currency,
                Items: checkoutItems,
                BillingData: billingData,
                ExpiresAt: booking.ExpiresAt.Value);

            PaymentCheckoutResult checkout;

            try
            {
                checkout = await _paymentGateway.CreateCheckoutAsync(
                    gatewayRequest,
                    cancellationToken);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                payment.UpdatedAt = DateTime.UtcNow;
                payment.FailureReason =
                    "The request was cancelled; the provider outcome may be unknown.";

                await _unitOfWork.SaveChangesAsync(
                    CancellationToken.None);

                throw;
            }
            catch (HttpRequestException ex)
            {
                var statusCode = ex.StatusCode.HasValue
                    ? (int)ex.StatusCode.Value
                    : (int?)null;

                payment.UpdatedAt = DateTime.UtcNow;

                if (statusCode.HasValue &&
                    statusCode.Value >= 400 &&
                    statusCode.Value < 500)
                {
                    payment.Status = PaymentStatus.Failed;

                    payment.FailureReason =
                        $"Paymob rejected the request (HTTP {statusCode.Value}).";

                    await _unitOfWork.SaveChangesAsync(
                        CancellationToken.None);

                    return Result<PaymentResponse>.Fail(
                        new Error(
                            "Payment.ProviderRejected",
                            $"Paymob rejected the checkout request (HTTP {statusCode.Value}). Check the integration ID and server logs.",
                            ErrorType.Conflict));
                }

                payment.FailureReason =
                    "Paymob outcome is unknown. Reconcile with the provider before retrying.";

                await _unitOfWork.SaveChangesAsync(
                    CancellationToken.None);

                throw;
            }
            catch
            {
                payment.UpdatedAt = DateTime.UtcNow;
                payment.FailureReason =
                    "The provider outcome could not be confirmed. Reconcile before retrying.";

                await _unitOfWork.SaveChangesAsync(
                    CancellationToken.None);

                throw;
            }

            payment.PaymobIntentionId = checkout.IntentionId;
            payment.PaymobOrderId = checkout.OrderId;
            payment.CheckoutUrl = checkout.CheckoutUrl;
            payment.UpdatedAt = DateTime.UtcNow;
            payment.FailureReason = null;

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<PaymentResponse>.Ok(
                MapResponse(payment, booking));
        }

        private static PaymentResponse MapResponse(
            PaymentEntity payment,
            Booking booking)
        {
            return new PaymentResponse(
                payment.Id,
                booking.Id,
                payment.Status,
                payment.CheckoutUrl!,
                booking.ExpiresAt);
        }
    }
}
