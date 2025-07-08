using BasculaTerminalApi.Models;
using Core.Application.Interfaces;
using Infrastructure.Repos;

namespace BasculaTerminalApi.Service
{
    public static class AppService
    {
        public static IServiceCollection AddBusinessServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddBasculaService();

            services.AddPrintService(configuration);

            services.AddRepoServices();

            return services;
        }

        private static IServiceCollection AddRepoServices(this IServiceCollection services)
        {
            services.AddTransient<IWeightRepo, WeightRepo>();
            return services;
        }

        private static IServiceCollection AddBasculaService(this IServiceCollection services)
        {
            services.AddSingleton<BasculaService>();
            return services;
        }

        public static IServiceCollection AddPrintService(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<PrintSettings>(sp =>
            {
                PrintSettings? settings = configuration.GetValue<PrintSettings>("HardwareSettings");
                if (settings is null)
                {
                    throw new InvalidDataException("No se pudo cargar la configuración de impresion/bascula desde el archivo de configuración.");
                }
                return settings;
            });

            services.AddSingleton<PrintService>();

            return services;
        }
    }
}
