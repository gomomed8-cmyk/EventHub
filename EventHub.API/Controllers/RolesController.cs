using EventHub.Application.Features.Roles.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventHub.API.Controllers
{
    
    public class RolesController : ApiBaseController
    {
        private readonly IMediator _mediator;

        public RolesController(IMediator mediator) 
        {
            _mediator = mediator;
        }
        [Authorize(Roles = "Admin")]
        [HttpPost("promote-to-organizer")]
        public async Task<Result<ActionResult>> PromoteToOrganizer(PromoteToOrganizerCommand command, CancellationToken ct)
        {
            var result=await _mediator.Send(command, ct);

            return ToActionResult(result);
        }
    }
}
