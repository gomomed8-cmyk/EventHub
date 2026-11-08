using EventHub.Application.Common.Errors;
using EventHub.Application.Common.Results;
using EventHub.Application.Contracts;
using EventHub.Application.DTOs.Events;
using EventHub.Application.Specifications.Events;
using EventHub.Domain.Entities;
using EventHub.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace EventHub.Application.Features.Events.Queries.GetAllEvent
{
    public sealed class GetAllEventsQueryHandler : IRequestHandler<GetAllEventsQuery, Result<PagedResult<EventListResponse>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService<PagedResult<EventListResponse>> _cacheService;
        private readonly ILogger<GetAllEventsQueryHandler> _logger;
        private readonly IPublicEventsCacheVersionService _versionService;
        public GetAllEventsQueryHandler(IUnitOfWork unitOfWork, 
            ICacheService<PagedResult<EventListResponse>> cacheService,
            ILogger<GetAllEventsQueryHandler> logger,
            IPublicEventsCacheVersionService versionService)
        {
            _unitOfWork = unitOfWork;
            _cacheService = cacheService;
            _logger = logger;
            _versionService = versionService;
        }

        public async Task<Result<PagedResult<EventListResponse>>> Handle(GetAllEventsQuery request, CancellationToken cancellationToken)
        {
            string? cacheKey = null;

            try
            {
                var cacheVersion = await _versionService.GetOrCreateVersionAsync();

                cacheKey = BuildCacheKey(request, cacheVersion);

                var cachedResponse = await _cacheService.GetCacheAsync(
                    cacheKey,
                    cancellationToken);

                _logger.LogInformation(
                    "Cache lookup: Key={CacheKey}, Hit={CacheHit}",
                    cacheKey,
                    cachedResponse is not null);

                if (cachedResponse is not null)
                {
                    return Result<PagedResult<EventListResponse>>.Ok(cachedResponse);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Redis is unavailable. Falling back to database.");
            }



            var spec = new PublishedEventsSpecification(request);
            var events = await _unitOfWork.GetRepository<Event, Guid>().GetPagedAsync(spec, cancellationToken);

            foreach (var item in events.Items)
            {
                if (item.TicketMode == TicketMode.Categorized &&
           !item.TicketCategories.Any())
                {
                    return Result<PagedResult<EventListResponse>>.Fail(
                        new Error(
                            "Event.InvalidTicketCategories",
                            $"Categorized event with Id {item.Id} has no ticket categories.",
                            ErrorType.Conflict));
                }
            }

            var eventResponses = events.Items.Select(e => new EventListResponse
            {
                EventId = e.Id,
                Name = e.Name,
                Description = e.Description,
                StartDate = e.StartDate,
                EndDate = e.EndDate,
                Category = e.Category,
                TicketMode = e.TicketMode,
                Price = e.TicketMode == TicketMode.GeneralAdmission ? e.Price : null,
                MinTicketPrice = e.TicketMode == TicketMode.Categorized ? e.TicketCategories.Min(tc => tc.Price) : null,
                MaxTicketPrice = e.TicketMode == TicketMode.Categorized ? e.TicketCategories.Max(tc => tc.Price) : null,
                AvailableQuantity = e.TicketMode == TicketMode.GeneralAdmission
                         ? e.AvailableQuantity
                         : e.TicketCategories.Sum(tc => tc.AvailableQuantity)
            }).ToList();
            var pagedResponse = new PagedResult<EventListResponse>(
                    eventResponses,
                    events.PageNumber,
                    events.PageSize,
                    events.TotalCount,
                    events.TotalPages);

            if (cacheKey is not null)
            {
                try
                {
                    await _cacheService.AddCacheAsync(
                        cacheKey,
                        pagedResponse,
                        TimeSpan.FromMinutes(5),
                        cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Failed to write public events to cache.");
                }
            }

            return Result<PagedResult<EventListResponse>>.Ok(pagedResponse);
        }

        private static string BuildCacheKey(GetAllEventsQuery request, string cacheVersion)
        {
            var searchTerm= request.SearchTerm?.Trim().ToLowerInvariant() ?? "";

            var category=request.Category?.ToString() ?? "";

            var startDateFrom=request.StartDateFrom?.ToString(
                "O", CultureInfo.InvariantCulture) ?? "";

            var startDateTo= request.StartDateTo?.ToString(
                "O", CultureInfo.InvariantCulture) ?? "";
            var sortBy = string.IsNullOrWhiteSpace(request.SortBy)
                        ? "startdate"
                        : request.SortBy.Trim().ToLowerInvariant();

            return $"Event:Public:search:{searchTerm}" +
                $":category:{category}" +
                $":from:{startDateFrom}" +
                $":to:{startDateTo}" +
                $":page:{request.PageNumber ?? 1}" +
                $":size:{request.PageSize ?? 10}" +
                $":sort:{sortBy}" +
                $":desc:{request.SortDescending}"+
                $":cacheVersion:{cacheVersion}";

        }
    }
}
