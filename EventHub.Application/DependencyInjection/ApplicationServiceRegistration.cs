using Microsoft.Extensions.DependencyInjection;


namespace EventHub.Application.DependencyInjection
{
    public static class ApplicationServiceRegistration
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddAutoMapper(cfg => cfg.AddMaps(typeof(ApplicationServiceRegistration).Assembly));
            services.AddMediatR(cfg => 
            cfg.RegisterServicesFromAssembly(typeof(ApplicationServiceRegistration).Assembly));

            return services;
        }
    }
}
