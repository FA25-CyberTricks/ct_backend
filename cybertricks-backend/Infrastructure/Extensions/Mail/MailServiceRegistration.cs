using ct.backend.Common.Ports.Mail;
using ct.backend.Infrastructure.ExternalServices.Mail;

namespace ct.backend.Infrastructure.Extension.Mail;


public static class MailServiceRegistration
{
    public static IServiceCollection AddMailService(
        this IServiceCollection services,
        IConfiguration config
    )
    {
        var mailConfigs = config.GetSection("MailSettings");
        if (mailConfigs == null)
        {
            throw new InvalidOperationException("MailSettings configuration section not found.");
        }

        services.Configure<MailSettings>(mailConfigs);

        services.AddTransient<IMailService, MailService>();
        return services;
    }
}
