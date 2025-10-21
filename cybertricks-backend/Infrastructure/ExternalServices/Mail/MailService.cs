using ct.backend.Common.Ports.Mail;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace ct.backend.Infrastructure.ExternalServices.Mail;

public class MailService : IMailService
{
    private readonly MailSettings _mailSettings;
    private readonly ILogger<MailService> _logger;

    public MailService(IOptions<MailSettings> mailSettings, ILogger<MailService> logger)
    {
        _mailSettings = mailSettings.Value;
        _logger = logger;
    }

    public async Task SendMailAsync(MailContent mailContent)
    {
        var email = new MimeMessage();
        email.Sender = new MailboxAddress(_mailSettings.DisplayName, _mailSettings.Mail);
        email.From.Add(new MailboxAddress(_mailSettings.DisplayName, _mailSettings.Mail));
        email.To.Add(MailboxAddress.Parse(mailContent.To));
        email.Subject = mailContent.Subject;

        var builder = new BodyBuilder();
        builder.HtmlBody = mailContent.Body;
        email.Body = builder.ToMessageBody();

        // use SmtpClient of MailKit
        using var smtp = new MailKit.Net.Smtp.SmtpClient();

        try
        {
            smtp.Connect(_mailSettings.Host, _mailSettings.Port, SecureSocketOptions.StartTls);
            smtp.Authenticate(_mailSettings.Mail, _mailSettings.Password);
            await smtp.SendAsync(email);
            _logger.LogInformation("Send email successfully to " + mailContent.To);
        }
        catch (Exception ex)
        {
            // Send failed, email's content will be saved as mailssave
            Directory.CreateDirectory("mailssave");
            var emailsavefile = string.Format(@"mailssave/{0}.eml", Guid.NewGuid());
            await email.WriteToAsync(emailsavefile);

            _logger.LogInformation("Sending mail error, save as - " + emailsavefile);
            _logger.LogError(ex.Message);
        }

        smtp.Disconnect(true);

        _logger.LogInformation("send mail to " + mailContent.To);
    }
}
