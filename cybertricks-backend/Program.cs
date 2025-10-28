using ct.backend.Domain.Entities;
using ct.backend.Infrastructure.Data;
using ct.backend.Infrastructure.Extension;
using Microsoft.AspNetCore.Identity;

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
