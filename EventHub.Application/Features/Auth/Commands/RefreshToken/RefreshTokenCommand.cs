using EventHub.Application.DTOs.Auth;
using MediatR;


namespace EventHub.Application.Features.Auth.Commands.RefreshToken
{
    public sealed record RefreshTokenCommand(string RefreshToken):IRequest<Result<LoginResponse>>
    {
    }
}
