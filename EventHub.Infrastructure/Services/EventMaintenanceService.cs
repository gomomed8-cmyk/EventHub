using EventHub.Application.Contracts;
using EventHub.Application.Specifications.Events;
using EventHub.Domain.Entities;
using EventHub.Domain.Enums;

namespace EventHub.Infrastructure.Services
{
    internal class EventMaintenanceService : IEventMaintenanceService
    {
        private readonly IUnitOfWork _unitOfWork;
        public EventMaintenanceService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public async Task CompleteExpiredEventsAsync(CancellationToken ct = default)
        {
            var spec = new ExpiredEventsSpecification();

            var events= await _unitOfWork.GetRepository<Event,Guid>().GetAllAsync(spec,ct);
            if(events.Any())
            {
                foreach(var ev in events)
                {
                    ev.Status = EventStatus.Completed;
                    _unitOfWork.GetRepository<Event,Guid>().Update(ev);
                }
                await _unitOfWork.SaveChangesAsync(ct);
            }

        }
    }
}
