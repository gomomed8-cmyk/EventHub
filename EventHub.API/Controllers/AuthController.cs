using EventHub.Application.DTOs.Auth;
using EventHub.Application.Features.Auth.Commands.Login;
using EventHub.Application.Features.Auth.Commands.RefreshToken;
using EventHub.Application.Features.Auth.Commands.Register;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EventHub.API.Controllers
{
    
    public class AuthController : ApiBaseController
    {
        private readonly IMediator _mediator;
        public AuthController(IMediator mediator) 
        { 
            _mediator = mediator;
        }

        [HttpPost("register")]
        public async Task<ActionResult<RegisterResponse>> Register(RegisterCommand command,CancellationToken ct)
        {
            var response =await _mediator.Send(command, ct);
            return ToActionResult(response);

        }
        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login(LoginCommand command ,CancellationToken ct)
        {
           var login = await _mediator.Send(command,ct);
            return ToActionResult( login);
        }
        [HttpPost("refresh")]
        public async Task<ActionResult<LoginResponse>> RefreshToken(RefreshTokenCommand command ,CancellationToken ct)
        {
            var refresh=await _mediator.Send(command,ct);
            return ToActionResult(refresh);
        }
    }
}
