
namespace EventHub.Application.Contracts
{
    public interface IEmailService
    {
        Task SendEmailAsync(string to,string subject,string htmlBody,
            CancellationToken cancellationToken = default);

        Task SendEmailWithAttachmentAsync(string to,string subject,string htmlBody,byte[] attachment,string attachmentFileName,
                CancellationToken cancellationToken = default);
    }
}
