using EventHub.Application.Contracts;
using EventHub.Infrastructure.Options;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace EventHub.Infrastructure.Services.Email
{
    public sealed class EmailService : IEmailService
    {
        private readonly EmailOptions _emailOptions;

        public EmailService(IOptions<EmailOptions> emailOptions)
        {
            _emailOptions = emailOptions.Value;
        }

        public async Task SendEmailAsync(string to,string subject,string htmlBody,CancellationToken cancellationToken = default)
        {
            var message = new MimeMessage();

            message.From.Add(
                new MailboxAddress(
                    _emailOptions.FromName,
                    _emailOptions.FromEmail));

            message.To.Add(MailboxAddress.Parse(to));

            message.Subject = subject;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = htmlBody
            };

            message.Body = bodyBuilder.ToMessageBody();

            using var smtpClient = new SmtpClient();

            try
            {
                await smtpClient.ConnectAsync(
                    _emailOptions.Host,
                    _emailOptions.Port,
                    SecureSocketOptions.StartTls,
                    cancellationToken);

                await smtpClient.AuthenticateAsync(
                    _emailOptions.Username,
                    _emailOptions.Password,
                    cancellationToken);

                await smtpClient.SendAsync(
                    message,
                    cancellationToken);
            }
            finally
            {
                if (smtpClient.IsConnected)
                {
                    await smtpClient.DisconnectAsync(
                        true,
                        CancellationToken.None);
                }
            }
        }

        public async Task SendEmailWithAttachmentAsync(
     string to,
     string subject,
     string htmlBody,
     byte[] attachment,
     string attachmentFileName,
     CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(to);
            ArgumentException.ThrowIfNullOrWhiteSpace(subject);
            ArgumentNullException.ThrowIfNull(htmlBody);
            ArgumentNullException.ThrowIfNull(attachment);
            ArgumentException.ThrowIfNullOrWhiteSpace(attachmentFileName);

            if (attachment.Length == 0)
                throw new ArgumentException(
                    "Attachment cannot be empty.",
                    nameof(attachment));

            var fileName = Path.GetFileName(attachmentFileName);

            var message = new MimeMessage();

            message.From.Add(
                new MailboxAddress(
                    _emailOptions.FromName,
                    _emailOptions.FromEmail));

            message.To.Add(MailboxAddress.Parse(to));

            message.Subject = subject;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = htmlBody
            };

            bodyBuilder.Attachments.Add(
                fileName,
                attachment,
                new ContentType("application", "pdf"));

            message.Body = bodyBuilder.ToMessageBody();

            using var smtpClient = new SmtpClient();

            try
            {
                await smtpClient.ConnectAsync(
                    _emailOptions.Host,
                    _emailOptions.Port,
                    SecureSocketOptions.StartTls,
                    cancellationToken);

                await smtpClient.AuthenticateAsync(
                    _emailOptions.Username,
                    _emailOptions.Password,
                    cancellationToken);

                await smtpClient.SendAsync(
                    message,
                    cancellationToken);
            }
            finally
            {
                if (smtpClient.IsConnected)
                {
                    await smtpClient.DisconnectAsync(
                        true,
                        CancellationToken.None);
                }
            }
        }
    }
}
