using EventHub.Application.Common.Errors;
using EventHub.Application.Common.Results;
using EventHub.Application.Contracts;
using MediatR;

namespace EventHub.Application.Features.Roles.Commands
{
    public sealed class PromoteToOrganizerCommandHandler
        : IRequestHandler<PromoteToOrganizerCommand, Result>
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly IIdentityService _identityService;

        public PromoteToOrganizerCommandHandler(
            ICurrentUserService currentUserService,
            IIdentityService identityService)
        {
            _currentUserService = currentUserService;
            _identityService = identityService;
        }

        public async Task<Result> Handle(
            PromoteToOrganizerCommand request,
            CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.UserId;

            if (currentUserId == Guid.Empty)
            {
                return Result.Fail(
                    new Error("User.NotAuthenticated", "Current user was not found.",ErrorType.Unauthorized));
            }

            var currentUser = await _identityService.GetUserByIdAsync(
                currentUserId,
                cancellationToken);

            if (currentUser.IsFailure)
            {
                return Result.Fail(currentUser.Errors);
            }

            if (currentUser.Value.Role != "Admin")
            {
                return Result.Fail(
                    new Error(
                        "Role.Forbidden",
                        "Only admins can promote users to organizers.",ErrorType.Forbidden));
            }

            var targetUser = await _identityService.GetUserByIdAsync(
                request.UserId,
                cancellationToken);

            if (targetUser.IsFailure)
            {
                return Result.Fail(targetUser.Errors);
            }
             

            var result = await _identityService.PromoteToOrganizerAsync(
                targetUser.Value.UserId,
                cancellationToken);

            if (result.IsFailure)
            {
                return Result.Fail(result.Errors);
            }

            return Result.Ok();
        }
    }
}