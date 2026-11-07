using EventHub.Application.DTOs.Auth;
using MediatR;


namespace EventHub.Application.Features.Auth.Commands.Login
{
    public sealed record LoginCommand(LoginRequest Request): IRequest<Result<LoginResponse>>
    {
      
    }
}
