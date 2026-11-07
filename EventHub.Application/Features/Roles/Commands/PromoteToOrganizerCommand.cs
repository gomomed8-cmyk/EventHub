using EventHub.Application.Common.Results;
using MediatR;


namespace EventHub.Application.Features.Roles.Commands
{
    public sealed record PromoteToOrganizerCommand(Guid UserId):IRequest<Result>
    {

    }
}
