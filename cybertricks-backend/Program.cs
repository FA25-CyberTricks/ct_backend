using ct.backend.Domain.Entities;
using ct.backend.Infrastructure.Data;
using ct.backend.Infrastructure.Extension;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Threading.RateLimiting;

namespace ct.backend
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddRouting(options =>
            {
                options.LowercaseUrls = true;
                options.AppendTrailingSlash = false;
            });
            builder.Services.AddCoreInfrastructure(builder.Configuration);
            builder.Services.AddCors(opt =>
            {
                opt.AddPolicy("frontend", p => p
                       .WithOrigins(
                           "http://localhost:5173",
                           "https://localhost:5173",
                           "http://localhost:3000", 
                           "https://localhost:3000",
                           "https://cybertricks.vercel.app"
                       )
                       .AllowAnyHeader()
                       .AllowAnyMethod()
                       .AllowCredentials()            
                   );
            });
            builder.Services.AddRateLimiter(options =>
            {
                options.AddPolicy("AuthRefreshLimiter", context =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 3,
                            Window = TimeSpan.FromSeconds(3),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 0
                        }));
            });

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            //builder.Services.AddEndpointsApiExplorer();
            //builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Seed Database
            //using (var scope = app.Services.CreateScope())
            //{
            //    var ctx = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
            //    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            //    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            //    var seeder = new DatabaseSeeder(ctx, userManager, roleManager);
            //    await seeder.SeedAllAsync();
            //}

            using (var scope = app.Services.CreateScope())
            {
                var ctx = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<BrandStoreDataSeeder>>();
                var seeder = new BrandStoreDataSeeder(ctx, logger);
                await seeder.SeedAllAsync();
            }

            app.UseRateLimiter();

            app.UseForwardedHeaders();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseCors("frontend");

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
