using EventHub.Application.Contracts;
using EventHub.Infrastructure.Identity.ContextIdentity;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Infrastructure.Services
{
    internal sealed class CustomerInfoLookup : ICustomerInfoLookup
    {
        private readonly EventHubIdentityDbContext _context;

        public CustomerInfoLookup(EventHubIdentityDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyDictionary<Guid, CustomerInfo>>GetByUserIdsAsync(IReadOnlyCollection<Guid> userIds,
                CancellationToken cancellationToken = default)
        {
            var ids = userIds.Distinct().ToArray();

            if (ids.Length == 0)
                return new Dictionary<Guid, CustomerInfo>();

            var users = await _context.Users
                .AsNoTracking()
                .Where(user => ids.Contains(user.Id))
                .Select(user => new
                {
                    user.Id,
                    user.DisplayName,
                    user.Email
                })
                .ToListAsync(cancellationToken);

            return users.ToDictionary(
                user => user.Id,
                user => new CustomerInfo(
                    user.Id,
                    user.DisplayName,
                    user.Email));
        }
    }
}