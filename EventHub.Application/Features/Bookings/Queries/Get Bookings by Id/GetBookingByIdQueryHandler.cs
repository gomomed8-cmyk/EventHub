using EventHub.Application.Common.Errors;
using EventHub.Application.Contracts;
using EventHub.Application.DTOs.Booking;
using EventHub.Application.Specifications.Bookings;
using EventHub.Domain.Entities;
using MediatR;


namespace EventHub.Application.Features.Bookings.Queries.Get_Bookings_by_Id
{
    public sealed class GetBookingByIdQueryHandler : IRequestHandler<GetBookingByIdQuery, Result<BookingDetailsResponse>>
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly IUnitOfWork _unitOfWork;
        public GetBookingByIdQueryHandler(ICurrentUserService currentUserService,IUnitOfWork unitOfWork) 
        { 
          _currentUserService = currentUserService;
          _unitOfWork = unitOfWork;
        }
        public async Task<Result<BookingDetailsResponse>> Handle(GetBookingByIdQuery request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            if (userId == Guid.Empty)
            {
                return UserErrors.Unauthorized;
            }
            var spec= new GetBookingByIdSpecification(request.BookingId,userId);
            var book=await _unitOfWork.GetRepository<Booking,Guid>()
                .GetByIdAsync(spec,cancellationToken);
            if (book == null)
            {
                return Result<BookingDetailsResponse>.Fail(new Error("Booking Id Not Found",
                    "The booking does not belong to this user.",
                    ErrorType.NotFound));
            }
            var response = new BookingDetailsResponse
            {
                BookingId = request.BookingId,
                EventName = book.Event.Name,
                EventEndDate = book.Event.EndDate,
                EventStartDate = book.Event.StartDate,
                CreatedAt = book.CreatedAt,
                ExpiresAt = book.ExpiresAt,
                EventId = book.EventId,
                Status = book.Status,
                TotalAmount = book.TotalAmount,
                Items = book.BookingItems
               .Select(item => new CreateBookingItemResponse
               {
                   TicketCategoryId = item.TicketCategoryId,
                   CategoryName = item.TicketCategory != null
        ? item.TicketCategory.Name
        : "General Admission",
                   Quantity = item.Quantity,
                   UnitPrice = item.UnitPrice,
                   TotalPrice = item.TotalPrice
               }).ToList()

            };
            return Result<BookingDetailsResponse>.Ok(response);
        }
    }
}
