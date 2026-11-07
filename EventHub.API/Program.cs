using EventHub.API.Exceptions;
using EventHub.Application.DependencyInjection;
using EventHub.Infrastructure.BackgroundJobs;
using EventHub.Infrastructure.Identity.DataSeeding;
using EventHub.Infrastructure.Services.DependencyInjection;
using Hangfire;
using Microsoft.AspNetCore.HttpOverrides;
using System.Net;
using System.Text.Json.Serialization;

namespace EventHub.API
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(
                        new JsonStringEnumConverter());
                });

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddInfrastructureServices(
                builder.Configuration);

            builder.Services.AddApplicationServices();

            builder.Services.AddHangfireServer();

            builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
            builder.Services.AddProblemDetails();

            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
            // Configure forwarded headers for ngrok.
            builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders =
                    ForwardedHeaders.XForwardedFor |
                    ForwardedHeaders.XForwardedProto;

                options.ForwardLimit = 1;

                // Trust only the local ngrok agent connection.
                options.KnownProxies.Add(IPAddress.Loopback);
                options.KnownProxies.Add(IPAddress.IPv6Loopback);
            });

            var app = builder.Build();

            // MUST execute before HTTPS redirection.
            app.UseForwardedHeaders();

            using (var scope = app.Services.CreateScope())
            {
                var seeder = scope.ServiceProvider
                    .GetRequiredService<IdentitySeeder>();

                await seeder.SeedRolesAsync();
                await seeder.SeedAdminAsync();

                var recurringJobManager = scope.ServiceProvider
                    .GetRequiredService<IRecurringJobManager>();

                HangfireJobRegistration.RegisterRecurringJobs(
                    recurringJobManager);
            }

            app.UseExceptionHandler();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseHangfireDashboard();

            app.MapControllers();

            app.Run();
        }
    }
}
