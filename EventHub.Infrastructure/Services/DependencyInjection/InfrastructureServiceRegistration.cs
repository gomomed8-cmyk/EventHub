using EventHub.Application.Contracts;
using EventHub.Application.Contracts.Payments;
using EventHub.Application.Settings;
using EventHub.Infrastructure.Identity;
using EventHub.Infrastructure.Identity.ContextIdentity;
using EventHub.Infrastructure.Identity.DataSeeding;
using EventHub.Infrastructure.Identity.EntityIdentity;
using EventHub.Infrastructure.Options;
using EventHub.Infrastructure.Persistence.Context;
using EventHub.Infrastructure.Persistence.Repositories;
using EventHub.Infrastructure.Redis;
using EventHub.Infrastructure.Services.Email;
using EventHub.Infrastructure.Services.Payments;
using EventHub.Infrastructure.Services.Pdf;
using EventHub.Infrastructure.Settings;
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using System.Text;
namespace EventHub.Infrastructure.Services.DependencyInjection
{
    public static class InfrastructureServiceRegistration
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services,IConfiguration configuration)
        {
            services.AddDbContext<HubDbContext>(options =>
            {
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));
            });

            services.AddHttpContextAccessor();

            services.AddDbContext<EventHubIdentityDbContext>(options=>
            {
                options.UseSqlServer(configuration.GetConnectionString("IdentityConnection"));
            });
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = configuration.GetConnectionString("Redis");
            });
            services.AddSingleton<IConnectionMultiplexer>(config =>
            {
                return ConnectionMultiplexer.Connect(configuration.GetConnectionString("Redis")!);
            });
            services.AddHangfire(options =>
            {
                options.UseSqlServerStorage(configuration.GetConnectionString("HangfireConnection"));
            });
            services.AddScoped<IRefreshTokenStore, RedisRefreshTokenStore>();
            services.Configure<RefreshTokenSettings>(configuration.GetSection("RefreshTokenSettings"));
            services.Configure<AdminSeedOptions>(configuration.GetSection("AdminSeed"));
            services.AddIdentityCore<ApplicationUser>()
                .AddRoles<IdentityRole<Guid>>()
                .AddEntityFrameworkStores<EventHubIdentityDbContext>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IIdentityService, IdentityService>();
            services.AddScoped<IEventMaintenanceService, EventMaintenanceService>();
            services.AddScoped(typeof(ICacheService<>),typeof(CacheService<>));
            services.AddScoped<IPublicEventsCacheVersionService, PublicEventsCacheVersionService>();
            services.AddScoped<IBookingExpirationService, BookingExpirationService>();
            services.AddScoped<ITicketPdfGenerator, TicketPdfGenerator>();
            services.Configure<JwtSettings>(configuration.GetSection("Jwt"));

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    var jwtSettings = configuration
                        .GetSection("Jwt")
                        .Get<JwtSettings>()!;

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,

                        ValidIssuer = jwtSettings.Issuer,
                        ValidAudience = jwtSettings.Audience,

                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(jwtSettings.SecretKey))
                    };
                });

            services.AddAuthorization();

            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddScoped<IdentitySeeder>();
            services.AddScoped<ICustomerInfoLookup, CustomerInfoLookup>();
            services.AddScoped<IEventBookingSummaryReader, EventBookingSummaryReader>();
            services.AddHttpClient<IPaymentGateway, PaymobPaymentGateway>();
            services.AddScoped<IPaymobWebhookSignatureValidator,PaymobWebhookSignatureValidator>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IEmailTemplateRenderer, EmailTemplateRenderer>();
            services.AddScoped<BookingConfirmationEmailJob>();
            services.AddScoped<IBookingConfirmationEmailScheduler,BookingConfirmationEmailScheduler>();
            services
           .AddOptions<PaymobOptions>().Bind(configuration.GetSection(PaymobOptions.SectionName))
           .Validate(
            options => Uri.TryCreate(options.BaseUrl,UriKind.Absolute,out var uri)
            && uri.Scheme == Uri.UriSchemeHttps,
            "Paymob BaseUrl must be a valid HTTPS URL.")
              .Validate(
             options =>!string.IsNullOrWhiteSpace(options.SecretKey),"Paymob SecretKey is required.")
              .Validate(
             options =>!string.IsNullOrWhiteSpace(options.PublicKey),"Paymob PublicKey is required.")
              .Validate(
             options => !string.IsNullOrWhiteSpace(options.ApiKey),"Paymob ApiKey is required.")
              .Validate(
             options => !string.IsNullOrWhiteSpace(options.HmacSecret),"Paymob HmacSecret is required.")
              .Validate(
             options => options.IntegrationId > 0,"Paymob IntegrationId must be greater than zero.")
              .Validate(
             options => options.Currency == "EGP", "Paymob Currency must be EGP.").ValidateOnStart();

            services.AddOptions<EmailOptions>().Bind(configuration.GetSection(EmailOptions.SectionName))
                .Validate(
                     options => !string.IsNullOrWhiteSpace(options.Host),"Email Host is required.")
                .Validate(
                     options => options.Port > 0 && options.Port <= 65535,"Email Port must be a valid port number.")
                .Validate(
                     options => !string.IsNullOrWhiteSpace(options.Username),"Email Username is required.")
                .Validate(
                     options => !string.IsNullOrWhiteSpace(options.Password),"Email Password is required.")
                .Validate(
                     options => !string.IsNullOrWhiteSpace(options.FromEmail),"Email FromEmail is required.")
                .ValidateOnStart();

            return services;
        }
    }
}
