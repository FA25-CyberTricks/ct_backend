using ct.backend.Extensions.GoogleStorage;
using ct.backend.Infrastructure.Extension.Database;
using ct.backend.Infrastructure.Extension.Jwt;
using ct.backend.Infrastructure.Extension.Mail;
using ct.backend.Infrastructure.Extension.OData;
using ct.backend.Infrastructure.Extension.Swagger;
using ct.backend.Infrastructure.Extensions.GoogleAuth;

namespace ct.backend.Infrastructure.Extension
{
    public static class CoreServiceRegistration
    {
        [Obsolete]
        public static IServiceCollection AddCoreInfrastructure(
            this IServiceCollection services,
            IConfiguration config
        )
        {
            services.AddDatabase(config);
            services.AddJwtAuthentication(config);
            services.AddMailService(config);
            services.AddGoogleAuthService(config);
            services.AddGoogleStorage(config);
            //services.AddRabbitMq(config);
            //services.AddPayment(config);

            services.AddSwaggerWithAuth();
            services.AddODataSupport();
            services.AddAutoMapper(cfg => cfg.AddMaps(AppDomain.CurrentDomain.GetAssemblies()));

            return services;
        }

    }
}
