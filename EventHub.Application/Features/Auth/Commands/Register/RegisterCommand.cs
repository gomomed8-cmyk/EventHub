using EventHub.Application.DTOs.Auth;
using MediatR;


namespace EventHub.Application.Features.Auth.Commands.Register
{
    public sealed record RegisterCommand(RegisterRequest Request) : IRequest<Result<RegisterResponse>>;
    
}
