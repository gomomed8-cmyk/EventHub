using EventHub.Application.Contracts;
using EventHub.Application.DTOs.Email;
using Microsoft.AspNetCore.Mvc;

namespace EventHub.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmailTestController : ControllerBase
    {
        private readonly IEmailService _emailService;
        private readonly IWebHostEnvironment _environment;
        private readonly IEmailTemplateRenderer _templateRenderer;
        public EmailTestController(
     IEmailService emailService,
     IEmailTemplateRenderer templateRenderer,
     IWebHostEnvironment environment)
        {
            _emailService = emailService;
            _templateRenderer = templateRenderer;
            _environment = environment;
        }

        [HttpPost]
        public async Task<IActionResult> SendTestEmail([FromBody] EmailTestRequest request,
            CancellationToken cancellationToken)
        {
            if (!_environment.IsDevelopment())
                return NotFound();

            if (string.IsNullOrWhiteSpace(request.To))
                return BadRequest("Recipient email is required.");

            const string subject = "EventHub - SMTP Test";

            const string htmlBody = """
                <!DOCTYPE html>
                <html>
                <body>
                    <h2>Hello from EventHub!</h2>
                    <p>Your email configuration is working successfully.</p>
                    <p>This is a test email sent using Gmail SMTP and MailKit.</p>
                    <strong>EventHub Team</strong>
                </body>
                </html>
                """;

            await _emailService.SendEmailAsync(request.To,subject,htmlBody,cancellationToken);

            return Ok(new
            {
                Message = "Test email sent successfully."
            });
        }
        [HttpPost("render")]
        public async Task<IActionResult> RenderBookingEmail(
    [FromBody] BookingConfirmationEmailModel model,
    CancellationToken cancellationToken)
        {
            if (!_environment.IsDevelopment())
                return NotFound();

            var html = await _templateRenderer.RenderBookingConfirmationAsync(
                model,
                cancellationToken);

            return Content(html, "text/html");
        }
    }

    public sealed record EmailTestRequest(string To);
}
