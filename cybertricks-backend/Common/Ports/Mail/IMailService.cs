namespace ct.backend.Common.Ports.Mail
{
    public interface IMailService
    {
        Task SendMailAsync(MailContent mailContent);
    }
}
